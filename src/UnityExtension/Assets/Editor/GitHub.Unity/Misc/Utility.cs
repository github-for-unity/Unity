using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class Utility : ScriptableObject
    {
        private static readonly ILogging logger = Logging.GetLogger<Utility>();

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
            if (!ready)
            {
                onReady += callback;
            }
            else
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

            }
        }

        public static void UnregisterReadyCallback(Action callback)
        {
            onReady -= callback;
        }

        public static void Initialize()
        {
            // Evaluate project settings
            Issues = new List<ProjectConfigurationIssue>();
        }

        public static void Run()
        {
            ready = true;
            onReady.SafeInvoke();
        }

        public static Texture2D GetIcon(string filename, string filename2x = "")
        {
            if (EditorGUIUtility.pixelsPerPoint > 1f && !string.IsNullOrEmpty(filename2x))
            {
                filename = filename2x;
            }

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GitHub.Unity.Icons." + filename);
            if (stream != null)
                return stream.ToTexture2D();

            var iconPath = ExtensionInstallPath.ToNPath().Combine("Icons", filename).ToString(SlashMode.Forward);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        public static Texture2D CreateTextureFromColor(Color color)
        {
					Texture2D backgroundTexture = new Texture2D(1,1);
					Color c = color;
					backgroundTexture.SetPixel(1, 1, c);
					backgroundTexture.Apply();

          return backgroundTexture;
        }

        // Based on: https://www.rosettacode.org/wiki/Find_common_directory_path#C.23
        public static string FindCommonPath(IEnumerable<string> paths)
        {
            var longestPath =
                paths.First(first => first.Length == paths.Max(second => second.Length))
                .ToNPath();

            NPath commonParent = longestPath;
            foreach (var path in paths)
            {
                var cp = commonParent.GetCommonParent(path);
                if (cp != null)
                    commonParent = cp;
                else
                {
                    commonParent = null;
                    break;
                }
            }
            return commonParent;
        }

        public static string GitInstallPath
        {
            get { return EntryPoint.Environment.GitExecutablePath; }
        }

        public static string GitRoot
        {
            get { return EntryPoint.Environment.RepositoryPath; }
        }

        public static string UnityAssetsPath
        {
            get { return EntryPoint.Environment.UnityAssetsPath; }
        }

        public static string UnityProjectPath
        {
            get { return EntryPoint.Environment.UnityProjectPath; }
        }

        public static string ExtensionInstallPath
        {
            get { return EntryPoint.Environment.ExtensionInstallPath; }
        }

        public static List<ProjectConfigurationIssue> Issues { get; protected set; }

        public static bool GitFound
        {
            get { return !string.IsNullOrEmpty(GitInstallPath); }
        }

        public static bool ActiveRepository
        {
            get { return !string.IsNullOrEmpty(GitRoot); }
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
