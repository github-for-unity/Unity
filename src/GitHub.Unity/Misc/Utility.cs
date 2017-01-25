using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ILogger = GitHub.Unity.Logging.ILogger;

namespace GitHub.Unity
{
    class Utility : ScriptableObject
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger<Utility>();

        public const string StatusRenameDivider = "->";
        public static readonly Regex ListBranchesRegex =
            new Regex(@"^(?<active>\*)?\s+(?<name>[\w\d\/\-\_]+)\s*(?:[a-z|0-9]{7} \[(?<tracking>[\w\d\/\-\_]+)\])?");
        public static readonly Regex ListRemotesRegex = 
            new Regex(@"(?<name>[\w\d\-\_]+)\s+(?<url>https?:\/\/(?<login>(?<user>[\w\d]+)(?::(?<token>[\w\d]+))?)@(?<host>[\w\d\.\/\%]+))\s+\((?<function>fetch|push)\)");
        public static readonly Regex LogCommitRegex = new Regex(@"commit\s(\S+)");
        public static readonly Regex LogMergeRegex = new Regex(@"Merge:\s+(\S+)\s+(\S+)");
        public static readonly Regex LogAuthorRegex = new Regex(@"Author:\s+(.+)\s<(.+)>");
        public static readonly Regex LogTimeRegex = new Regex(@"Date:\s+(.+)");
        public static readonly Regex LogDescriptionRegex = new Regex(@"^\s+(.+)");
        public static readonly Regex StatusStartRegex = new Regex(@"(?<status>[AMRDC]|\?\?)(?:\d*)\s+(?<path>[\w\d\/\.\-_ \@]+)");
        public static readonly Regex StatusEndRegex = new Regex(@"->\s(?<path>[\w\d\/\.\-_ ]+)");
        public static readonly Regex StatusBranchLineValidRegex = new Regex(@"\#\#\s+(?:[\w\d\/\-_\.]+)");
        public static readonly Regex StatusAheadBehindRegex =
                                         new Regex(
                                             @"\[ahead (?<ahead>\d+), behind (?<behind>\d+)\]|\[ahead (?<ahead>\d+)\]|\[behind (?<behind>\d+>)\]");
        public static readonly Regex BranchNameRegex = new Regex(@"^(?<name>[\w\d\/\-\_]+)$");

        private static bool ready;
        private static Action onReady;

        public static void RegisterReadyCallback(Action callback)
        {
            onReady += callback;
            if (ready)
            {
                callback();
            }
        }

        public static void UnregisterReadyCallback(Action callback)
        {
            onReady -= callback;
        }

        public static void Initialize()
        {
            // Unity paths
            UnityAssetsPath = Application.dataPath;
            UnityProjectPath = UnityAssetsPath.Substring(0, UnityAssetsPath.Length - "Assets".Length - 1);

            // Secure settings here so other threads don't try to reload
            Settings.Reload();

            // Juggling to find out where we got installed
            var instance = FindObjectOfType(typeof(Utility)) as Utility;
            if (instance == null)
            {
                instance = CreateInstance<Utility>();
            }

            var script = MonoScript.FromScriptableObject(instance);
            if (script == null)
            {
                ExtensionInstallPath = string.Empty;
            }
            else
            {
                ExtensionInstallPath = AssetDatabase.GetAssetPath(script);
                ExtensionInstallPath = ExtensionInstallPath.Substring(0, ExtensionInstallPath.LastIndexOf('/'));
                ExtensionInstallPath = ExtensionInstallPath.Substring(0, ExtensionInstallPath.LastIndexOf('/'));
            }

            DestroyImmediate(instance);

            // Evaluate project settings
            Issues = new List<ProjectConfigurationIssue>();
            EvaluateProjectConfigurationTask.UnregisterCallback(OnEvaluationResult);
            EvaluateProjectConfigurationTask.RegisterCallback(OnEvaluationResult);
            EvaluateProjectConfigurationTask.Schedule();

            // Root paths
            if (string.IsNullOrEmpty(GitInstallPath) || !File.Exists(GitInstallPath))
            {
                FindGitTask.Schedule(path =>
                    {
                        Logger.Debug("found " + path);
                        if (!string.IsNullOrEmpty(path))
                        {
                            GitInstallPath = path;
                            DetermineGitRoot();
                            OnPrepareCompleted();
                        }
                    },
                    () => Logger.Debug("NOT FOUND")
                );
            }
            else
            {
                DetermineGitRoot();
                OnPrepareCompleted();
            }
        }

