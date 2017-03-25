using GitHub.Unity;
using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IGitInstaller
    {
        bool IsExtracted();
        NPath PackageDestinationDirectory { get; }
        string PackageNameWithVersion { get; }
    }
}
