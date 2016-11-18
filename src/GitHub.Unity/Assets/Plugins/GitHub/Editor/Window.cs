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


		void OnGUI()
		{
			titleContent = new GUIContent(Title);
			for(int index = 0; index < entries.Count; ++index)
			{
				GUILayout.Box(entries[index].ToString());
			}
		}
	}
}
