using GitHub.Unity;
using System;

namespace GitHub.Unity
{
    interface IPortableGitManager
    {
        void ExtractGitIfNeeded(IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null);

        bool IsExtracted();
        NPath PackageDestinationDirectory { get; }
        string PackageNameWithVersion { get; }
    }
}
