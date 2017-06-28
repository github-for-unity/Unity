using GitHub.Unity;
using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IGitInstaller
    {
        bool IsExtracted();
        NPath GitInstallationPath { get; }
        string PackageNameWithVersion { get; }
    }
}
