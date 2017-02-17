using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitOtherClient
    {
        private readonly IEnvironment environment;
        private readonly CancellationToken cancellationToken;

        public GitOtherClient(IEnvironment environment, CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.cancellationToken = cancellationToken;
        }
        public void SetupIfNeeded(IProgress<float> percentage, IProgress<long> timeRemaining)
        {
            var gitInstaller = new PortableGitManager(environment, cancellationToken: cancellationToken);
            gitInstaller.ExtractGitIfNeeded(percentage, timeRemaining);

        }
    }

    class GitClient : IGitClient, IDisposable
    {
        private static string[] emptyContents = new string[0];
        private static Func<string, string[]> fileReadAllLines = s => { try { return File.ReadAllLines(s); } catch { return emptyContents; } };

        private readonly NPath dotGitPath;
        private readonly NPath refsPath;
        private readonly GitConfig config;

        private readonly Dictionary<string, ConfigBranch> branches = new Dictionary<string, ConfigBranch>();

        private FileSystemWatcher headWatcher;
        private FileSystemWatcher branchesWatcher;
        private string head;
        private ConfigBranch? activeBranch;

        public GitClient(string localPath)
        {
            Guard.ArgumentNotNullOrWhiteSpace(localPath, nameof(localPath));

            var path = localPath.ToNPath();
            if (path.IsRelative)
                throw new InvalidOperationException("GitClient localPath has to be absolute");

            path = FindRepositoryRoot(path);

            if (path != null)
            {
                RepositoryPath = path;
                dotGitPath = path.Combine(".git");
                refsPath = dotGitPath.Combine("refs", "heads");

                if (dotGitPath.FileExists())
                {
                    dotGitPath = dotGitPath.ReadAllLines()
                        .Where(x => x.StartsWith("gitdir:"))
                        .Select(x => x.Substring(7).Trim())
                        .First();
                }
                config = new GitConfig(dotGitPath.Combine("config"));
            }
        }

        public void Start()
        {
            LoadBranches(refsPath, config.GetBranches().Where(x => x.IsTracking), "");
            RefreshCurrentBranch();

            headWatcher = new FileSystemWatcher();
            headWatcher.Path = dotGitPath;
            headWatcher.Filter = "HEAD";
            headWatcher.NotifyFilter = NotifyFilters.LastWrite;
            headWatcher.Changed += (s, e) => RefreshCurrentBranch();
            headWatcher.EnableRaisingEvents = true;

            branchesWatcher = new FileSystemWatcher();
            branchesWatcher.Path = refsPath;
            branchesWatcher.IncludeSubdirectories = true;
            branchesWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
            branchesWatcher.Renamed += (s, e) => UpdateBranchList(e.Name, e.OldName);
            branchesWatcher.Created += (s, e) => UpdateBranchList(e.Name);
            branchesWatcher.Deleted += (s, e) => UpdateBranchList(null, e.Name);
            branchesWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            headWatcher.EnableRaisingEvents = false;
            branchesWatcher.EnableRaisingEvents = false;
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
            return new Repository(this, new NPath(RepositoryPath).FileName, cloneUrl, RepositoryPath);
        }

        public ConfigRemote? GetActiveRemote(string defaultRemote = "origin")
        {
            if (RepositoryPath == null)
                return null;

            var branch = ActiveBranch;
            if (branch.HasValue)
                return branch.Value.Remote;
            var remote = config.GetRemote(defaultRemote);
            if (remote.HasValue)
                return remote;
            return config.GetRemotes().FirstOrDefault();
        }

        private void LoadBranches(NPath path, IEnumerable<ConfigBranch> configBranches, string prefix)
        {
            foreach (var file in path.Files())
            {
                var branchName = prefix + file.FileName;
                var branch = configBranches.Where(x => x.Name == branchName).Select(x => x as ConfigBranch?).FirstOrDefault();
                if (!branch.HasValue)
                    branch = new ConfigBranch { Name = branchName };
                branches.Add(branchName, branch.Value);
            }

            foreach (var dir in path.Directories())
            {
                LoadBranches(dir, configBranches, prefix + dir.FileName + "/");
            }
        }

        private void UpdateBranchList(string name = null, string oldName = null)
        {
            name = name != null ? new NPath(name).MakeAbsolute().RelativeTo(refsPath).ToString(SlashMode.Forward) : null;
            oldName = oldName != null ? new NPath(oldName).MakeAbsolute().RelativeTo(refsPath).ToString(SlashMode.Forward) : null;

            // creation
            if (name != null && oldName == null)
            {
                AddBranch(name);
            }
            // deletion
            else if (name == null && oldName != null)
            {
                RemoveBranch(oldName);
            }
            // renaming
            else if (name != null && oldName != null)
            {
                RemoveBranch(oldName);
                AddBranch(name);
            }
            RefreshCurrentBranch();
        }

        private void RemoveBranch(string oldName)
        {
            if (branches.ContainsKey(oldName))
                branches.Remove(oldName);
        }

        private void AddBranch(string name)
        {
            if (!branches.ContainsKey(name))
            {
                var branch = config.GetBranch(name);
                if (!branch.HasValue)
                    branch = new ConfigBranch { Name = name };
                branches.Add(name, branch.Value);
            }
        }

        private ConfigBranch? GetBranch(string name)
        {
            if (branches.ContainsKey(name))
                return branches[name];
            return null;
        }

        private void RefreshCurrentBranch()
        {
            if (RepositoryPath == null)
                return;

            head = GetHead();
            if (head.StartsWith("ref:"))
            {
                var branch = head.Substring(head.IndexOf("refs/heads/") + "refs/heads/".Length);
                activeBranch = GetBranch(branch);
            }
            else
                activeBranch = null;
        }

        private string GetHead()
        {
            return dotGitPath.Combine("HEAD")
                .ReadAllLines()
                .FirstOrDefault();
        }

        private static NPath FindRepositoryRoot(NPath path)
        {
            if (path.Exists(".git"))
                return path;

            if (!path.IsRoot)
                return FindRepositoryRoot(path.Parent);

            return null;
        }

        bool disposed;
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                branchesWatcher?.Dispose();
                headWatcher?.Dispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<GitClient>();
        public string RepositoryPath { get; }
        public ConfigBranch? ActiveBranch => activeBranch;
    }
}