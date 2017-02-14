namespace GitHub.Api
{
    interface IPortableGitManager
    {
//        void ExtractGitIfNeeded();
//        void EnsureSystemConfigFileExtracted();
        string ExtractSuggestedGitAttributes(string targetDirectory);
        bool IsExtracted();
        string GetPortableGitDestinationDirectory(bool createIfNeeded = false);
        string GetPackageNameWithVersion();
    }
}
