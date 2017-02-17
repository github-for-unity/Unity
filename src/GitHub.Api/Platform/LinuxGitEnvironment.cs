using System.Threading.Tasks;
using System;

namespace GitHub.Unity
{
    class LinuxGitEnvironment : GitEnvironment
    {
        public LinuxGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override Task<string> FindGitInstallationPath(IProcessManager processManager)
        {
            if (!String.IsNullOrEmpty(Environment.GitExecutablePath))
                return TaskEx.FromResult(Environment.GitExecutablePath);

            return base.FindGitInstallationPath(processManager); ;
        }

        public override string GetGitExecutableExtension()
        {
            return null;
        }
    }
}