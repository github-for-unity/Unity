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
            var absolutePath = new NPath(path).MakeAbsolute();
            var relativePath = absolutePath.RelativeTo(environment.RepositoryPath);
            var projectPath = absolutePath.RelativeTo(environment.UnityProjectPath);

            return new GitStatusEntry(relativePath, absolutePath, projectPath, status, originalPath?.ToNPath(), staged);
        }

        public GitLock CreateGitLock(string path, string user, int id)
        {
            var npath = new NPath(path).MakeAbsolute();
            var fullPath = npath.RelativeTo(environment.RepositoryPath);

            return new GitLock(path, fullPath, user, id);
        }
    }
}
