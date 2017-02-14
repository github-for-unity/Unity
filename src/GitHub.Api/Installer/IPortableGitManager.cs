namespace GitHub.Api
{
    interface IPortableGitManager
    {
        void ExtractGitIfNeeded();
        bool IsExtracted();
        string GetPortableGitDestinationDirectory(bool createIfNeeded = false);
        void EnsureSystemConfigFileExtracted();
        string ExtractSuggestedGitAttributes(string targetDirectory);
        bool IsPackageExtracted();
        string GetPackageDestinationDirectory(bool createIfNeeded = false, bool includeExpectedVersion = true);
        string GetPackageNameWithVersion();
    }
}
