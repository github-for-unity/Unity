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

#if DEBUG
/*
        For external contributors, we have bundled a developer OAuth application
        called `GitHub for Unity (dev)` so that you can complete the sign in flow
        locally without needing to configure your own application.
        This is for testing only and it is (obviously) public, proceed with caution.

        For a release build, you should create a new oauth application on github.com,
        copy the `common/ApplicationInfo_Local.cs-example`
        template to `common/ApplicationInfo_Local.cs` and fill out the `myClientId` and
        `myClientSecret` fields for your oauth app.
 */
        internal static string ClientId { get; private set; } = "924a97f36926f535e72c";
        internal static string ClientSecret { get; private set; } = "b4fa550b7f8e38034c6b1339084fa125eebb6155";
#else
        internal static string ClientId { get; private set; } = "";
        internal static string ClientSecret { get; private set; } = "";
#endif

        public static string Version { get { return System.AssemblyVersionInformation.Version; } }

        static partial void SetClientData();

        static ApplicationInfo()
        {
            SetClientData();
        }
    }
}