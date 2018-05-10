using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using GitHub.Logging;

namespace GitHub.Unity
{
    [Serializable]
    class GUIPackage
    {
        [SerializeField] private string version;
        [SerializeField] private string url;
        [SerializeField] private string releaseNotes;
        [SerializeField] private string releaseNotesUrl;
        [SerializeField] private string message;

        [NonSerialized] private Package package;
        public Package Package
        {
            get
            {
                if (package == null)
                {
                    package = new Package
                    {
                        Version = TheVersion.Parse(version),
                        Url = url,
                        ReleaseNotes = releaseNotes,
                        ReleaseNotesUrl = releaseNotesUrl,
                        Message = message
                    };
                }
                return package;
            }
        }

        public GUIPackage()
        {}

        public GUIPackage(Package package)
        {
            version = package.Version.ToString();
            url = package.Url;
            releaseNotes = package.ReleaseNotes;
            releaseNotesUrl = package.ReleaseNotesUrl;
            message = package.Message;
        }
    }

    public class UpdateCheckWindow :  EditorWindow
    {
        public const string UpdateFeedUrl =
#if DEBUG
        "http://localhost:50000/unity/latest.json"
#else
        "https://ghfvs-installer.github.com/unity/latest.json"
#endif
        ;

        public static void CheckForUpdates()
        {
            var download = new DownloadTask(TaskManager.Instance.Token, EntryPoint.Environment.FileSystem, UpdateFeedUrl, EntryPoint.Environment.UserCachePath);
            download.OnEnd += (thisTask, result, success, exception) =>
            {
                if (success)
                {
                    try
                    {
                        Package package = result.ReadAllText().FromJson<Package>(lowerCase: true, onlyPublic: false);
                        TheVersion current = TheVersion.Parse(ApplicationInfo.Version);
                        TheVersion newVersion = package.Version;

                        var versionToSkip = EntryPoint.ApplicationManager.UserSettings.Get<TheVersion>(Constants.SkipVersionKey);
                        if (versionToSkip == newVersion)
                        {
                            LogHelper.Info("Skipping GitHub for Unity update v" + newVersion);
                            return;
                        }

                        if (newVersion <= current)
                        {
                            LogHelper.Trace("Skipping GitHub for Unity update v" + newVersion + ", we already have it");
                            return;
                        }


                        TaskManager.Instance.RunInUI(() =>
                        {
                            NotifyOfNewUpdate(current, package);
                        });
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                }
            };
            download.Start();
        }

        private static void NotifyOfNewUpdate(TheVersion currentVersion, Package package)
        {
            var window = GetWindowWithRect<UpdateCheckWindow>(new Rect(100, 100, 580, 400), true, windowTitle);
            window.Initialize(currentVersion, package);
            window.Show();
        }

        private const string windowTitle = "GitHub for Unity Update Check";
        private const string newUpdateMessage = "There is a new version of GitHub for Unity available.\n\nCurrent version is {0}\nNew version is {1}";
        private const string skipThisVersionMessage = "Skip new version";
        private const string downloadNewVersionMessage = "Download new version";
        private const string browseReleaseNotes = "Browse the release notes";

        private static GUIContent guiLogo;
        private static GUIContent guiNewUpdate;
        private static GUIContent guiPackageReleaseNotes;
        private static GUIContent guiPackageMessage;
        private static GUIContent guiSkipThisVersion;
        private static GUIContent guiDownloadNewVersion;
        private static GUIContent guiBrowseReleaseNotes;

        [SerializeField] private GUIPackage package;
        [SerializeField] private string currentVersion;
        [SerializeField] private Vector2 scrollPos;
        [SerializeField] private bool hasReleaseNotes;
        [SerializeField] private bool hasReleaseNotesUrl;
        [SerializeField] private bool hasMessage;

        private void Initialize(TheVersion current, Package newPackage)
        {
            package = new GUIPackage(newPackage);
            currentVersion = current.ToString();
            if (guiLogo != null)
            {
                guiLogo = null;
                Repaint();
            }
        }

        private void OnGUI()
        {
            LoadContents();

            GUILayout.BeginVertical();

            GUILayout.Space(10);
            GUI.Box(new Rect(13, 8, guiLogo.image.width, guiLogo.image.height), guiLogo, GUIStyle.none);

            GUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(150);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(guiNewUpdate, "WordWrappedLabel", GUILayout.Width(300));

            if (hasReleaseNotesUrl)
            {
                if (GUILayout.Button(guiBrowseReleaseNotes, Styles.HyperlinkStyle))
                {
                    Help.BrowseURL(package.Package.ReleaseNotesUrl);
                }
            }

            if (hasMessage || hasReleaseNotes)
            {
                GUILayout.Space(20);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(405), GUILayout.Height(200));
                if (hasMessage)
                {
                    EditorGUILayout.LabelField(guiPackageMessage, "WordWrappedLabel");
                }

                if (hasReleaseNotes)
                {
                    EditorGUILayout.LabelField(guiPackageReleaseNotes, "WordWrappedLabel");
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(guiDownloadNewVersion, GUILayout.Width(200)))
            {
                Help.BrowseURL(package.Package.Url);
                Close();
            }

            if (GUILayout.Button(guiSkipThisVersion, GUILayout.Width(200)))
            {
                var settings = EntryPoint.ApplicationManager.UserSettings;
                settings.Set<TheVersion>(Constants.SkipVersionKey, package.Package.Version);
                this.Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void LoadContents()
        {
            if (guiLogo != null)
                return;

            guiLogo = new GUIContent(Styles.BigLogo);
            guiNewUpdate = new GUIContent(String.Format(newUpdateMessage, currentVersion, package.Package.Version.ToString()));
            guiSkipThisVersion = new GUIContent(skipThisVersionMessage);
            guiDownloadNewVersion = new GUIContent(downloadNewVersionMessage);
            guiBrowseReleaseNotes = new GUIContent(browseReleaseNotes);
            hasMessage = !String.IsNullOrEmpty(package.Package.Message);
            hasReleaseNotes = !String.IsNullOrEmpty(package.Package.ReleaseNotes);
            hasReleaseNotesUrl = !String.IsNullOrEmpty(package.Package.ReleaseNotesUrl);
            if (hasMessage)
                guiPackageMessage = new GUIContent(package.Package.Message);
            if (hasReleaseNotes)
                guiPackageReleaseNotes = new GUIContent(package.Package.ReleaseNotes);
        }

    }
}
