namespace GitHub.Api
{
    class MacGitEnvironment : GitEnvironment
    {
        public const string DefaultGitPath = "/usr/bin/git";

        public MacGitEnvironment(IEnvironment environment, IFileSystem filesystem)
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