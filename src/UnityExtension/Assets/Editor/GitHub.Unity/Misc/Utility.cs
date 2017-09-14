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
            new Regex(@"^(?<active>\*)?\s+(?<name>[\w\d\/\-_]+)\s*(?:[a-z|0-9]{7} \[(?<tracking>[\w\d\/\-\_]+)\])?");
        public static readonly Regex ListRemotesRegex =
            new Regex(@"(?<name>[\w\d\-_]+)\s+(?<url>https?:\/\/(?<login>(?<user>[\w\d]+)(?::(?<token>[\w\d]+))?)@(?<host>[\w\d\.\/\%]+))\s+\((?<function>fetch|push)\)");
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

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GitHub.Unity.IconsAndLogos." + filename);
            if (stream != null)
                return stream.ToTexture2D();

            var iconPath = ExtensionInstallPath.ToNPath().Combine("IconsAndLogos", filename).ToString(SlashMode.Forward);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        public static Texture2D CreateTextureFromColor(Color color)
        {
            Texture2D backgroundTexture = new Texture2D(1, 1);
            Color c = color;
            backgroundTexture.SetPixel(1, 1, c);
            backgroundTexture.Apply();

            return backgroundTexture;
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

        public static Texture2D GetTextureFromColor(Color color)
        {
            Color[] pix = new Color[1];
            pix[0] = color;

            Texture2D result = new Texture2D(1, 1);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
    }

    static class StreamExtensions
    {
        private static MethodInfo loadImage;
        private static Func<Texture2D, MemoryStream, Texture2D> invokeLoadImage;

        static StreamExtensions()
        {
            var t = typeof(Texture2D).Assembly.GetType("UnityEngine.ImageConversion", false, false);
            if (t != null)
            {
                // looking for ImageConversion.LoadImage(this Texture2D tex, byte[] data)
                loadImage = t.GetMethods().FirstOrDefault(x => x.Name == "LoadImage" && x.GetParameters().Length == 2);
                invokeLoadImage = (tex, ms) =>
                {
                    loadImage.Invoke(null, new object[] { tex, ms.ToArray() });
                    return tex;
                };
            }
            else
            {
                // looking for Texture2D.LoadImage(byte[] data)
                loadImage = typeof(Texture2D).GetMethods().FirstOrDefault(x => x.Name == "LoadImage" && x.GetParameters().Length == 1);
                invokeLoadImage = (tex, ms) =>
                {
                    loadImage.Invoke(tex, new object[] { ms.ToArray() });
                    return tex;
                };
            }
        }

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
                tex = invokeLoadImage(tex, ms);
                return tex;
            }
        }
    }
}
