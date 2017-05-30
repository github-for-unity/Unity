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

        public override ITask<NPath> FindGitInstallationPath(IProcessManager processManager)
        {
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
            NPath path = "/usr/local/bin/git".ToNPath();
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