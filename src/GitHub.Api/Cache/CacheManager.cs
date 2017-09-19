using System;
using System.Linq;

namespace GitHub.Unity
{
    class CacheManager
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

        public void SetupCache(IBranchCache branchCache, IRepository repository)
        {
            if (repository == null)
                return;

            BranchCache = branchCache;
            UpdateCache(repository);
            if (onLocalBranchListChanged != null)
                repository.OnLocalBranchListChanged -= onLocalBranchListChanged;
            onLocalBranchListChanged = () =>
            {
                if (!ThreadingHelper.InUIThread)
                    new ActionTask(TaskManager.Instance.Token, () => UpdateCache(repository)) { Affinity = TaskAffinity.UI }.Start();
                else
                    UpdateCache(repository);
            };
            repository.OnLocalBranchListChanged += onLocalBranchListChanged;
        }

        private void UpdateCache(IRepository repository)
        {
            BranchCache.LocalBranches = repository.LocalBranches.ToList();
            BranchCache.RemoteBranches = repository.RemoteBranches.ToList();
        }
    }
}