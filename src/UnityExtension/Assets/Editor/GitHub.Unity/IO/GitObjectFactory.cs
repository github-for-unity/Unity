using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitObjectFactory : IGitObjectFactory
    {
        private readonly IEnvironment environment;
        private readonly IGitEnvironment gitEnvironment;
        private readonly IFileSystem filesystem;
        private string fullGitRoot;

        public GitObjectFactory(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem filesystem)
        {
            this.environment = environment;
            this.gitEnvironment = gitEnvironment;
            this.filesystem = filesystem;
        }

        public GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus status, string originalPath = null, bool staged = false)
        {
            var fullPath = GetFullPath(path);
            var projectPath = GetProjectPath(path);

            return new GitStatusEntry(path, fullPath, projectPath, status, originalPath, staged);
        }

        public GitLock CreateGitLock(string path, string user)
        {
            var fullPath = GetFullPath(path);

            return new GitLock(path, fullPath, user);
        }

        private string GetFullGitRoot()
        {
            if (fullGitRoot == null)
            {
                var fullProjectRoot = filesystem.GetFullPath(environment.UnityProjectPath);
                var gitRoot = gitEnvironment.FindRoot(fullProjectRoot);
                var testFullGitRoot = filesystem.GetFullPath(gitRoot);

                var projectRootIsGitRoot = fullProjectRoot == testFullGitRoot;
                var projectRootIsGitRootOrChild = projectRootIsGitRoot || fullProjectRoot.StartsWith(testFullGitRoot);

                if (!projectRootIsGitRootOrChild)
                {
                    throw new Exception("Project root cannot be outside gitroot");
                }

                fullGitRoot = testFullGitRoot;
            }

            return fullGitRoot;
        }

        private string GetFullPath(string path)
        {
            var gitRoot = GetFullGitRoot();
            return filesystem.Combine(gitRoot, path);
        }

        private string GetProjectPath(string path)
        {
            //TODO: Do we need this?
            //var projectPath = filesystem.Combine(projectRoot, path);
            return null;
        }
    }
}
