using System.Collections.Generic;
using UnityEngine;

namespace GitHub.Unity
{
    sealed class ApplicationCache : ScriptObjectSingleton<ApplicationCache>
    {
        [SerializeField] private bool firstRun = true;
        public bool FirstRun
        {
            get
            {
                var val = firstRun;
                if (firstRun)
                {
                    firstRun = false;
                    Save(true);
                }
                return val;
            }
        }

        [SerializeField] private string createdDate;
        public string CreatedDate
        {
            get { return createdDate; }
        }
    }

    [Location("cache/branches.yaml", LocationAttribute.Location.UserFolder)]
    sealed class BranchCache : ScriptObjectSingleton<BranchCache>, IBranchCache
    {
        [SerializeField] private List<GitBranch> localBranches;
        [SerializeField] private List<GitBranch> remoteBranches;
        [SerializeField] private List<GitBranch> test;
        public BranchCache()
        {
            test = new List<GitBranch>() { new GitBranch("name", "tracking", false) };
        }

        public List<GitBranch> LocalBranches
        {
            get
            {
                if (localBranches == null)
                    localBranches = new List<GitBranch>();
                return localBranches;
            }
            set
            {
                Logging.GetLogger().Debug("Saving branches {0}", value.Join(","));
                localBranches = value;
                Save(true);
            }
        }
        public List<GitBranch> RemoteBranches
        {
            get
            {
                if (remoteBranches == null)
                    remoteBranches = new List<GitBranch>();
                return remoteBranches;
            }
            set
            {
                remoteBranches = value;
                Save(true);
            }
        }
    }

    [Location("views/branches.yaml", LocationAttribute.Location.UserFolder)]
    sealed class Favourites : ScriptObjectSingleton<Favourites>
    {
        [SerializeField] private List<string> favouriteBranches;
        public List<string> FavouriteBranches
        {
            get
            {
                if (favouriteBranches == null)
                    FavouriteBranches = new List<string>();
                return favouriteBranches;
            }
            set
            {
                favouriteBranches = value;
                Save(true);
            }
        }

        public void SetFavourite(string branchName)
        {
            if (FavouriteBranches.Contains(branchName))
                return;
            FavouriteBranches.Add(branchName);
            Save(true);
        }

        public void UnsetFavourite(string branchName)
        {
            if (!FavouriteBranches.Contains(branchName))
                return;
            FavouriteBranches.Remove(branchName);
            Save(true);
        }

        public void ToggleFavourite(string branchName)
        {
            if (FavouriteBranches.Contains(branchName))
                FavouriteBranches.Remove(branchName);
            else
                FavouriteBranches.Add(branchName);
            Save(true);
        }

        public bool IsFavourite(string branchName)
        {
            return FavouriteBranches.Contains(branchName);
        }
    }
}
