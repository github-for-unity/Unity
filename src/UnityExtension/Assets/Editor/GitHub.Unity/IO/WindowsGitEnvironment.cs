using System;
using System.Linq;

namespace GitHub.Unity
{
    class WindowsGitEnvironment : GitEnvironment
    {
        private string defaultGitPath;

        public WindowsGitEnvironment(IFileSystem fileSystem, IEnvironment environment):base(fileSystem, environment)
        {
        }

        public override string FindGitInstallationPath()
        {
            if (defaultGitPath != null)
                return defaultGitPath;

            var localAppDataPath = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData);
            var gitHubLocalAppDataPath = System.IO.Path.Combine(localAppDataPath, "GitHub");

            var searchPath = System.IO.Path.Combine(gitHubLocalAppDataPath, "PortableGit_");

            var directories = FileSystem.GetDirectories(gitHubLocalAppDataPath).ToArray();

            var portableGitPath = directories
                .Where(s =>
                {
                    var startsWith = s.StartsWith(searchPath, StringComparison.InvariantCultureIgnoreCase);
                    return startsWith;
                })
                .Select(s => System.IO.Path.Combine(gitHubLocalAppDataPath, s))
                .ToArray()
                .FirstOrDefault();

            if (portableGitPath == null)
            {
                Logger.Debug("Git Installation folder not discovered gitHubLocalAppDataPath:\"" + gitHubLocalAppDataPath);
                return null;
            }

            portableGitPath = System.IO.Path.Combine(gitHubLocalAppDataPath, portableGitPath);
            portableGitPath = System.IO.Path.Combine(portableGitPath, "cmd");
            portableGitPath = System.IO.Path.Combine(portableGitPath, "git.exe");
            defaultGitPath = portableGitPath;

            Logger.Debug("Git Installation folder discovered - path\"" + defaultGitPath + "\"");

            return defaultGitPath;
        }

        public override string GetGitExecutableExtension()
        {
            return "exe";
        }
    }
}