using GitHub.Unity;
using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IGitInstaller
    {
        Task<bool> ExtractGitIfNeeded(NPath tempPath, IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null);

        bool IsExtracted();
        NPath PackageDestinationDirectory { get; }
        string PackageNameWithVersion { get; }
    }
}
