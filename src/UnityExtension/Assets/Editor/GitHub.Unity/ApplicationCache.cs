using System;
using System.Collections.Generic;
using UnityEditor;
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
    }

    sealed class EnvironmentCache : ScriptObjectSingleton<EnvironmentCache>
    {
        [SerializeField] private string repositoryPath;
        [SerializeField] private string unityApplication;
        [SerializeField] private string unityAssetsPath;
        [SerializeField] private string extensionInstallPath;
        [SerializeField] private string gitExecutablePath;
        [SerializeField] private string unityVersion;

        [NonSerialized] private IEnvironment environment;
        public IEnvironment Environment
        {
            get
            {
                if (environment == null)
                {
                    environment = new DefaultEnvironment();
                    if (unityApplication == null)
                    {
                        unityAssetsPath = Application.dataPath;
                        unityApplication = EditorApplication.applicationPath;
                        extensionInstallPath = DetermineInstallationPath();
                        unityVersion = Application.unityVersion;
                    }
                    environment.Initialize(unityVersion, extensionInstallPath.ToNPath(), unityApplication.ToNPath(), unityAssetsPath.ToNPath());
                    if (repositoryPath != null)
                        environment.InitializeRepository(repositoryPath.ToNPath());
                    Flush();
                }
                return environment;
            }
        }

        private NPath DetermineInstallationPath()
        {
            // Juggling to find out where we got installed
            var shim = ScriptableObject.CreateInstance<RunLocationShim>();
            var script = MonoScript.FromScriptableObject(shim);
            var scriptPath = AssetDatabase.GetAssetPath(script).ToNPath();
            ScriptableObject.DestroyImmediate(shim);
            return scriptPath.Parent;
        }

        public void Flush()
        {
            repositoryPath = Environment.RepositoryPath;
            unityApplication = Environment.UnityApplication;
            unityAssetsPath = Environment.UnityAssetsPath;
            extensionInstallPath = Environment.ExtensionInstallPath;
            gitExecutablePath = Environment.GitExecutablePath;
            Save(true);
        }
    }

    [Location("cache/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class BranchCache : ScriptObjectSingleton<BranchCache>, IBranchCache
    {
        [SerializeField] private List<GitBranch> localBranches;
        [SerializeField] private List<GitBranch> remoteBranches;

        public BranchCache()
        {
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

    [Location("views/branches.yaml", LocationAttribute.Location.LibraryFolder)]
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

    [Location("cache/log.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class LogCache : ScriptObjectSingleton<LogCache>
    {
        [SerializeField] private List<GitLogEntry> log;
        public LogCache()
        {}

        public List<GitLogEntry> Log
        {
            get
            {
                if (log == null)
                    log = new List<GitLogEntry>();
                return log;
            }
            set
            {
                log = value;
                Save(true);
            }
        }
    }
}
