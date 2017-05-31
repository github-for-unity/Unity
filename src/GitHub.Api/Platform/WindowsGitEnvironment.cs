using GitHub.Unity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class WindowsGitEnvironment : GitProcessEnvironment
    {
        public WindowsGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override ITask<NPath> FindGitInstallationPath(IProcessManager processManager)
        {
            if (!String.IsNullOrEmpty(Environment.GitExecutablePath))
                return new FuncTask<NPath>(TaskEx.FromResult(Environment.GitExecutablePath));

            return new FuncTask<NPath>(TaskManager.Instance.Token, _ =>
                {
                    Logger.Trace("Looking for Git Installation folder");
                    return LookForPortableGit();
                })
                .ThenIf(path =>
                {
                    if (path == null)
                        return base.FindGitInstallationPath(processManager);
                    else
                        return new FuncTask<NPath>(TaskEx.FromResult(path));
                })
                .Finally((s, e, path) =>
                {
                    Logger.Trace("Git Installation folder {0} discovered: '{1}'", path == null ? "not" : "", path);
                    return path;
                });
        }

        private NPath LookForPortableGit()
        {
            var gitHubLocalAppDataPath = Environment.UserCachePath;
            if (!gitHubLocalAppDataPath.DirectoryExists())
                return null;

            var searchPath = "PortableGit_";

            var portableGitPath = gitHubLocalAppDataPath.Directories()
                .Where(s => s.FileName.StartsWith(searchPath, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();

            if (portableGitPath != null)
            {
                portableGitPath = portableGitPath.Combine("cmd", $"git.{GetExecutableExtension()}");
            }

            return portableGitPath;
        }

        public override string GetExecutableExtension()
        {
            return "exe";
        }
    }
}