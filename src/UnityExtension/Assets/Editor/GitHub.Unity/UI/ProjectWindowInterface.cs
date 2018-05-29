using GitHub.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class ProjectWindowInterface : AssetPostprocessor
    {
        private const string AssetsMenuRequestLock = "Assets/Request Lock";
        private const string AssetsMenuReleaseLock = "Assets/Release Lock";
        private const string AssetsMenuReleaseLockForced = "Assets/Release Lock (forced)";

        private static List<GitStatusEntry> entries = new List<GitStatusEntry>();
        private static List<GitLock> locks = new List<GitLock>();
        private static List<string> guids = new List<string>();
        private static List<string> guidsLocks = new List<string>();

        private static IApplicationManager manager;
        private static bool isBusy = false;
        private static ILogging logger;
        private static ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger<ProjectWindowInterface>(); } }
        private static CacheUpdateEvent lastRepositoryStatusChangedEvent;
        private static CacheUpdateEvent lastLocksChangedEvent;
        private static IRepository Repository { get { return manager != null ? manager.Environment.Repository : null; } }
        private static bool IsInitialized { get { return Repository != null && Repository.CurrentRemote.HasValue; } }

        public static void Initialize(IApplicationManager theManager)
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            manager = theManager;

            if (IsInitialized)
            {
                Repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
                Repository.LocksChanged += RepositoryOnLocksChanged;
                ValidateCachedData();
            }
        }

        private static bool EnsureInitialized()
        {
            if (locks == null)
                locks = new List<GitLock>();
            if (entries == null)
                entries = new List<GitStatusEntry>();
            if (guids == null)
                guids = new List<string>();
            if (guidsLocks == null)
                guidsLocks = new List<string>();
            return IsInitialized;
        }

        private static void ValidateCachedData()
        {
            Repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitStatus, lastRepositoryStatusChangedEvent);
            Repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLocks, lastLocksChangedEvent);
        }

        private static void RepositoryOnStatusEntriesChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastRepositoryStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                lastRepositoryStatusChangedEvent = cacheUpdateEvent;
                entries.Clear();
                entries.AddRange(Repository.CurrentChanges);
                OnStatusUpdate();
            }
        }

        private static void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                locks = Repository.CurrentLocks;
                OnLocksUpdate();
            }
        }

        [MenuItem(AssetsMenuRequestLock, true)]
        private static bool ContextMenu_CanLock()
        {
            if (!EnsureInitialized())
                return false;
            if (isBusy)
                return false;

            return Selection.objects.Any(IsObjectUnlocked);
        }

        private static bool IsObjectUnlocked(Object selected)
        {
            if (selected == null)
                return false;

            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            var alreadyLocked = locks.Any(x => repositoryPath == x.Path);
            GitFileStatus status = GitFileStatus.None;
            if (entries != null)
            {
                status = entries.FirstOrDefault(x => repositoryPath == x.Path.ToNPath()).Status;
            }

            return !alreadyLocked && status != GitFileStatus.Untracked && status != GitFileStatus.Ignored;
        }

        [MenuItem(AssetsMenuRequestLock)]
        private static void ContextMenu_Lock()
        {
            isBusy = true;

            var unlockedObjects = Selection.objects.Where(IsObjectUnlocked).ToArray();
            var tasks = unlockedObjects.Select(CreateLockObjectTask).ToArray();

            var taskQueue = new TaskQueue();
            foreach (var task in tasks)
            {
                taskQueue.Queue(task);
            }

            taskQueue.FinallyInUI((success, exception) =>
            {
                if (!success)
                {
                    var error = exception.Message;
                    if (error.Contains("exit status 255"))
                        error = "Failed to lock: no permissions";
                    EditorUtility.DisplayDialog(Localization.RequestLockActionTitle,
                        error,
                        Localization.Ok);
                }

                isBusy = false;
                Selection.activeGameObject = null;
            }).Start();
        }

        private static ITask CreateLockObjectTask(Object selected)
        {
            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            return Repository.RequestLock(repositoryPath)
                      .FinallyInUI((success, ex) =>
                      {
                          if (success)
                          {
                              manager.TaskManager.Run(manager.UsageTracker.IncrementUnityProjectViewContextLfsLock, null);
                          }
                      });
        }

        [MenuItem(AssetsMenuReleaseLock, true, 1000)]
        private static bool ContextMenu_CanUnlock()
        {
            if (!EnsureInitialized())
                return false;
            if (isBusy)
                return false;

            return Selection.objects.Any(IsObjectLocked);
        }

        private static bool IsObjectLocked(Object selected)
        {
            if (selected == null)
                return false;

            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            return locks.Any(x => repositoryPath == x.Path);
        }

        [MenuItem(AssetsMenuReleaseLock, false, 1000)]
        private static void ContextMenu_Unlock()
        {
            isBusy = true;

            var lockedObjects = Selection.objects.Where(IsObjectLocked).ToArray();
            var tasks = lockedObjects.Select(o => CreateUnlockObjectTask(o, false)).ToArray();

            var taskQueue = new TaskQueue();
            foreach (var task in tasks)
            {
                taskQueue.Queue(task);
            }

            taskQueue.FinallyInUI((success, exception) =>
            {
                if (!success)
                {
                    var error = exception.Message;
                    if (error.Contains("exit status 255"))
                        error = "Failed to unlock: no permissions";
                    EditorUtility.DisplayDialog(Localization.RequestLockActionTitle,
                        error,
                        Localization.Ok);
                }

                isBusy = false;
                Selection.activeGameObject = null;
            }).Start();
        }

        [MenuItem(AssetsMenuReleaseLockForced, true, 1000)]
        private static bool ContextMenu_CanUnlockForce()
        {
            if (!EnsureInitialized())
                return false;
            if (isBusy)
                return false;

            var selected = Selection.activeObject;
            if (selected == null)
                return false;

            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            return locks.Any(x => repositoryPath == x.Path);
        }

        [MenuItem(AssetsMenuReleaseLockForced, false, 1000)]
        private static void ContextMenu_UnlockForce()
        {
            isBusy = true;

            var lockedObjects = Selection.objects.Where(IsObjectLocked).ToArray();
            var tasks = lockedObjects.Select(o => CreateUnlockObjectTask(o, true)).ToArray();

            var taskQueue = new TaskQueue();
            foreach (var task in tasks)
            {
                taskQueue.Queue(task);
            }

            taskQueue.FinallyInUI((success, exception) =>
            {
                isBusy = false;
                Selection.activeGameObject = null;
            }).Start();
        }

        private static ITask CreateUnlockObjectTask(Object selected, bool force)
        {
            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            return Repository.ReleaseLock(repositoryPath, force)
                      .FinallyInUI((success, ex) =>
                      {
                          if (success)
                          {
                              manager.TaskManager.Run(manager.UsageTracker.IncrementUnityProjectViewContextLfsUnlock, null);
                          }
                      });
        }

        private static void OnLocksUpdate()
        {
            guidsLocks.Clear();
            foreach (var lck in locks)
            {
                NPath repositoryPath = lck.Path;
                NPath assetPath = manager.Environment.GetAssetPath(repositoryPath);

                var g = AssetDatabase.AssetPathToGUID(assetPath);
                guidsLocks.Add(g);
            }

            EditorApplication.RepaintProjectWindow();
        }

        private static void OnStatusUpdate()
        {
            guids.Clear();
            for (var index = 0; index < entries.Count; ++index)
            {
                var path = entries[index].ProjectPath;
                var guid = AssetDatabase.AssetPathToGUID(path);
                guids.Add(guid);
            }

            EditorApplication.RepaintProjectWindow();
        }

        private static void OnProjectWindowItemGUI(string guid, Rect itemRect)
        {
            if (!EnsureInitialized())
                return;

            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(guid))
            {
                return;
            }

            var index = guids.IndexOf(guid);
            var indexLock = guidsLocks.IndexOf(guid);

            if (index < 0 && indexLock < 0)
            {
                return;
            }

            GitStatusEntry? gitStatusEntry = null;
            GitFileStatus status = GitFileStatus.None;

            if (index >= 0)
            {
                gitStatusEntry = entries[index];
                status = gitStatusEntry.Value.Status;
            }

            var isLocked = indexLock >= 0;
            var texture = Styles.GetFileStatusIcon(status, isLocked);

            if (texture == null)
            {
                var path = gitStatusEntry.HasValue ? gitStatusEntry.Value.Path : string.Empty;
                Logger.Warning("Unable to retrieve texture for Guid:{0} EntryPath:{1} Status: {2} IsLocked:{3}", guid, path, status.ToString(), isLocked);
                return;
            }

            Rect rect;

            // End of row placement
            if (itemRect.width > itemRect.height)
            {
                rect = new Rect(itemRect.xMax - texture.width, itemRect.y, texture.width,
                    Mathf.Min(texture.height, EditorGUIUtility.singleLineHeight));
            }
            // Corner placement
            // TODO: Magic numbers that need reviewing. Make sure this works properly with long filenames and wordwrap.
            else
            {
                var scale = itemRect.height / 90f;
                var size = new Vector2(texture.width * scale, texture.height * scale);
                var offset = new Vector2(itemRect.width * Mathf.Min(.4f * scale, .2f), itemRect.height * Mathf.Min(.2f * scale, .2f));
                rect = new Rect(itemRect.center.x - size.x * .5f + offset.x, itemRect.center.y - size.y * .5f + offset.y, size.x, size.y);
            }

            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        }
    }
}
