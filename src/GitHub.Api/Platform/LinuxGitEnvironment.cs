using System.Threading.Tasks;
using System;

namespace GitHub.Unity
{
    class LinuxGitEnvironment : GitProcessEnvironment
    {
        public LinuxGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override ITask<NPath> FindGitInstallationPath(IProcessManager processManager)
        {
            if (!String.IsNullOrEmpty(Environment.GitExecutablePath))
                return new FuncTask<NPath>(TaskEx.FromResult(Environment.GitExecutablePath));

            return base.FindGitInstallationPath(processManager); ;
        }

        public override string GetExecutableExtension()
        {
            return null;
        }
    }
}