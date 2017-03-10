using System.Threading.Tasks;
using GitHub.Unity;
using System;

namespace GitHub.Unity
{
    class MacGitEnvironment : GitProcessEnvironment
    {
        public MacGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override async Task<string> FindGitInstallationPath(IProcessManager processManager)
        {
            Logger.Trace("Looking for Git Installation folder");

            //if (!String.IsNullOrEmpty(Environment.GitExecutablePath))
            //    return Environment.GitExecutablePath;

            var path = LookForPortableGit();

            if (path == null)
            {
                path =  await base.FindGitInstallationPath(processManager);
            }

            Logger.Trace("Git Installation folder {0} discovered: '{1}'", path == null ? "not" : "", path);
            return path;
        }

        private string LookForPortableGit()
        {
            NPath path = "/usr/local/bin/git";
            if (path.FileExists())
                return path;

            return null;
        }
        public override string GetExecutableExtension()
        {
            return null;
        }
    }
}