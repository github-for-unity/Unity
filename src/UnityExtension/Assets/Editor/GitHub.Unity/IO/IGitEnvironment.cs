using System.Diagnostics;

namespace GitHub.Unity
{
    interface IGitEnvironment
    {
        string FindGitInstallationPath();

        string GetGitExecutableExtension();

        bool ValidateGitInstall(string path);

        void Configure(ProcessStartInfo psi, string workingDirectory);
    }
}