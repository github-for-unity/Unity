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
        private static readonly List<GitStatusEntry> entries = new List<GitStatusEntry>();
        private static List<GitLock> locks = new List<GitLock>();

        private static readonly List<string> guids = new List<string>();
        private static readonly List<string> guidsLocks = new List<string>();
        private static IApplicationManager manager;
        private static bool isBusy = false;
        private static ILogging logger;
        private static ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger<ProjectWindowInterface>(); } }
        private static CacheUpdateEvent lastRepositoryStatusChangedEvent;
        private static CacheUpdateEvent lastLocksChangedEvent;
        private static IRepository Repository { get { return manager.Environment.Repository; } }

        public static void Initialize(IApplicationManager theManager)
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            manager = theManager;

            if (Repository != null)
            {
                Repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
                Repository.LocksChanged += RepositoryOnLocksChanged;
                ValidateCachedData();
            }
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
            if (isBusy)
                return false;
            if (Repository == null || !Repository.CurrentRemote.HasValue)
                return false;

            return Selection.objects.Any(IsObjectUnlocked);
        }

        private static bool IsObjectUnlocked(Object selected)
        {
            if (selected == null)
                return false;
            if (locks == null)
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
//            isBusy = true;

            var unlockedObjects = Selection.objects.Where(IsObjectUnlocked).ToArray();
            var tasks = unlockedObjects.Select(LockObject).ToArray();

            var taskQueue = new TaskQueue();
            foreach (var task in tasks)
            {
                taskQueue.Queue(task);
            }

            taskQueue.Start();

//            taskQueue.FinallyInUI((success, exception) =>
//            {
//                isBusy = false;
//                Selection.activeGameObject = null;
//                EditorApplication.RepaintProjectWindow();
//            }).Start();
        }

        private static ITask LockObject(Object selected)
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
            if (isBusy)
                return false;
            if (Repository == null || !Repository.CurrentRemote.HasValue)
                return false;

            return Selection.objects.Any(IsObjectLocked);
        }

        private static bool IsObjectLocked(Object selected)
        {
            if (selected == null)
                return false;
            if (locks == null || locks.Count == 0)
                return false;

            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            var isLocked = locks.Any(x => repositoryPath == x.Path);
            return isLocked;
        }

        [MenuItem(AssetsMenuReleaseLock, false, 1000)]
        private static void ContextMenu_Unlock()
        {
//            isBusy = true;

            var lockedObjects = Selection.objects.Where(IsObjectLocked).ToArray();
            var tasks = lockedObjects.Select(o => UnlockObject(o, false)).ToArray();

            var taskQueue = new TaskQueue();
            foreach (var task in tasks)
            {
                taskQueue.Queue(task);
            }

            taskQueue.Start();

//            taskQueue.FinallyInUI((success, exception) =>
//            {
//                isBusy = false;
//                Selection.activeGameObject = null;
//                EditorApplication.RepaintProjectWindow();
//            }).Start();
        }

        [MenuItem(AssetsMenuReleaseLockForced, true, 1000)]
        private static bool ContextMenu_CanUnlockForce()
        {
            if (isBusy)
                return false;
            if (Repository == null || !Repository.CurrentRemote.HasValue)
                return false;

            var selected = Selection.activeObject;
            if (selected == null)
                return false;
            if (locks == null || locks.Count == 0)
                return false;

            NPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToNPath();
            NPath repositoryPath = manager.Environment.GetRepositoryPath(assetPath);

            var isLocked = locks.Any(x => repositoryPath == x.Path);
            return isLocked;
        }

        [MenuItem(AssetsMenuReleaseLockForced, false, 1000)]
        private static void ContextMenu_UnlockForce()
        {
//            isBusy = true;

            var lockedObjects = Selection.objects.Where(IsObjectLocked).ToArray();
            var tasks = lockedObjects.Select(o => UnlockObject(o, true)).ToArray();

            var taskQueue = new TaskQueue();
            foreach (var task in tasks)
            {
                taskQueue.Queue(task);
            }

            taskQueue.Start();

//            taskQueue.FinallyInUI((success, exception) =>
//            {
//                isBusy = false;
//                Selection.activeGameObject = null;
//                EditorApplication.RepaintProjectWindow();
//            }).Start();
        }

        private static ITask UnlockObject(Object selected, bool force)
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
            if (locks == null)
            {
                return;
            }
            locks = locks.ToList();

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
