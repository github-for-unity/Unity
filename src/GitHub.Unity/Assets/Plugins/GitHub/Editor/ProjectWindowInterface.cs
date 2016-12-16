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
			GUI.DrawTexture(
				new Rect(itemRect.xMax - texture.width, itemRect.y, texture.width, Mathf.Min(texture.height, EditorGUIUtility.singleLineHeight)),
				texture,
				ScaleMode.ScaleToFit
			);
		}
	}
}
