using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitObjectFactory : IGitObjectFactory
    {
        private readonly IEnvironment environment;

        public GitObjectFactory(IEnvironment environment)
        {
            this.environment = environment;
        }

        public GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus status, string originalPath = null, bool staged = false)
        {
            var npath = new NPath(path).MakeAbsolute();
            var relativePath = npath.RelativeTo(environment.RepositoryPath);
            var projectPath = npath.RelativeTo(environment.UnityProjectPath);

            return new GitStatusEntry(relativePath, npath, projectPath, status, originalPath?.ToNPath(), staged);
        }

        public GitLock CreateGitLock(string path, string user, int id)
        {
            var npath = new NPath(path).MakeAbsolute();
            var fullPath = npath.RelativeTo(environment.RepositoryPath);

            return new GitLock(path, fullPath, user, id);
        }
    }
}
