using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class ProjectWindowInterface : AssetPostprocessor
    {
        private static readonly List<GitStatusEntry> entries = new List<GitStatusEntry>();
        private static readonly List<string> guids = new List<string>();
        private static bool initialized = false;

        public static void Initialize(IRepository repository)
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            repository.OnRepositoryChanged += status => Tasks.ScheduleMainThread(() => OnStatusUpdate(status));
            initialized = true;
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
            if (initialized)
            {
                StatusService.Instance.Run();
            }
        }

        private static void OnStatusUpdate(GitStatus update)
        {
            if (update.Entries == null)
            {
                Refresh();
                return;
            }
            entries.Clear();
            entries.AddRange(update.Entries);

            guids.Clear();
            for (var index = 0; index < entries.Count; ++index)
            {
                var path = entries[index].ProjectPath;
                guids.Add(string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
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
            if (index < 0)
            {
                return;
            }

            var texture = Styles.GetGitFileStatusIcon(entries[index].Status);
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
