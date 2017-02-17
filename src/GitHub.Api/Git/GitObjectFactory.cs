using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitObjectFactory : IGitObjectFactory
    {
        private readonly IEnvironment environment;
        private readonly IGitEnvironment gitEnvironment;

        public GitObjectFactory(IEnvironment environment, IGitEnvironment gitEnvironment)
        {
            this.environment = environment;
            this.gitEnvironment = gitEnvironment;
        }

        public GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus status, string originalPath = null, bool staged = false)
        {
            var npath = new NPath(path).MakeAbsolute();
            var relativePath = npath.RelativeTo(environment.RepositoryPath);
            var projectPath = npath.RelativeTo(environment.UnityProjectPath);

            return new GitStatusEntry(relativePath, npath, projectPath, status, originalPath?.ToNPath(), staged);
        }

        public GitLock CreateGitLock(string path, string user)
        {
            var npath = new NPath(path);
            var fullPath = npath.RelativeTo(environment.RepositoryPath);

            return new GitLock(path, fullPath, user);
        }
    }
}
