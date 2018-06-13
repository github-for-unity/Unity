#pragma warning disable 436
namespace GitHub.Unity
{
    static partial class ApplicationInfo
    {
#if DEBUG
        public const string ApplicationName = "GitHub for Unity Debug";
        public const string ApplicationProvider = "GitHub";
        public const string ApplicationSafeName = "GitHubUnity-dev";
#else
        public const string ApplicationName = "GitHubUnity";
        public const string ApplicationProvider = "GitHub";
        public const string ApplicationSafeName = "GitHubUnity";
#endif
        public const string ApplicationDescription = "GitHub for Unity";

        internal static string ClientId { get; private set; } = "";
        internal static string ClientSecret { get; private set; } = "";

        public static string Version { get { return System.AssemblyVersionInformation.Version; } }

        static partial void SetClientData();

        static ApplicationInfo()
        {
            SetClientData();
        }
    }
}