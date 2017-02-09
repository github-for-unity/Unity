using GitHub.Api;

namespace GitHub.Api
{
    class LinuxGitEnvironment : GitEnvironment
    {
        public const string DefaultGitPath = "/usr/bin/git";

        public LinuxGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
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