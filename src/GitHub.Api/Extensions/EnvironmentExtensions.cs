using System;

namespace GitHub.Unity
{
    static class EnvironmentExtensions
    {
        public static string GetRepositoryPath(this IEnvironment environment, string assetPath)
        {
            Guard.ArgumentNotNull(assetPath, nameof(assetPath));
            if (environment.UnityProjectPath == environment.RepositoryPath)
            {
                return assetPath;
            }

            var projectPath = environment.UnityProjectPath.ToNPath();
            var repositoryPath = environment.RepositoryPath.ToNPath();

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new Exception($"RepositoryPath:\"{projectPath}\" should not be child or ProjectPath:\"{repositoryPath}\"");
            }

            return projectPath.RelativeTo(repositoryPath).Combine(assetPath).ToString();
        }
    }
}
