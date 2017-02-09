using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitStatusEntryFactory : IGitStatusEntryFactory
    {
        private readonly IEnvironment environment;
        private readonly IFileSystem filesystem;
        private readonly IGitEnvironment gitEnvironment;
        private string fullGitRoot;

        public GitStatusEntryFactory(IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment)
        {
            this.environment = environment;
            this.filesystem = filesystem;
            this.gitEnvironment = gitEnvironment;
        }

        public GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus status, string originalPath = null, bool staged = false)
        {
            var fullPath = GetFullPath(path);
            var projectPath = GetProjectPath(path);

            return new GitStatusEntry(path, fullPath, projectPath, status, originalPath, staged);
        }

        public GitLock CreateGitLock(string path, string server, string user, int userId)
        {
            var fullPath = GetFullPath(path);

            return new GitLock(path, fullPath, server, user, userId);
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
