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

    class UpdateCheckWindow : BaseWindow
    {
        public const string UpdateFeedUrl =
#if DEBUG
        "http://localhost:50000/unity/latest.json"
#else
        "http://github-vs.s3.amazonaws.com/unity/latest.json"
#endif
        ;

        public static void CheckForUpdates(IApplicationManager manager)
        {
            var download = new DownloadTask(manager.TaskManager.Token, manager.Environment.FileSystem, UpdateFeedUrl, manager.Environment.UserCachePath)
                .Catch(ex =>
                {
                    LogHelper.Warning(@"Error downloading update check:{0} ""{1}""", UpdateFeedUrl, ex.GetExceptionMessageShort());
                    return true;
                });
            download.OnEnd += (thisTask, result, success, exception) =>
            {
                if (success)
                {
                    try
                    {
                        Package package = result.ReadAllText().FromJson<Package>(lowerCase: true, onlyPublic: false);
                        TheVersion current = TheVersion.Parse(ApplicationInfo.Version);
                        TheVersion newVersion = package.Version;

                        var versionToSkip = manager.UserSettings.Get<TheVersion>(Constants.SkipVersionKey);
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

                        manager.TaskManager.RunInUI(() =>
                        {
                            NotifyOfNewUpdate(manager, current, package);
                        });
                    }
                    catch(Exception ex)
                    {
                        LogHelper.GetLogger<UpdateCheckWindow>().Error(ex);
                    }
                }
            };
            download.Start();
        }

        private static void NotifyOfNewUpdate(IApplicationManager manager, TheVersion currentVersion, Package package)
        {
            var window = GetWindowWithRect<UpdateCheckWindow>(new Rect(100, 100, 580, 400), true, windowTitle);
            window.Initialize(manager, currentVersion, package);
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

        private void Initialize(IApplicationManager manager, TheVersion current, Package newPackage)
        {
            package = new GUIPackage(newPackage);
            currentVersion = current.ToString();
            var requiresRedraw = guiLogo != null;
            guiLogo = null;
            this.InitializeWindow(manager, requiresRedraw);
        }

        public override void OnDataUpdate(bool first)
        {
            base.OnDataUpdate(first);
            LoadContents(first);
        }

        public override void OnUI()
        {
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
                Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void LoadContents(bool first)
        {
            if (!first) return;

            guiLogo = new GUIContent(Styles.BigLogo);
            guiNewUpdate = new GUIContent(String.Format(newUpdateMessage, currentVersion,
                package.Package.Version.ToString()));
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

        public override bool IsBusy { get { return false; } }
    }
}
