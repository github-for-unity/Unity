namespace GitHub.Api
{
    interface IPortablePackageManager
    {
        void Clean();
        bool IsPackageExtracted();
        string GetPackageDestinationDirectory(bool createIfNeeded = false, bool includeExpectedVersion = true);
        string GetPackageNameWithVersion();
    }
}