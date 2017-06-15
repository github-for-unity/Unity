using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IGitClient
    {
        Task<NPath> FindGitInstallation();
        bool ValidateGitInstall(NPath path);

        ITask Init(IOutputProcessor<string> processor = null);

        ITask LfsInstall(IOutputProcessor<string> processor = null);

        ITask<GitStatus?> Status(IOutputProcessor<GitStatus?> processor = null);

        ITask<string> GetConfig(string key, GitConfigSource configSource,
            IOutputProcessor<string> processor = null);

        ITask<string> SetConfig(string key, string value, GitConfigSource configSource,
            IOutputProcessor<string> processor = null);

        ITask<List<GitLock>> ListLocks(bool local,
            BaseOutputListProcessor<GitLock> processor = null);

        ITask<string> Pull(string remote, string branch,
            IOutputProcessor<string> processor = null);

        ITask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null);

        ITask<string> Revert(string changeset,
            IOutputProcessor<string> processor = null);

        ITask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null);

        ITask<string> SwitchBranch(string branch,
            IOutputProcessor<string> processor = null);

        ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null);

        ITask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null);

        ITask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null);

        ITask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null);

        ITask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null);

        ITask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null);

        ITask<string> Add(IList<string> files,
            IOutputProcessor<string> processor = null);

        ITask<string> Remove(IList<string> files,
            IOutputProcessor<string> processor = null);

        ITask<string> AddAndCommit(IList<string> files, string message, string body,
            IOutputProcessor<string> processor = null);

        ITask<string> Lock(string file,
            IOutputProcessor<string> processor = null);

        ITask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null);

        ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null);
    }

    class GitClient : IGitClient
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ICredentialManager credentialManager;
        private readonly ITaskManager taskManager;
        private readonly CancellationToken cancellationToken;


        public GitClient(IEnvironment environment, IProcessManager processManager,
            ICredentialManager credentialManager, ITaskManager taskManager)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.credentialManager = credentialManager;
            this.taskManager = taskManager;
            this.cancellationToken = taskManager.Token;
        }

        public async Task<NPath> FindGitInstallation()
        {
            if (!String.IsNullOrEmpty(environment.GitExecutablePath))
                return environment.GitExecutablePath;

            var path = await LookForPortableGit();
            if (path == null)
                path = await LookForSystemGit();

            Logger.Trace("Git Installation folder {0} discovered: '{1}'", path == null ? "not" : "", path);

            return path;
        }

        private Task<NPath> LookForPortableGit()
        {
            var gitHubLocalAppDataPath = environment.UserCachePath;
            if (!gitHubLocalAppDataPath.DirectoryExists())
                return null;

            var searchPath = "PortableGit_";

            var portableGitPath = gitHubLocalAppDataPath.Directories()
                .Where(s => s.FileName.StartsWith(searchPath, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();

            if (portableGitPath != null)
            {
                portableGitPath = portableGitPath.Combine("cmd", $"git{environment.ExecutableExtension}");
            }

            return TaskEx.FromResult(portableGitPath);
        }

        private async Task<NPath> LookForSystemGit()
        {
            if (environment.IsMac)
            {
                var path = "/usr/local/bin/git".ToNPath();
                if (path.FileExists())
                    return path;
            }
            return await new FindExecTask("git", taskManager.Token).StartAwait();
        }

        public bool ValidateGitInstall(NPath path)
        {
            return path.FileExists();
        }

        public ITask Init(IOutputProcessor<string> processor = null)
        {
            return new GitInitTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask LfsInstall(IOutputProcessor<string> processor = null)
        {
            return new GitLfsInstallTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<GitStatus?> Status(IOutputProcessor<GitStatus?> processor = null)
        {
            return new GitStatusTask(new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null)
        {
            return new GitLogTask(new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> GetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            return new GitConfigGetTask(key, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> SetConfig(string key, string value, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            return new GitConfigSetTask(key, value, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<List<GitLock>> ListLocks(bool local, BaseOutputListProcessor<GitLock> processor = null)
        {
            return new GitListLocksTask(new GitObjectFactory(environment), local, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Pull(string remote, string branch, IOutputProcessor<string> processor = null)
        {
            return new GitPullTask(remote, branch, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null)
        {
            return new GitPushTask(remote, branch, true, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Revert(string changeset, IOutputProcessor<string> processor = null)
        {
            return new GitRevertTask(changeset, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null)
        {
            return new GitFetchTask(remote, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> SwitchBranch(string branch, IOutputProcessor<string> processor = null)
        {
            return new GitSwitchBranchesTask(branch, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null)
        {
            return new GitBranchDeleteTask(branch, deleteUnmerged, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null)
        {
            return new GitBranchCreateTask(branch, baseBranch, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoteAddTask(remote, url, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoteRemoveTask(remote, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoteChangeTask(remote, url, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null)
        {
            return new GitCommitTask(message, body, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Add(IList<string> files,
            IOutputProcessor<string> processor = null)
        {
            return new GitAddTask(files, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Remove(IList<string> files,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoveFromIndexTask(files, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> AddAndCommit(IList<string> files, string message, string body,
            IOutputProcessor<string> processor = null)
        {
            return Add(files)
                .Then(new GitCommitTask(message, body, cancellationToken)
                    .Configure(processManager));
        }

        public ITask<string> Lock(string file,
            IOutputProcessor<string> processor = null)
        {
            return new GitLockTask(file, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null)
        {
            return new GitUnlockTask(file, force, cancellationToken, processor)
                .Configure(processManager);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<GitClient>();
    }
}
