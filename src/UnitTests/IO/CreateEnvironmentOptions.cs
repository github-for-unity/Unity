namespace UnitTests
{
    class CreateEnvironmentOptions
    {
        public const string DefaultExtensionFolder = @"c:\ExtensionFolder";
        public const string DefaultUserProfilePath = @"c:\UserProfile";

        public string Extensionfolder { get; set; } = DefaultExtensionFolder;
        public string UserProfilePath { get; set; } = DefaultUserProfilePath;
    }
}