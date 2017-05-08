using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    interface IGitClient
    {
        FuncTask<GitStatus?> Status(IOutputProcessor<GitStatus?> processor = null, ITask dependsOn = null);

        FuncTask<string> GetConfig(string key, GitConfigSource configSource,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> SetConfig(string key, string value, GitConfigSource configSource,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncListTask<GitLock> ListLocks(bool local,
            BaseOutputListProcessor<GitLock> processor = null, ITask dependsOn = null);

        FuncTask<string> Pull(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> SwitchBranch(string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Add(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Remove(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> AddAndCommit(List<string> files, string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Lock(string file,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncTask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null, ITask dependsOn = null);

        FuncListTask<GitLogEntry> Log(BaseOutputListProcessor<GitLogEntry> processor = null, ITask dependsOn = null);
    }

    class GitClient : IGitClient
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ICredentialManager credentialManager;
        private readonly ITaskManager taskManager;
        private readonly CancellationToken cancellationToken;


        public GitClient(IEnvironment environment, IProcessManager processManager,
            ICredentialManager credentialManager, ITaskManager taskManager,
            CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.credentialManager = credentialManager;
            this.taskManager = taskManager;
            this.cancellationToken = cancellationToken;
        }

        public FuncTask<GitStatus?> Status(IOutputProcessor<GitStatus?> processor = null, ITask dependsOn = null)
        {
            return new GitStatusTask(new GitObjectFactory(environment), cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncListTask<GitLogEntry> Log(BaseOutputListProcessor<GitLogEntry> processor = null, ITask dependsOn = null)
        {
            return new GitLogTask(new GitObjectFactory(environment), cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> GetConfig(string key, GitConfigSource configSource,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitConfigGetTask(key, configSource, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> SetConfig(string key, string value, GitConfigSource configSource,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitConfigSetTask(key, value, configSource, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncListTask<GitLock> ListLocks(bool local,
            BaseOutputListProcessor<GitLock> processor = null, ITask dependsOn = null)
        {
            return new GitListLocksTask(new GitObjectFactory(environment), local, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Pull(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitPullTask(remote, branch, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitPushTask(remote, branch, true, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitFetchTask(remote, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> SwitchBranch(string branch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitSwitchBranchesTask(branch, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitBranchDeleteTask(branch, deleteUnmerged, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitBranchCreateTask(branch, baseBranch, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoteAddTask(remote, url, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoteRemoveTask(remote, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoteChangeTask(remote, url, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitCommitTask(message, body, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Add(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitAddTask(files, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Remove(List<string> files,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitRemoveFromIndexTask(files, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> AddAndCommit(List<string> files, string message, string body,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return Add(files)
                .Then(new GitCommitTask(body, message, cancellationToken)
                    .ConfigureGitProcess(processManager));
        }

        public FuncTask<string> Lock(string file,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitLockTask(file, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        public FuncTask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null, ITask dependsOn = null)
        {
            return new GitUnlockTask(file, force, cancellationToken, processor, dependsOn)
                .ConfigureGitProcess(processManager)
                .Schedule(taskManager);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<GitClient>();
    }
}
