using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using static GitHub.Unity.GitInstaller;

namespace GitHub.Unity
{
    /// <summary>
    /// Client that provides access to git functionality
    /// </summary>
    public interface IGitClient
    {
        /// <summary>
        /// Executes `git init` to initialize a git repo.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Init(IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git lfs install` to install LFS hooks.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> LfsInstall(IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git rev-list` to determine the ahead/behind status between two refs.
        /// </summary>
        /// <param name="gitRef">Ref to compare</param>
        /// <param name="otherRef">Ref to compare against</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="GitAheadBehindStatus"/> output</returns>
        ITask<GitAheadBehindStatus> AheadBehindStatus(string gitRef, string otherRef, IOutputProcessor<GitAheadBehindStatus> processor = null);

        /// <summary>
        /// Executes `git status` to determine the working directory status.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="GitStatus"/> output</returns>
        ITask<GitStatus> Status(IOutputProcessor<GitStatus> processor = null);

        /// <summary>
        /// Executes `git config get` to get a configuration value.
        /// </summary>
        /// <param name="key">The configuration key to get</param>
        /// <param name="configSource">The config source (unspecified, local,user,global) to use</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> GetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git config set` to set a configuration value.
        /// </summary>
        /// <param name="key">The configuration key to set</param>
        /// <param name="value">The value to set</param>
        /// <param name="configSource">The config source (unspecified, local,user,global) to use</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> SetConfig(string key, string value, GitConfigSource configSource, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git config --unset` to remove a configuration value.
        /// </summary>
        /// <param name="key">The configuration key to remove</param>
        /// <param name="configSource">The config source (unspecified, local,user,global) to use</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> UnSetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes two `git config get` commands to get the git user and email.
        /// </summary>
        /// <returns><see cref="GitUser"/> output</returns>
        ITask<GitUser> GetConfigUserAndEmail();

        /// <summary>
        /// Executes `git lfs locks` to get a list of lfs locks from the git lfs server.
        /// </summary>
        /// <param name="local"></param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="List&lt;T&gt;"/> of <see cref="GitLock"/> output</returns>
        ITask<List<GitLock>> ListLocks(bool local, BaseOutputListProcessor<GitLock> processor = null);

        /// <summary>
        /// Executes `git pull` to perform a pull operation.
        /// </summary>
        /// <param name="remote">The remote to pull from</param>
        /// <param name="branch">The branch to pull</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Pull(string remote, string branch, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git push` to perform a push operation.
        /// </summary>
        /// <param name="remote">The remote to push to</param>
        /// <param name="branch">The branch to push</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Push(string remote, string branch, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git revert` to perform a revert operation.
        /// </summary>
        /// <param name="changeset">The changeset to revert</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Revert(string changeset, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git fetch` to perform a fetch operation.
        /// </summary>
        /// <param name="remote">The remote to fetch from</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Fetch(string remote, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git checkout` to switch branches.
        /// </summary>
        /// <param name="branch">The branch to checkout</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> SwitchBranch(string branch, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git branch -d` to delete a branch.
        /// </summary>
        /// <param name="branch">The branch to delete</param>
        /// <param name="deleteUnmerged">The flag to indicate the branch should be deleted even if not merged</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git branch` to create a branch.
        /// </summary>
        /// <param name="branch">The name of branch to create</param>
        /// <param name="baseBranch">The name of branch to create from</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> CreateBranch(string branch, string baseBranch, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git remote add` to add a git remote.
        /// </summary>
        /// <param name="remote">The remote to add</param>
        /// <param name="url">The url of the remote</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> RemoteAdd(string remote, string url, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git remote rm` to remove a git remote.
        /// </summary>
        /// <param name="remote">The remote to remove</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> RemoteRemove(string remote, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git remote set-url` to change the url of a git remote.
        /// </summary>
        /// <param name="remote">The remote to change</param>
        /// <param name="url">The url to change to</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> RemoteChange(string remote, string url, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git commit` to perform a commit operation.
        /// </summary>
        /// <param name="message">The commit message summary</param>
        /// <param name="body">The commit message body</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Commit(string message, string body, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes at least one `git add` command to add the list of files to the git index.
        /// </summary>
        /// <param name="files">The file to add</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Add(IList<string> files, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git add -A` to add all files to the git index.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> AddAll(IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes at least one `git checkout` command to discard changes to the list of files.
        /// </summary>
        /// <param name="files">The files to discard</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Discard(IList<string> files, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git checkout -- .` to discard all changes in the working directory.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> DiscardAll(IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes at least one `git checkout` command to checkout files at the given changeset
        /// </summary>
        /// <param name="changeset">The md5 of the changeset</param>
        /// <param name="files">The files to check out</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> CheckoutVersion(string changeset, IList<string> files, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes at least one `git reset HEAD` command to remove files from the git index.
        /// </summary>
        /// <param name="files">The files to remove</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Remove(IList<string> files, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes at least one `git add` command to add the list of files to the git index. Followed by a `git commit` command to commit the changes.
        /// </summary>
        /// <param name="files">The files to add and commit</param>
        /// <param name="message">The commit message summary</param>
        /// <param name="body">The commit message body</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> AddAndCommit(IList<string> files, string message, string body, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git lfs lock` to lock a file.
        /// </summary>
        /// <param name="file">The file to lock</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Lock(NPath file, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git lfs unlock` to unlock a file.
        /// </summary>
        /// <param name="file">The file to unlock</param>
        /// <param name="force">If force should be used</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Unlock(NPath file, bool force, IOutputProcessor<string> processor = null);

