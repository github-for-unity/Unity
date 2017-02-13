using System;
using GitHub.Helpers;
using GitHub.IO;

namespace GitHub.PortableGit.Helpers
{
    public interface IPortableGitManager
    {
        IObservable<ProgressResult> ExtractGitIfNeeded();
        IObservable<IFile> EnsureSystemConfigFileExtracted();
        string ExtractSuggestedGitAttributes(string targetDirectory);
        bool IsExtracted();
        string GetPortableGitDestinationDirectory(bool createIfNeeded = false);
        string GetPackageNameWithVersion();
        string GitExecutablePath { get; }
        string EtcDirectoryPath { get; }
    }
}
