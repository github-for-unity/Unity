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
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			int index = guids.IndexOf(guid);
			if (index < 0)
			{
				return;
			}

			GUIStyle style = EditorStyles.miniLabel;
			GUIContent content = new GUIContent(entries[index].Status.ToString());
			Vector2 size = style.CalcSize(content);

			style.Draw(new Rect(itemRect.xMax - size.x, itemRect.y, size.x, size.y), content, false, false, false, false);
		}
	}
}
