namespace GitHub.PortableGit.Helpers
{
    public interface IPortableGitManager
    {
//        void ExtractGitIfNeeded();
//        void EnsureSystemConfigFileExtracted();
        string ExtractSuggestedGitAttributes(string targetDirectory);
        bool IsExtracted();
        string GetPortableGitDestinationDirectory(bool createIfNeeded = false);
        string GetPackageNameWithVersion();
        string GitExecutablePath { get; }
        string EtcDirectoryPath { get; }
    }
}