        /// <summary>
        /// Executes `git log` to get the history of the current branch.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="List&lt;T&gt;"/> of <see cref="GitLogEntry"/> output</returns>
        ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null);

        /// <summary>
        /// Executes `git log -- <file>` to get the history of a specific file.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="List&lt;T&gt;"/> of <see cref="GitLogEntry"/> output</returns>
        ITask<List<GitLogEntry>> LogFile(NPath file, BaseOutputListProcessor<GitLogEntry> processor = null);

        /// <summary>
        /// Executes `git --version` to get the git version.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="TheVersion"/> output</returns>
        ITask<TheVersion> Version(IOutputProcessor<TheVersion> processor = null);

        /// <summary>
        /// Executes `git lfs version` to get the git lfs version.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="TheVersion"/> output</returns>
        ITask<TheVersion> LfsVersion(IOutputProcessor<TheVersion> processor = null);

        /// <summary>
        /// Executes `git count-objects` to get the size of the git repo in kilobytes.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="int"/> output</returns>
        ITask<int> CountObjects(IOutputProcessor<int> processor = null);

        /// <summary>
        /// Executes two `git set config` commands to set the git name and email.
        /// </summary>
        /// <param name="username">The username to set</param>
        /// <param name="email">The email to set</param>
        /// <returns><see cref="GitUser"/> output</returns>
        ITask<GitUser> SetConfigNameAndEmail(string username, string email);

