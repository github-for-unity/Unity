using GitHub.Api;

namespace GitHub.Unity
{
    class MacBasedGitEnvironment : GitEnvironment
    {
        public const string DefaultGitPath = "/usr/bin/git";

        public MacBasedGitEnvironment(IFileSystem fileSystem, IEnvironment environment) : base(fileSystem, environment)
        {
        }

        public override string FindGitInstallationPath()
        {
            return FileSystem.FileExists(DefaultGitPath)
                ? DefaultGitPath :
                null;
        }

        public override string GetGitExecutableExtension()
        {
            return null;
        }
    }
}