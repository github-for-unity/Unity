using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace GitHub.Unity
{
    sealed class ApplicationCache : ScriptObjectSingleton<ApplicationCache>
    {
        [SerializeField] private bool firstRun = true;

        [NonSerialized] private bool? val;

        public bool FirstRun
        {
            get
            {
                if (!val.HasValue)
                {
                    val = firstRun;
                }

                if (firstRun)
                {
                    firstRun = false;
                    Save(true);
                }

                return val.Value;
            }
        }
    }

    sealed class EnvironmentCache : ScriptObjectSingleton<EnvironmentCache>
    {
        [SerializeField] private string repositoryPath;
        [SerializeField] private string unityApplication;
        [SerializeField] private string unityAssetsPath;
        [SerializeField] private string extensionInstallPath;
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
                    environment.InitializeRepository(!String.IsNullOrEmpty(repositoryPath) ? repositoryPath.ToNPath() : null);
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

    [Location("cache/gitlog.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLogCache : ScriptObjectSingleton<GitLogCache>
    {
        [SerializeField] private List<GitLogEntry> log;
        public GitLogCache()
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
