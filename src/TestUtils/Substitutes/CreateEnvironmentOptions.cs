namespace TestUtils
{
    class CreateEnvironmentOptions
    {
        public const string DefaultExtensionFolder = @"c:\ExtensionFolder";
        public const string DefaultUserProfilePath = @"c:\UserProfile";
        public const string DefaultUnityProjectPath = @"c:\UnityProject";

        public string Extensionfolder { get; set; } = DefaultExtensionFolder;
        public string UserProfilePath { get; set; } = DefaultUserProfilePath;
        public string UnityProjectPath { get; set; } = DefaultUnityProjectPath;
    }
}