        public static string RepositoryPathToAbsolute(string repositoryPath)
        {
            return Path.Combine(GitRoot, repositoryPath);
        }

        public static string RepositoryPathToAsset(string repositoryPath)
        {
            var localDataPath = UnityAssetsPath.Substring(GitRoot.Length + 1);
            return repositoryPath.IndexOf(localDataPath) == 0
                ? ("Assets" + repositoryPath.Substring(localDataPath.Length)).Replace(Path.DirectorySeparatorChar, '/')
                : null;
        }

        public static string AssetPathToRepository(string assetPath)
        {
            var localDataPath = UnityAssetsPath.Substring(GitRoot.Length + 1);
            return Path.Combine(localDataPath.Substring(0, localDataPath.Length - "Assets".Length),
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        public static Texture2D GetIcon(string filename, string filename2x = "")
        {
            if (EditorGUIUtility.pixelsPerPoint > 1f && !string.IsNullOrEmpty(filename2x))
            {
                filename = filename2x;
            }

            return Assembly.GetExecutingAssembly().GetManifestResourceStream("GitHub.Unity.Icons." + filename).ToTexture2D();
        }

        // Based on: https://www.rosettacode.org/wiki/Find_common_directory_path#C.23
        public static string FindCommonPath(string separator, IEnumerable<string> paths)
        {
            var commonPath = string.Empty;
            var separatedPath =
                paths.First(first => first.Length == paths.Max(second => second.Length))
                     .Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                     .ToList();

            foreach (var pathSegment in separatedPath.AsEnumerable())
            {
                var pathExtension = pathSegment + separator;

                if (commonPath.Length == 0 && paths.All(path => path.StartsWith(pathExtension)))
                {
                    commonPath = pathExtension;
                }
                else if (paths.All(path => path.StartsWith(commonPath + pathExtension)))
                {
                    commonPath += pathExtension;
                }
                else
                {
                    break;
                }
            }

            return commonPath;
        }

        private static void OnPrepareCompleted()
        {
            ready = true;
            if (onReady != null)
            {
                onReady();
            }
        }

        private static void OnEvaluationResult(IEnumerable<ProjectConfigurationIssue> result)
        {
            Issues = new List<ProjectConfigurationIssue>(result);
        }

        private static void DetermineGitRoot()
        {
            GitRoot = FindRoot(UnityAssetsPath);
        }

        // TODO: replace with libgit2sharp call
        private static string FindRoot(string path)
        {
            if (string.IsNullOrEmpty(Path.GetDirectoryName(path)))
            {
                return null;
            }

            if (Directory.Exists(Path.Combine(path, ".git")))
            {
                return path;
            }

            return FindRoot(Directory.GetParent(path).FullName);
        }

        public static string GitInstallPath
        {
            get { return Settings.Get("GitInstallPath"); }
            set { Settings.Set("GitInstallPath", value); }
        }

        public static string GitRoot { get; protected set; }
        public static string UnityAssetsPath { get; protected set; }
        public static string UnityProjectPath { get; protected set; }
        public static string ExtensionInstallPath { get; protected set; }
        public static List<ProjectConfigurationIssue> Issues { get; protected set; }

        public static bool GitFound
        {
            get { return !string.IsNullOrEmpty(GitInstallPath); }
        }

        public static bool ActiveRepository
        {
            get { return !string.IsNullOrEmpty(GitRoot); }
        }

        public static bool IsWindows
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsDevelopmentBuild
        {
            get { return File.Exists(Path.Combine(UnityProjectPath.Replace('/', Path.DirectorySeparatorChar), ".devroot")); }
        }
    }

    static class StreamExtensions
    {
        public static Texture2D ToTexture2D(this Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                var tex = new Texture2D(1, 1);
                tex.LoadImage(ms.ToArray());
                return tex;
            }
        }
    }
}
