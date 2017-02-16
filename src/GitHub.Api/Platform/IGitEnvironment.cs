using System.Diagnostics;
using System.Threading.Tasks;
using GitHub.Unity;

namespace GitHub.Api
{
    interface IGitEnvironment
    {
        Task<string> FindGitInstallationPath(IProcessManager processManager);

        string GetGitExecutableExtension();

        bool ValidateGitInstall(string path);

        void Configure(ProcessStartInfo psi, string workingDirectory);

        string FindRoot(string path);
    }
}