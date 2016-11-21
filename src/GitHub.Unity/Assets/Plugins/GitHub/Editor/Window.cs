using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;


namespace GitHub.Unity
{
	public class Window : EditorWindow
	{
		const string
			Title = "GitHub",
			LaunchMenu = "Window/GitHub";


		[MenuItem(LaunchMenu)]
		static void Launch()
		{
			GetWindow<Window>().Show();
		}


		List<GitStatusEntry> entries = new List<GitStatusEntry>();


		void OnEnable()
		{
			GitStatusTask.RegisterCallback(OnStatusUpdate);
			GitStatusTask.Schedule();
		}


		void OnDisable()
		{
			GitStatusTask.UnregisterCallback(OnStatusUpdate);
		}


		void OnStatusUpdate(IList<GitStatusEntry> update)
		{
			entries.Clear();
			entries.AddRange(update);
			Repaint();
		}


		List<string> selections = new List<string>();

		Vector2 scrollPosition;
		Dictionary<string, bool> fileSelection = new Dictionary<string, bool>();

		string commitMessage = "";
		string commitBody = "";
		void OnGUI()
		{
			titleContent = new GUIContent(Title);

			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Button("Select"))
			{
				Selection.activeObject = this;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
			{
				GitStatusTask.Schedule();
			}


			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			GUILayout.BeginVertical();
			for(int index = 0; index < entries.Count; ++index)
			{
				var key = entries[index].Path;
				GUILayout.BeginHorizontal();
				//GUILayout.Box(entries[index].ToString());
				var selected = selections.Contains(key);
				EditorGUI.BeginChangeCheck();
				var newSelection = GUILayout.Toggle(selected, "");
				if (EditorGUI.EndChangeCheck())
				{
					if (newSelection)
					{
						if (!selected)
							selections.Add(key);
					}
					else
					{
						if (selected)
							selections.Remove(key);
					}
				}
				GUILayout.Label(key);
				GUILayout.FlexibleSpace();
				GUILayout.Label(entries[index].Status.ToString());
				GUILayout.EndHorizontal();
			}

			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			commitMessage = EditorGUILayout.TextField("Summary", commitMessage);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Description");
			commitBody = EditorGUILayout.TextArea(commitBody, GUILayout.Height(16*10));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Commit") && !String.IsNullOrEmpty(commitMessage))
			{
				CommitTask.Schedule(selections, commitMessage, commitBody);
				commitMessage = "";
				commitBody = "";
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}
	}
}
