using System.Threading.Tasks;
using GitHub.Api;
using GitHub.Unity;

namespace GitHub.Api
{
    class LinuxGitEnvironment : GitEnvironment
    {
        public LinuxGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override Task<string> FindGitInstallationPath(IProcessManager processManager)
        {
            if (Environment.GitExecutablePath != null)
                return TaskEx.FromResult(Environment.GitExecutablePath);

            return base.FindGitInstallationPath(processManager); ;
        }

        public override string GetGitExecutableExtension()
        {
            return null;
        }
    }
}