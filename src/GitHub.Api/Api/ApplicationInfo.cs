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

        internal const string ClientId = "107b906ff287f62a12a4";
        internal const string ClientSecret = "fcb983cd490063a9c08efc9e32545fff197d2137";

        public static string Version { get { return System.AssemblyVersionInformation.Version; } }
    }
}