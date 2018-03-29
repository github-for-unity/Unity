using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IGitClient
    {
        ITask<ValidateGitInstallResult> ValidateGitInstall(NPath path, bool isCustomGit);

        ITask Init(IOutputProcessor<string> processor = null);

        ITask LfsInstall(IOutputProcessor<string> processor = null);

        ITask<GitAheadBehindStatus> AheadBehindStatus(string gitRef, string otherRef, 
            IOutputProcessor<GitAheadBehindStatus> processor = null);

        ITask<GitStatus> Status(IOutputProcessor<GitStatus> processor = null);

        ITask<string> GetConfig(string key, GitConfigSource configSource,
            IOutputProcessor<string> processor = null);

        ITask<string> SetConfig(string key, string value, GitConfigSource configSource,
            IOutputProcessor<string> processor = null);

        ITask<GitUser> GetConfigUserAndEmail();

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

        ITask<string> AddAll(IOutputProcessor<string> processor = null);

        ITask<string> Discard(IList<string> files,
            IOutputProcessor<string> processor = null);

        ITask<string> DiscardAll(IOutputProcessor<string> processor = null);

        ITask<string> Remove(IList<string> files,
            IOutputProcessor<string> processor = null);

        ITask<string> AddAndCommit(IList<string> files, string message, string body,
            IOutputProcessor<string> processor = null);

        ITask<string> Lock(string file,
            IOutputProcessor<string> processor = null);

        ITask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null);

        ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null);

        ITask<Version> Version(IOutputProcessor<Version> processor = null);

        ITask<Version> LfsVersion(IOutputProcessor<Version> processor = null);

        ITask<GitUser> SetConfigNameAndEmail(string username, string email);
    }

    class GitClient : IGitClient
    {
        private const string UserNameConfigKey = "user.name";
        private const string UserEmailConfigKey = "user.email";
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly CancellationToken cancellationToken;

        public GitClient(IEnvironment environment, IProcessManager processManager, CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.cancellationToken = cancellationToken;
        }

        public ITask<ValidateGitInstallResult> ValidateGitInstall(NPath path, bool isCustomGit)
        {
            if (!path.FileExists())
            {
                return new FuncTask<ValidateGitInstallResult>(TaskEx.FromResult(new ValidateGitInstallResult(false, null, null)));
            }

            Version gitVersion = null;
            Version gitLfsVersion = null;

            var endTask = new FuncTask<ValidateGitInstallResult>(cancellationToken,
                () => new ValidateGitInstallResult(
                    gitVersion?.CompareTo(Constants.MinimumGitVersion) >= 0 &&
                    gitLfsVersion?.CompareTo(Constants.MinimumGitLfsVersion) >= 0,
                    gitVersion, gitLfsVersion));
              
            var gitLfsVersionTask = new GitLfsVersionTask(cancellationToken)
                .Configure(processManager, path, dontSetupGit: isCustomGit);
            
            gitLfsVersionTask
                .Then((result, version) => {return gitLfsVersion = version;})
                .Then(endTask, taskIsTopOfChain: true);

            gitLfsVersionTask.Then(endTask, TaskRunOptions.OnFailure, taskIsTopOfChain:true);

            var gitVersionTask = new GitVersionTask(cancellationToken)
                .Configure(processManager, path, dontSetupGit: isCustomGit);

            gitVersionTask
                .Then((result, version) => { return gitVersion = version; })
                .Then(gitLfsVersionTask, taskIsTopOfChain: true);
            
            gitVersionTask.Then(endTask, TaskRunOptions.OnFailure, taskIsTopOfChain:true);

            return endTask;
        }

        public ITask Init(IOutputProcessor<string> processor = null)
        {
            return new GitInitTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask LfsInstall(IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("LfsInstall");

            return new GitLfsInstallTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<GitStatus> Status(IOutputProcessor<GitStatus> processor = null)
        {
            //Logger.Trace("Status");

            return new GitStatusTask(new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<GitAheadBehindStatus> AheadBehindStatus(string gitRef, string otherRef, IOutputProcessor<GitAheadBehindStatus> processor = null)
        {
            //Logger.Trace("AheadBehindStatus");

            return new GitAheadBehindStatusTask(gitRef, otherRef, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null)
        {
            //Logger.Trace("Log");

            return new GitLogTask(new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<Version> Version(IOutputProcessor<Version> processor = null)
        {
            //Logger.Trace("Version");

            return new GitVersionTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<Version> LfsVersion(IOutputProcessor<Version> processor = null)
        {
            //Logger.Trace("LfsVersion");

            return new GitLfsVersionTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> GetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("GetConfig: {0}", key);

            return new GitConfigGetTask(key, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> SetConfig(string key, string value, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("SetConfig");

            return new GitConfigSetTask(key, value, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<GitUser> GetConfigUserAndEmail()
        {
            string username = null;
            string email = null;

            return GetConfig(UserNameConfigKey, GitConfigSource.User)
                .Then((success, value) => {
                    if (success)
                    {
                        username = value;
                    }
                })
                .Then(GetConfig(UserEmailConfigKey, GitConfigSource.User)
                    .Then((success, value) => {
                        if (success)
                        {
                            email = value;
                        }
                    })).Then(success => {
                //Logger.Trace("{0}:{1} {2}:{3}", UserNameConfigKey, username, UserEmailConfigKey, email);
                return new GitUser(username, email);
            });
        }

        public ITask<GitUser> SetConfigNameAndEmail(string username, string email)
        {
            return SetConfig(UserNameConfigKey, username, GitConfigSource.User)
                .Then(SetConfig(UserEmailConfigKey, email, GitConfigSource.User))
                .Then(b => new GitUser(username, email));
        }

        public ITask<List<GitLock>> ListLocks(bool local, BaseOutputListProcessor<GitLock> processor = null)
        {
            //Logger.Trace("ListLocks");

            return new GitListLocksTask(new GitObjectFactory(environment), local, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Pull(string remote, string branch, IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Pull");

            return new GitPullTask(remote, branch, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Push");

            return new GitPushTask(remote, branch, true, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Revert(string changeset, IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Revert");

            return new GitRevertTask(changeset, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Fetch");

            return new GitFetchTask(remote, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> SwitchBranch(string branch, IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("SwitchBranch");

            return new GitSwitchBranchesTask(branch, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("DeleteBranch");

            return new GitBranchDeleteTask(branch, deleteUnmerged, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("CreateBranch");

            return new GitBranchCreateTask(branch, baseBranch, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("RemoteAdd");

            return new GitRemoteAddTask(remote, url, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("RemoteRemove");

            return new GitRemoteRemoveTask(remote, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("RemoteChange");

            return new GitRemoteChangeTask(remote, url, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Commit");

            return new GitCommitTask(message, body, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> AddAll(IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Add all files");

            return new GitAddTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Add(IList<string> files,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Add Files");

            GitAddTask last = null;
            foreach (var batch in files.Spool(5000))
            {
                var current = new GitAddTask(batch, cancellationToken, processor).Configure(processManager);
                if (last == null)
                {
                    last = current;
                }
                else
                {
                    last.Then(current);
                    last = current;
                }
            }

            return last;
        }

        public ITask<string> Discard( IList<string> files,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Checkout Files");

            GitCheckoutTask last = null;
            foreach (var batch in files.Spool(5000))
            {
                var current = new GitCheckoutTask(batch, cancellationToken, processor).Configure(processManager);
                if (last == null)
                {
                    last = current;
                }
                else
                {
                    last.Then(current);
                    last = current;
                }
            }

            return last;
        }

        public ITask<string> DiscardAll(IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Checkout all files");

            return new GitCheckoutTask(cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Remove(IList<string> files,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Remove");

            return new GitRemoveFromIndexTask(files, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> AddAndCommit(IList<string> files, string message, string body,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("AddAndCommit");

            return Add(files)
                .Then(new GitCommitTask(message, body, cancellationToken)
                    .Configure(processManager));
        }

        public ITask<string> Lock(string file,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Lock");

            return new GitLockTask(file, cancellationToken, processor)
                .Configure(processManager);
        }

        public ITask<string> Unlock(string file, bool force,
            IOutputProcessor<string> processor = null)
        {
            //Logger.Trace("Unlock");

            return new GitUnlockTask(file, force, cancellationToken, processor)
                .Configure(processManager);
        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<GitClient>();
    }

    public struct GitUser
    {
        public static GitUser Default = new GitUser();

        public string name;
        public string email;

        public string Name { get { return name; } }
        public string Email { get { return email; } }

        public GitUser(string name, string email)
        {
            this.name = name;
            this.email = email;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + (email?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitUser)
                return Equals((GitUser)other);
            return false;
        }

        public bool Equals(GitUser other)
        {
            return
                String.Equals(name, other.name) &&
                String.Equals(email, other.email)
                ;
        }

        public static bool operator ==(GitUser lhs, GitUser rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitUser lhs, GitUser rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return $"Name:\"{Name}\" Email:\"{Email}\"";
        }
    }
}
