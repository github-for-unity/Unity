using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    interface IGitClient
    {
        ITask<GitStatus?> Status(IOutputProcessor<GitStatus?> processor = null, ITask dependsOn = null);

        ITask<string> GetConfig(string key, GitConfigSource configSource,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> SetConfig(string key, string value, GitConfigSource configSource,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<List<GitLock>> ListLocks(bool local,
            BaseOutputListProcessor<GitLock> processor = null, ITask dependsOn = null);

        ITask<string> Pull(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> SwitchBranch(string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Add(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Remove(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> AddAndCommit(List<string> files, string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Lock(string file,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null, ITask dependsOn = null);
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

        public ITask<GitStatus?> Status(IOutputProcessor<GitStatus?> processor = null, ITask dependsOn = null)
        {
            return new GitStatusTask(new GitObjectFactory(environment), cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null, ITask dependsOn = null)
        {
            return new GitLogTask(new GitObjectFactory(environment), cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> GetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitConfigGetTask(key, configSource, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> SetConfig(string key, string value, GitConfigSource configSource, IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitConfigSetTask(key, value, configSource, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<List<GitLock>> ListLocks(bool local, BaseOutputListProcessor<GitLock> processor = null, ITask dependsOn = null)
        {
            return new GitListLocksTask(new GitObjectFactory(environment), local, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Pull(string remote, string branch, IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitPullTask(remote, branch, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitPushTask(remote, branch, true, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitFetchTask(remote, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> SwitchBranch(string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitSwitchBranchesTask(branch, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitBranchDeleteTask(branch, deleteUnmerged, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitBranchCreateTask(branch, baseBranch, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoteAddTask(remote, url, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoteRemoveTask(remote, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoteChangeTask(remote, url, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitCommitTask(message, body, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Add(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitAddTask(files, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Remove(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoveFromIndexTask(files, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> AddAndCommit(List<string> files, string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return Add(files)
                .ContinueWith(new GitCommitTask(body, message, cancellationToken)
                    .ConfigureGitProcess(processManager));
        }

        public ITask<string> Lock(string file,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitLockTask(file, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        public ITask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitUnlockTask(file, force, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<GitClient>();
    }
}
