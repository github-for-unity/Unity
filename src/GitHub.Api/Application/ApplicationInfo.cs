#pragma warning disable 436
namespace GitHub.Unity
{
    static partial class ApplicationInfo
    {
#if DEBUG
        public const string ApplicationName = "GitHubUnityDebug";
        public const string ApplicationProvider = "GitHub";
#else
        public const string ApplicationName = "GitHubUnity";
        public const string ApplicationProvider = "GitHub";
#endif
        public const string ApplicationSafeName = "GitHubUnity";
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