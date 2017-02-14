namespace GitHub.Api
{
    interface IPortableGitManager: IPortablePackageManager
    {
        void ExtractGitIfNeeded();
        bool IsExtracted();
        string GetPortableGitDestinationDirectory(bool createIfNeeded = false);
        void EnsureSystemConfigFileExtracted();
        string ExtractSuggestedGitAttributes(string targetDirectory);
    }
}
