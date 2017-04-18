using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class ProjectWindowInterface : AssetPostprocessor
    {
        private static readonly List<GitStatusEntry> entries = new List<GitStatusEntry>();
        private static List<GitLock> locks = new List<GitLock>();

        private static readonly List<string> guids = new List<string>();
        private static readonly List<string> guidsLocks = new List<string>();
        private static bool initialized = false;
        private static IRepository repository;
        private static bool isBusy = false;
        private static ILogging logger = Logging.GetLogger<ProjectWindowInterface>();
        private static ILogging Logger { get { return logger; } }

        public static void Initialize(IRepository repo)
        {
            repository = repo;
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            initialized = true;
            repository.OnRepositoryChanged += RunStatusUpdateOnMainThread;
            repository.OnLocksUpdated += RunLocksUpdateOnMainThread;
        }

        [MenuItem("Assets/Request Lock", true)]
        private static bool ContextMenu_CanLock()
        {
            if (isBusy)
                return false;
            if (repository == null || !repository.CurrentRemote.HasValue)
                return false;

            var selected = Selection.activeObject;
            if (selected == null)
                return false;
            if (locks == null)
                return false;

            var assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID());
            var repositoryPath = EntryPoint.Environment.GetRepositoryPath(assetPath);

            var alreadyLocked = locks.Any(x =>
            {
                return x.Path == repositoryPath;

            });
            GitFileStatus status = GitFileStatus.None;
            if (entries != null)
            {
                status = entries.FirstOrDefault(x => x.Path.ToNPath() == repositoryPath).Status;
            }
            return !alreadyLocked && status != GitFileStatus.Untracked && status != GitFileStatus.Ignored;
        }

        [MenuItem("Assets/Request Lock")]
        private static void ContextMenu_Lock()
        {
            isBusy = true;
            var selected = Selection.activeObject;

            var assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID());
            var repositoryPath = EntryPoint.Environment.GetRepositoryPath(assetPath);

            repository.RequestLock(new MainThreadTaskResultDispatcher<string>(s => {
                isBusy = false;
                GUI.FocusControl(null);
                EditorApplication.RepaintProjectWindow();
            },
            () => {
                isBusy = false;
                GUI.FocusControl(null);
                EditorApplication.RepaintProjectWindow();
            }), repositoryPath);
        }

        [MenuItem("Assets/Release lock", true, 1000)]
        private static bool ContextMenu_CanUnlock()
        {
            if (isBusy)
                return false;
            if (repository == null || !repository.CurrentRemote.HasValue)
                return false;

            var selected = Selection.activeObject;
            if (selected == null)
                return false;
            if (locks == null || locks.Count == 0)
                return false;

            var assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID());
            var repositoryPath = EntryPoint.Environment.GetRepositoryPath(assetPath);

            var isLocked = locks.Any(x => x.Path == repositoryPath);
            return isLocked;
        }

        [MenuItem("Assets/Release lock", false, 1000)]
        private static void ContextMenu_Unlock()
        {
            isBusy = true;
            var selected = Selection.activeObject;

            var assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID());
            var repositoryPath = EntryPoint.Environment.GetRepositoryPath(assetPath);

            repository.ReleaseLock(new MainThreadTaskResultDispatcher<string>(s =>
            {
                isBusy = false;
                GUI.FocusControl(null);
                EditorApplication.RepaintProjectWindow();
            },
            () => isBusy = false), repositoryPath, false);
        }
        public static void Run()
        {
            Refresh();
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
        {
            Refresh();
        }

        private static void Refresh()
        {
            if (repository == null)
                return;
            if (initialized)
            {
                if (!DefaultEnvironment.OnWindows)
                {
                    repository.Refresh();
                }
            }
        }

        private static void RunLocksUpdateOnMainThread(IEnumerable<GitLock> update)
        {
            TaskRunner.ScheduleMainThread(() => OnLocksUpdate(update));
        }

        private static void OnLocksUpdate(IEnumerable<GitLock> update)
        {
            if (update == null)
            {
                return;
            }
            locks = update.ToList();
            guidsLocks.Clear();
            foreach (var lck in locks)
            {
                var repositoryPath = lck.Path;
                var assetPath = EntryPoint.Environment.GetAssetPath(repositoryPath);

                var g = AssetDatabase.AssetPathToGUID(assetPath);
                if (!guidsLocks.Contains(g))
                {
                    guidsLocks.Add(g);
                }
            }
        }

        private static void RunStatusUpdateOnMainThread(GitStatus update)
        {
            TaskRunner.ScheduleMainThread(() => OnStatusUpdate(update));
        }

        private static void OnStatusUpdate(GitStatus update)
        {
            if (update.Entries == null)
            {
                return;
            }

            entries.Clear();
            entries.AddRange(update.Entries);

            guids.Clear();
            for (var index = 0; index < entries.Count; ++index)
            {
                var path = entries[index].ProjectPath;
                var g = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
                if (!guids.Contains(g))
                {
                    guids.Add(g);
                }
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

            var status = index >= 0 ? entries[index].Status : GitFileStatus.None;
            var texture = Styles.GetFileStatusIcon(status, indexLock >= 0);

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
