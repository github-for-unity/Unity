using System;
using System.Linq;

namespace GitHub.Unity
{
    public class CacheManager
    {
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

        private Action onLocalBranchListChanged;
        private Action<GitStatus> onStatusChanged;

        public void SetupCache(IBranchCache branchCache, IRepository repository)
        {
            if (repository == null)
                return;

            BranchCache = branchCache;
            UpdateCache(repository);

            if (onLocalBranchListChanged != null)
            {
                repository.OnLocalBranchListChanged -= onLocalBranchListChanged;
            }

            if (onStatusChanged != null)
            {
                repository.OnStatusChanged -= onStatusChanged;
            }

            onLocalBranchListChanged = () =>
            {
                if (!ThreadingHelper.InUIThread)
                    new ActionTask(TaskManager.Instance.Token, () => UpdateCache(repository)) { Affinity = TaskAffinity.UI }.Start();
                else
                    UpdateCache(repository);
            };

            onStatusChanged = status =>
            {
                if (!ThreadingHelper.InUIThread)
                    new ActionTask(TaskManager.Instance.Token, () => UpdateCache(repository)) { Affinity = TaskAffinity.UI }.Start();
                else
                    UpdateCache(repository);
            };

            repository.OnLocalBranchListChanged += onLocalBranchListChanged;
            repository.OnStatusChanged += onStatusChanged;
        }

        private void UpdateCache(IRepository repository)
        {
            BranchCache.LocalBranches = repository.LocalBranches.ToList();
            BranchCache.RemoteBranches = repository.RemoteBranches.ToList();
        }
    }
}