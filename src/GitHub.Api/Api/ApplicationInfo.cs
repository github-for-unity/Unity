namespace GitHub.Unity
{
    public static class ApplicationInfo
    {
#if DEBUG
        public const string ApplicationName = "GitHubUnityDebug";
        public const string ApplicationProvider = "GitHub";
#else
        public const string ApplicationName = "GitHubUnity";
        public const string ApplicationProvider = "GitHub";
#endif
        public const string ApplicationSafeName = "unity-internal-test";
        public const string ApplicationDescription = "GitHub for Unity";

        internal const string ClientId = "";
        internal const string ClientSecret = "";

        public static string Version { get { return System.AssemblyVersionInformation.Version; } }
    }
}