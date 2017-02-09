using GitHub.Api;
using System;

namespace GitHub.Unity
{
    class GitStatusEntryFactory : IGitStatusEntryFactory
    {
        private readonly IFileSystem filesystem;
        private readonly string gitRoot;

        public GitStatusEntryFactory(IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment)
        {
            this.filesystem = filesystem;

            var fullProjectRoot = filesystem.GetFullPath(environment.UnityProjectPath);
            gitRoot = gitEnvironment.FindRoot(fullProjectRoot);

            var fullGitRoot = filesystem.GetFullPath(gitRoot);

            var projectRootIsGitRoot = fullProjectRoot == fullGitRoot;
            var projectRootIsGitRootOrChild = projectRootIsGitRoot || fullProjectRoot.StartsWith(fullGitRoot);

            if (!projectRootIsGitRootOrChild)
            {
                throw new Exception("Project root cannot be outside gitroot");
            }
        }

        public GitStatusEntry Create(string path, GitFileStatus status, string originalPath = null, bool staged = false)
        {
            var fullPath = filesystem.Combine(gitRoot, path);

            //TODO: Do we need this?
            //var projectPath = filesystem.Combine(projectRoot, path);
            string projectPath = null;

            return new GitStatusEntry(path, fullPath, projectPath, status, originalPath, staged);
        }
    }
}
