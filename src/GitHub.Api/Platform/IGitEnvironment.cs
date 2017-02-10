using System.Diagnostics;

namespace GitHub.Api
{
    interface IGitEnvironment
    {
        string FindGitInstallationPath();

        string GetGitExecutableExtension();

        bool ValidateGitInstall(string path);

        void Configure(ProcessStartInfo psi, string workingDirectory);

        string FindRoot(string path);
    }
}