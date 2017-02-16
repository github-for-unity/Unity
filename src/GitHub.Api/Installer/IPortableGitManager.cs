using System;

namespace GitHub.Api
{
    interface IPortableGitManager
    {
        void ExtractGitIfNeeded(IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null);

        bool IsExtracted();
        string PackageDestinationDirectory { get; }
        string PackageNameWithVersion { get; }
    }
}
