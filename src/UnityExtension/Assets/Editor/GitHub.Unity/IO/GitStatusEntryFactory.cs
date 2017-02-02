using System;

namespace GitHub.Unity
{
    class GitStatusEntryFactory : IGitStatusEntryFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly string gitRoot;

        public GitStatusEntryFactory(IEnvironment environment, IFileSystem fileSystem, IGitEnvironment gitEnvironment)
        {
            this.fileSystem = fileSystem;

            var fullProjectRoot = fileSystem.GetFullPath(environment.UnityProjectPath);
            gitRoot = gitEnvironment.FindRoot(fullProjectRoot);

            var fullGitRoot = fileSystem.GetFullPath(gitRoot);

            var projectRootIsGitRoot = fullProjectRoot == fullGitRoot;
            var projectRootIsGitRootOrChild = projectRootIsGitRoot || fullProjectRoot.StartsWith(fullGitRoot);

            if (!projectRootIsGitRootOrChild)
            {
                throw new Exception("Project root cannot be outside gitroot");
            }
        }

        public GitStatusEntry Create(string path, GitFileStatus status, string originalPath = null)
        {
            var fullPath = fileSystem.Combine(gitRoot, path);

            //TODO: Do we need this?
            //var projectPath = fileSystem.Combine(projectRoot, path);
            string projectPath = null;

            return new GitStatusEntry(path, fullPath, projectPath, status, originalPath);
        }
    }
}