        /// <summary>
        /// Executes `git rev-parse --short HEAD` to get the current commit sha of the current branch.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> GetHead(IOutputProcessor<string> processor = null);
    }

    public class GitClient : IGitClient
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

        ///<inheritdoc/>
        public ITask<string> Init(IOutputProcessor<string> processor = null)
        {
            return new GitInitTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> LfsInstall(IOutputProcessor<string> processor = null)
        {
            return new GitLfsInstallTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<GitStatus> Status(IOutputProcessor<GitStatus> processor = null)
        {
            return new GitStatusTask(new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<GitAheadBehindStatus> AheadBehindStatus(string gitRef, string otherRef, IOutputProcessor<GitAheadBehindStatus> processor = null)
        {
            return new GitAheadBehindStatusTask(gitRef, otherRef, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<List<GitLogEntry>> Log(BaseOutputListProcessor<GitLogEntry> processor = null)
        {
            return new GitLogTask(new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager)
                .Catch(exception => exception is ProcessException &&
                    exception.Message.StartsWith("fatal: your current branch") &&
                    exception.Message.EndsWith("does not have any commits yet"))
                .Then((success, list) => success ? list : new List<GitLogEntry>());
        }

        ///<inheritdoc/>
        public ITask<List<GitLogEntry>> LogFile(NPath file, BaseOutputListProcessor<GitLogEntry> processor = null)
        {
            if (file == NPath.Default)
            {
                return new FuncTask<List<GitLogEntry>>(cancellationToken, () => new List<GitLogEntry>(0));
            }

            return new GitLogTask(file, new GitObjectFactory(environment), cancellationToken, processor)
                .Configure(processManager)
                .Catch(exception => exception is ProcessException &&
                    exception.Message.StartsWith("fatal: your current branch") &&
                    exception.Message.EndsWith("does not have any commits yet"))
                .Then((success, list) => success ? list : new List<GitLogEntry>());
        }

        ///<inheritdoc/>
        public ITask<TheVersion> Version(IOutputProcessor<TheVersion> processor = null)
        {
            return new GitVersionTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<TheVersion> LfsVersion(IOutputProcessor<TheVersion> processor = null)
        {
            return new GitLfsVersionTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<int> CountObjects(IOutputProcessor<int> processor = null)
        {
            return new GitCountObjectsTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> GetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            return new GitConfigGetTask(key, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> SetConfig(string key, string value, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            return new GitConfigSetTask(key, value, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> UnSetConfig(string key, GitConfigSource configSource, IOutputProcessor<string> processor = null)
        {
            return new GitConfigUnSetTask(key, configSource, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
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
                return new GitUser(username, email);
            });
        }

        ///<inheritdoc/>
        public ITask<GitUser> SetConfigNameAndEmail(string username, string email)
        {
            return SetConfig(UserNameConfigKey, username, GitConfigSource.User)
                .Then(SetConfig(UserEmailConfigKey, email, GitConfigSource.User))
                .Then(b => new GitUser(username, email));
        }

        ///<inheritdoc/>
        public ITask<List<GitLock>> ListLocks(bool local, BaseOutputListProcessor<GitLock> processor = null)
        {
            return new GitListLocksTask(local, cancellationToken, processor)
                .Configure(processManager, environment.GitLfsExecutablePath);
        }

        ///<inheritdoc/>
        public ITask<string> Pull(string remote, string branch, IOutputProcessor<string> processor = null)
        {
            return new GitPullTask(remote, branch, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Push(string remote, string branch,
            IOutputProcessor<string> processor = null)
        {
            return new GitPushTask(remote, branch, true, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Revert(string changeset, IOutputProcessor<string> processor = null)
        {
            return new GitRevertTask(changeset, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Fetch(string remote,
            IOutputProcessor<string> processor = null)
        {
            return new GitFetchTask(remote, cancellationToken, processor: processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> SwitchBranch(string branch, IOutputProcessor<string> processor = null)
        {
            return new GitSwitchBranchesTask(branch, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false,
            IOutputProcessor<string> processor = null)
        {
            return new GitBranchDeleteTask(branch, deleteUnmerged, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> CreateBranch(string branch, string baseBranch,
            IOutputProcessor<string> processor = null)
        {
            return new GitBranchCreateTask(branch, baseBranch, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> RemoteAdd(string remote, string url,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoteAddTask(remote, url, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> RemoteRemove(string remote,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoteRemoveTask(remote, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> RemoteChange(string remote, string url,
            IOutputProcessor<string> processor = null)
        {
            return new GitRemoteChangeTask(remote, url, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Commit(string message, string body,
            IOutputProcessor<string> processor = null)
        {
            return new GitCommitTask(message, body, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> AddAll(IOutputProcessor<string> processor = null)
        {
            return new GitAddTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Add(IList<string> files,
            IOutputProcessor<string> processor = null)
        {
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

        ///<inheritdoc/>
        public ITask<string> Discard( IList<string> files,
            IOutputProcessor<string> processor = null)
        {
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

        ///<inheritdoc/>
        public ITask<string> DiscardAll(IOutputProcessor<string> processor = null)
        {
            return new GitCheckoutTask(cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> CheckoutVersion(string changeset, IList<string> files, IOutputProcessor<string> processor = null)
        {
            return new GitCheckoutTask(changeset, files, cancellationToken, processor)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Remove(IList<string> files,
            IOutputProcessor<string> processor = null)
        {
            GitRemoveFromIndexTask last = null;
            foreach (var batch in files.Spool(5000))
            {
                var current = new GitRemoveFromIndexTask(batch, cancellationToken, processor).Configure(processManager);
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

        ///<inheritdoc/>
        public ITask<string> AddAndCommit(IList<string> files, string message, string body,
            IOutputProcessor<string> processor = null)
        {
            return Add(files)
                .Then(new GitCommitTask(message, body, cancellationToken)
                    .Configure(processManager));
        }

        ///<inheritdoc/>
        public ITask<string> Lock(NPath file,
            IOutputProcessor<string> processor = null)
        {
            return new GitLockTask(file, cancellationToken, processor)
                .Configure(processManager, environment.GitLfsExecutablePath);
        }

        ///<inheritdoc/>
        public ITask<string> Unlock(NPath file, bool force,
            IOutputProcessor<string> processor = null)
        {
            return new GitUnlockTask(file, force, cancellationToken, processor)
                .Configure(processManager, environment.GitLfsExecutablePath);
        }

        ///<inheritdoc/>
        public ITask<string> GetHead(IOutputProcessor<string> processor = null)
        {
            return new FirstNonNullLineProcessTask(cancellationToken, "rev-parse --short HEAD") { Name = "Getting current head..." }
                .Configure(processManager)
                   .Catch(exception => exception is ProcessException &&
                       exception.Message.StartsWith("fatal: your current branch") &&
                       exception.Message.EndsWith("does not have any commits yet"))
                   .Then((success, head) => success ? head : null);
        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<GitClient>();
    }

    [Serializable]
    public struct GitUser
    {
        public static GitUser Default = new GitUser();

        public string name;
        public string email;

        public string Name => name;
        public string Email { get { return String.IsNullOrEmpty(email) ? String.Empty : email; } }

        public GitUser(string name, string email)
        {
            this.name = name;
            this.email = email;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + Email.GetHashCode();
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
                Email.Equals(other.Email);
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
