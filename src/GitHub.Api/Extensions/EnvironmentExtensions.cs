using System;

namespace GitHub.Unity
{
    static class EnvironmentExtensions
    {
        public static string GetRepositoryPath(this IEnvironment environment, string path)
        {
            Guard.ArgumentNotNull(path, nameof(path));
            if (environment.UnityProjectPath == environment.RepositoryPath)
            {
                return path;
            }

            var projectPath = environment.UnityProjectPath.ToNPath();
            var repositoryPath = environment.RepositoryPath.ToNPath();

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new Exception($"RepositoryPath:\"{projectPath}\" should not be child or ProjectPath:\"{repositoryPath}\"");
            }

            return projectPath.RelativeTo(repositoryPath).Combine(path).ToString();
        }

        public static string GetAssetPath(this IEnvironment environment, string path)
        {
            Guard.ArgumentNotNull(path, nameof(path));
            if (environment.UnityProjectPath == environment.RepositoryPath)
            {
                return path;
            }

            var projectPath = environment.UnityProjectPath.ToNPath();
            var repositoryPath = environment.RepositoryPath.ToNPath();

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new Exception($"RepositoryPath:\"{projectPath}\" should not be child or ProjectPath:\"{repositoryPath}\"");
            }

            return repositoryPath.Combine(path).MakeAbsolute().RelativeTo(projectPath);
        }
    }
}
