using GitHub.Unity;
using NSubstitute.Core;

namespace TestUtils
{
    class CreateEnvironmentOptions
    {
        public const string DefaultExtensionFolder = @"c:\GitHubUnity\ExtensionFolder";
        public const string DefaultUserProfilePath = @"c:\GitHubUnity\UserProfile";
        public const string DefaultUnityProjectPathAndRepositoryPath = @"c:\GitHubUnity\UnityProject";

        public NPath Extensionfolder { get; set; } = DefaultExtensionFolder.ToNPath();
        public NPath UserProfilePath { get; set; } = DefaultUserProfilePath.ToNPath();
        public NPath UnityProjectPath { get; set; } = DefaultUnityProjectPathAndRepositoryPath.ToNPath();
        public string RepositoryPath { get; set; } = DefaultUnityProjectPathAndRepositoryPath;
    }
}