using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    public class GitObjectFactory : IGitObjectFactory
    {
        private readonly IEnvironment environment;

        public GitObjectFactory(IEnvironment environment)
        {
            this.environment = environment;
        }

        public GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus indexStatus, GitFileStatus workTreeStatus = GitFileStatus.None, string originalPath = null)
        {
            var absolutePath = new NPath(path).MakeAbsolute();
            var relativePath = absolutePath.RelativeTo(environment.RepositoryPath);
            var projectPath = absolutePath.RelativeTo(environment.UnityProjectPath);

            return new GitStatusEntry(relativePath, absolutePath, projectPath, indexStatus, workTreeStatus, originalPath?.ToNPath());
        }
    }
}
