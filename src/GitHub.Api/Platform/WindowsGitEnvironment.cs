using GitHub.Unity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Api
{
    class WindowsGitEnvironment : GitEnvironment
    {
        public WindowsGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override async Task<string> FindGitInstallationPath(IProcessManager processManager)
        {
            if (!String.IsNullOrEmpty(Environment.GitExecutablePath))
                return Environment.GitExecutablePath;

            var path = LookForPortableGit();

            if (path == null)
            {
                path =  await base.FindGitInstallationPath(processManager).ConfigureAwait(false);
            }

            Logger.Debug("Git Installation folder {0} discovered: '{1}'", path == null ? "not" : "", path);
            return path;
        }

        private string LookForPortableGit()
        {
            var localAppDataPath = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData);
            var gitHubLocalAppDataPath = System.IO.Path.Combine(localAppDataPath, "GitHub");

            var searchPath = System.IO.Path.Combine(gitHubLocalAppDataPath, "PortableGit_");

            if (!FileSystem.DirectoryExists(gitHubLocalAppDataPath))
                return null;

            var directories = FileSystem.GetDirectories(gitHubLocalAppDataPath).ToArray();

            var portableGitPath = directories.Where(s => {
                var startsWith = s.StartsWith(searchPath, StringComparison.InvariantCultureIgnoreCase);
                return startsWith;
            }).Select(s => System.IO.Path.Combine(gitHubLocalAppDataPath, s)).ToArray().FirstOrDefault();

            if (portableGitPath != null)
            {
                portableGitPath = System.IO.Path.Combine(gitHubLocalAppDataPath, portableGitPath);
                portableGitPath = System.IO.Path.Combine(portableGitPath, "cmd");
                portableGitPath = System.IO.Path.Combine(portableGitPath, "git.exe");
            }

            return portableGitPath;
        }

        public override string GetGitExecutableExtension()
        {
            return "exe";
        }
    }
}