namespace GitHub.Api
{
    interface IPortableGitManager
    {
        void ExtractGitIfNeeded();
        bool IsExtracted();
        string GetPortableGitDestinationDirectory(bool createIfNeeded = false);
        string GetPackageDestinationDirectory(bool createIfNeeded = false, bool includeExpectedVersion = true);
        string PackageNameWithVersion { get; }
    }
}
