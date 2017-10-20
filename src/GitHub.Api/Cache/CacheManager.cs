using System;
using System.Linq;

namespace GitHub.Unity
{
    public class CacheManager
    {
        private static ILogging logger = Logging.GetLogger<CacheManager>();

        private IBranchCache branchCache;
        public IBranchCache BranchCache
        {
            get { return branchCache; }
            set
            {
                if (branchCache == null)
                    branchCache = value;
            }
        }

        private IGitLogCache gitLogCache;
        public IGitLogCache GitLogCache
        {
            get { return gitLogCache; }
            set
            {
                if (gitLogCache == null)
                    gitLogCache = value;
            }
        }

        private Action onLocalBranchListChanged;
        private Action<GitStatus> onStatusChanged;
        private Action onCurrentBranchUpdated;

        public void SetupCache(IGitLogCache cache)
        {
            GitLogCache = cache;
        }

        public void SetupCache(IBranchCache cache)
        {
            BranchCache = cache;
        }

        public void SetRepository(IRepository repository)
        {
            if (repository == null)
                return;

            logger.Trace("SetRepository: {0}", repository);

            UpdateBranchCache(repository);
            UpdateGitLogCache(repository);

            if (onLocalBranchListChanged != null)
            {
                repository.OnLocalBranchListChanged -= onLocalBranchListChanged;
            }

            if (onStatusChanged != null)
            {
                repository.OnStatusChanged -= onStatusChanged;
            }

            if (onStatusChanged != null)
            {
                repository.OnCurrentBranchUpdated -= onCurrentBranchUpdated;
            }

            onCurrentBranchUpdated = () => {
                if (!ThreadingHelper.InUIThread)
                    new ActionTask(TaskManager.Instance.Token, () => OnCurrentBranchUpdated(repository)) {
                        Affinity = TaskAffinity.UI
                    }.Start();
                else
                    OnCurrentBranchUpdated(repository);
            };

            onLocalBranchListChanged = () => {
                if (!ThreadingHelper.InUIThread)
                    new ActionTask(TaskManager.Instance.Token, () => OnLocalBranchListChanged(repository)) {
                        Affinity = TaskAffinity.UI
                    }.Start();
                else
                    OnLocalBranchListChanged(repository);
            };

            onStatusChanged = status => {
                if (!ThreadingHelper.InUIThread)
                    new ActionTask(TaskManager.Instance.Token, () => OnStatusChanged(repository)) {
                        Affinity = TaskAffinity.UI
                    }.Start();
                else
                    OnStatusChanged(repository);
            };

            repository.OnCurrentBranchUpdated += onCurrentBranchUpdated;
            repository.OnLocalBranchListChanged += onLocalBranchListChanged;
            repository.OnStatusChanged += onStatusChanged;
        }

        private void OnCurrentBranchUpdated(IRepository repository)
        {
            logger.Trace("OnCurrentBranchUpdated");
            UpdateBranchCache(repository);
            UpdateGitLogCache(repository);
        }

        private void OnLocalBranchListChanged(IRepository repository)
        {
            logger.Trace("OnLocalBranchListChanged");
            UpdateBranchCache(repository);
        }

        private void OnStatusChanged(IRepository repository)
        {
            logger.Trace("OnStatusChanged");
            UpdateBranchCache(repository);
        }

        private void UpdateBranchCache(IRepository repository)
        {
            logger.Trace("UpdateBranchCache");
            BranchCache.LocalBranches = repository.LocalBranches.ToList();
            BranchCache.RemoteBranches = repository.RemoteBranches.ToList();
        }

        private void UpdateGitLogCache(IRepository repository)
        {
            logger.Trace("Start UpdateGitLogCache");
            repository
                .Log()
                .FinallyInUI((success, exception, log) => {
                    if (success)
                    {
                        logger.Trace("Completed UpdateGitLogCache");
                        GitLogCache.Log = log;
                    }
                }).Start();
        }
    }
}