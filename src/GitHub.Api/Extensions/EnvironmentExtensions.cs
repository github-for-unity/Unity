using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Unity
{
    static class EnvironmentExtensions
    {
        public static NPath GetRepositoryPath(this IEnvironment environment, NPath path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            NPath projectPath = environment.UnityProjectPath;
            NPath repositoryPath = environment.RepositoryPath;
            if (projectPath == repositoryPath)
            {
                return path;
            }

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new InvalidOperationException($"RepositoryPath:\"{repositoryPath}\" should not be child of ProjectPath:\"{projectPath}\"");
            }

            return projectPath.RelativeTo(repositoryPath).Combine(path);
        }

        public static NPath GetAssetPath(this IEnvironment environment, NPath path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            NPath projectPath = environment.UnityProjectPath;
            NPath repositoryPath = environment.RepositoryPath;
            if (projectPath == repositoryPath)
            {
                return path;
            }

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new InvalidOperationException($"RepositoryPath:\"{repositoryPath}\" should not be child of ProjectPath:\"{projectPath}\"");
            }

            return repositoryPath.Combine(path).MakeAbsolute().RelativeTo(projectPath);
        }

        public static IEnumerable<NPath> ToNPathList(this string envPath, IEnvironment environment)
        {
            return envPath
                    .Split(Path.PathSeparator)
                    .Where(x => x != null)
                    .Select(x => environment.ExpandEnvironmentVariables(x.Trim('"', '\'')))
                    .Where(x => !String.IsNullOrEmpty(x))
                    .Select(x => x.ToNPath());
        }
    }
}
