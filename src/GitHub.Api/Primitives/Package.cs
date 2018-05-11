using GitHub.Logging;
using System;

namespace GitHub.Unity
{
    public class Package
    {
        private string version;
        public string Md5 { get; set; }
        public string Url { get; set; }
        [NotSerialized] private UriString uri;
        [NotSerialized] public UriString Uri
        {
            get
            {
                if (uri == null)
                    uri = Url.ToString();
                return uri;
            }
        }
        public string ReleaseNotes { get; set; }
        public string ReleaseNotesUrl { get; set; }
        public string Message { get; set; }
        [NotSerialized] public TheVersion Version { get { return TheVersion.Parse(version); } set { version = value.ToString(); } }

        public static Package Load(IEnvironment environment, UriString packageFeed)
        {
            Package package = null;
            var key = packageFeed.Filename.ToNPath().FileNameWithoutExtension + "_updatelastCheckTime";
            var now = DateTimeOffset.Now;
            NPath feed = environment.UserCachePath.Combine(packageFeed.Filename);

            if (!feed.FileExists() || now.Date > environment.UserSettings.Get<DateTimeOffset>(key).Date)
            {
                feed = new DownloadTask(TaskManager.Instance.Token, environment.FileSystem, packageFeed, environment.UserCachePath)
                        .Catch(e => true)
                        .RunWithReturn(true);

                if (feed.IsInitialized)
                    environment.UserSettings.Set<DateTimeOffset>(key, now);
            }

            if (!feed.IsInitialized)
            {
                // try from assembly resources
                feed = AssemblyResources.ToFile(ResourceType.Platform, packageFeed.Filename, environment.UserCachePath.Combine(packageFeed.Filename), environment);
            }

            if (feed.IsInitialized)
            {
                try
                {
                    package = feed.ReadAllText().FromJson<Package>(true, false);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                }
            }
            return package;
        }
    }
}