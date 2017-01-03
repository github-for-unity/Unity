using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace GitHub.Unity
{
    class ProjectWindowInterface : AssetPostprocessor
    {
        static List<GitStatusEntry> entries = new List<GitStatusEntry>();
        static List<string> guids = new List<string>();


        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            GitStatusTask.UnregisterCallback(OnStatusUpdate);
            GitStatusTask.RegisterCallback(OnStatusUpdate);
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            Tasks.ScheduleMainThread(() => Refresh());
        }


        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
        {
            Refresh();
        }


        static void Refresh()
        {
            GitStatusTask.Schedule();
        }


        static void OnStatusUpdate(GitStatus update)
        {
            entries.Clear();
            entries.AddRange(update.Entries);

            guids.Clear();
            for (int index = 0; index < entries.Count; ++index)
            {
                string path = entries[index].ProjectPath;
                guids.Add(string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
            }

            EditorApplication.RepaintProjectWindow();
        }


        static void OnProjectWindowItemGUI(string guid, Rect itemRect)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(guid))
            {
                return;
            }

            int index = guids.IndexOf(guid);
            if (index < 0)
            {
                return;
            }

            Texture2D texture = Styles.GetGitFileStatusIcon(entries[index].Status);
            Rect rect;

            if (itemRect.width > itemRect.height)
            // End of row placement
            {
                rect = new Rect(itemRect.xMax - texture.width, itemRect.y, texture.width, Mathf.Min(texture.height, EditorGUIUtility.singleLineHeight));
            }
            else
            // Corner placement
            // TODO: Magic numbers that need reviewing. Make sure this works properly with long filenames and wordwrap.
            {
                float scale = itemRect.height / 90f;
                Vector2
                    size = new Vector2(texture.width * scale, texture.height * scale),
                    offset = new Vector2(
                        itemRect.width * Mathf.Min(.4f * scale, .2f),
                        itemRect.height * Mathf.Min(.2f * scale, .2f)
                    );

                rect = new Rect(
                    itemRect.center.x - size.x * .5f + offset.x,
                    itemRect.center.y - size.y * .5f + offset.y,
                    size.x,
                    size.y
                );
            }

            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        }
    }
}
