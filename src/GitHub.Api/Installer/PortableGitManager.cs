using System;

namespace GitHub.Api
{
    class PortableGitManager : PortablePackageManager, IPortableGitManager
    {
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit.zip";

        public PortableGitManager(IEnvironment environment, IFileSystem fileSystem, ISharpZipLibHelper sharpZipLibHelper)
            : base(environment, fileSystem, sharpZipLibHelper)
        {}

        public void ExtractGitIfNeeded()
        {
            ExtractPackageIfNeeded(WindowsPortableGitZip, null, null);
        }

        public bool IsExtracted()
        {
            return IsPackageExtracted();
        }

        public string GetPortableGitDestinationDirectory(bool createIfNeeded = false)
        {
            return GetPackageDestinationDirectory(createIfNeeded);
        }

        public void EnsureSystemConfigFileExtracted()
        {
            throw new NotImplementedException();
        }

        public string ExtractSuggestedGitAttributes(string targetDirectory)
        {
            throw new NotImplementedException();
        }

        protected override string GetExpectedVersion()
        {
            return "f02737a78695063deace08e96d5042710d3e32db";
        }

        protected override string GetPathToCanary(string rootDir)
        {
            return FileSystem.Combine(rootDir, "cmd", "git.exe");
        }

        protected override string GetPackageName()
        {
            return "PortableGit";
        }
    }
}
