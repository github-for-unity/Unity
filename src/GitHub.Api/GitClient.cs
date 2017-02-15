using System;
using System.IO;
using System.Linq;
using GitHub.Unity;

namespace GitHub.Api
{
    class GitClient : IGitClient
    {
        private static string[] emptyContents = new string[0];
        private static Func<string, string[]> fileReadAllLines = s => { try { return File.ReadAllLines(s); } catch { return emptyContents; } };

        public string RepositoryPath { get; }
        private readonly string dotGitPath;
        private readonly IFileSystem fs;
        private readonly IProcessManager processManager;
        private readonly GitConfig config;

        public GitClient(string localPath, IFileSystem fs, IProcessManager processManager)
        {
            Guard.ArgumentNotNullOrWhiteSpace(localPath, nameof(localPath));
            Guard.ArgumentNotNull(fs, nameof(fs));
            Guard.ArgumentNotNull(processManager, nameof(processManager));

            if (!Path.IsPathRooted(localPath))
                localPath = fs.GetFullPath(localPath);
            var path = FindRepositoryRoot(fs, localPath, Path.GetPathRoot(localPath));

            if (path != null)
            {
                RepositoryPath = path;
                this.dotGitPath = Path.Combine(RepositoryPath, ".git");
                this.fs = fs;
                this.processManager = processManager;
                if (fs.FileExists(dotGitPath))
                {
                    dotGitPath = fileReadAllLines(dotGitPath)
                        .Where(x => x.StartsWith("gitdir:"))
                        .Select(x => x.Substring(7).Trim())
                        .First();
                }
                config = new GitConfig(Path.Combine(dotGitPath, "config"));
            }
        }

        public IRepository GetRepository()
        {
            if (RepositoryPath == null)
                return null;

            var remote = config.GetRemotes()
                               .Where(x => HostAddress.Create(new UriString(x.Url).ToRepositoryUri()).IsGitHubDotCom())
                               .FirstOrDefault();
            UriString cloneUrl = "";
            if (remote.Url != null)
                cloneUrl = new UriString(remote.Url).ToRepositoryUrl();
            return new Repository(this, Path.GetDirectoryName(RepositoryPath), cloneUrl, RepositoryPath);
        }

        public ConfigRemote? GetActiveRemote(string defaultRemote = "origin")
        {
            if (RepositoryPath == null)
                return null;

            var branch = GetActiveBranch();
            if (branch.HasValue)
                return branch.Value.Remote;
            var remote = config.GetRemote(defaultRemote);
            if (remote.HasValue)
                return remote;
            return config.GetRemotes().FirstOrDefault();
        }

        private string GetHead()
        {
            return fileReadAllLines(Path.Combine(dotGitPath, "HEAD"))
                .FirstOrDefault();
        }

        public ConfigBranch? GetActiveBranch()
        {
            if (RepositoryPath == null)
                return null;

            var head = GetHead();
            if (head.StartsWith("ref:"))
            {
                var branch = head.Substring(head.IndexOf("refs/heads/") + "refs/heads/".Length);
                return config.GetBranch(branch);
            }
            return null;
        }

        private static string FindRepositoryRoot(IFileSystem fs, string path, string root)
        {
            var dotgit = Path.Combine(path, ".git");
            if (fs.DirectoryExists(dotgit) || fs.FileExists(dotgit))
            {
                return path;
            }
            if (path != root)
            {
                return FindRepositoryRoot(fs, fs.GetDirectoryName(path), root);
            }
            return null;
        }
    }
}