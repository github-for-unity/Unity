namespace GitHub.Api
{
    interface IPortableGitManager
    {
        void ExtractGitIfNeeded();
        bool IsExtracted();
        string PackageDestinationDirectory { get; }
        string PackageNameWithVersion { get; }
    }
}
