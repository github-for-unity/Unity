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
    }
}
