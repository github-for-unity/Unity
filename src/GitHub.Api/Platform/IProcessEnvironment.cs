using System.Diagnostics;
using System.Threading.Tasks;
using GitHub.Unity;

namespace GitHub.Unity
{
    interface IProcessEnvironment
    {
        Task<string> FindGitInstallationPath(IProcessManager processManager);

        string GetExecutableExtension();

        bool ValidateGitInstall(string path);

        void Configure(ProcessStartInfo psi, string workingDirectory);

        string FindRoot(string path);
    }
}