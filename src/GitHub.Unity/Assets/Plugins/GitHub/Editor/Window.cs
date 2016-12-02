using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;


namespace GitHub.Unity
{
	class RefreshRunner : AssetPostprocessor
	{
		[InitializeOnLoadMethod]
		static void OnLoad()
		{
			Tasks.ScheduleMainThread(Refresh);
		}


		static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
		{
			Refresh();
		}


		static void Refresh()
		{
			foreach (Window window in Object.FindObjectsOfType(typeof(Window)))
			{
				window.Refresh();
			}
		}
	}


	public class Window : EditorWindow, IView
	{
		enum ViewMode
		{
			History,
			Changes,
			Settings
		}


		const string
			Title = "GitHub",
			LaunchMenu = "Window/GitHub",
			ViewModeHistoryTab = "History",
			ViewModeChangesTab = "Changes",
			ViewModeSettingsTab = "Settings",
			RefreshButton = "Refresh",
			UnknownViewModeError = "Unsupported view mode: {0}";


		[MenuItem(LaunchMenu)]
		static void Launch()
		{
			GetWindow<Window>().Show();
		}


		[SerializeField] ViewMode viewMode = ViewMode.History;
		[SerializeField] HistoryView historyView;
		[SerializeField] ChangesView changesView;
		[SerializeField] SettingsView settingsView;


		void OnEnable()
		{
			if (historyView == null)
			{
				historyView = CreateInstance<HistoryView>();
			}
			historyView.Show(this);

			if (changesView == null)
			{
				changesView = CreateInstance<ChangesView>();
			}
			changesView.Show(this);

			if (settingsView == null)
			{
				settingsView = CreateInstance<SettingsView>();
			}
			settingsView.Show(this);

			Refresh();
		}


		public void OnGUI()
		{
			// Set window title
			titleContent = new GUIContent(Title, Styles.TitleIcon);

			// Initial state
			if (!Utility.ActiveRepository || !Utility.GitFound)
			{
				viewMode = ViewMode.Settings; // If we do complete init, make sure that we return to the settings tab for further setup
				settingsView.OnGUI();
				return;
			}

			// Subtabs & toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUI.BeginChangeCheck();
					viewMode = GUILayout.Toggle(viewMode == ViewMode.History, ViewModeHistoryTab, EditorStyles.toolbarButton) ? ViewMode.History : viewMode;
					viewMode = GUILayout.Toggle(viewMode == ViewMode.Changes, ViewModeChangesTab, EditorStyles.toolbarButton) ? ViewMode.Changes : viewMode;
					viewMode = GUILayout.Toggle(viewMode == ViewMode.Settings, ViewModeSettingsTab, EditorStyles.toolbarButton) ? ViewMode.Settings : viewMode;
				if (EditorGUI.EndChangeCheck())
				{
					Refresh();
				}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button(RefreshButton, EditorStyles.toolbarButton))
				{
					Refresh();
				}
			GUILayout.EndHorizontal();

			// Run the proper view mode
			switch(viewMode)
			{
				case ViewMode.History:
					historyView.OnGUI();
				break;
				case ViewMode.Changes:
					changesView.OnGUI();
				break;
				case ViewMode.Settings:
					settingsView.OnGUI();
				break;
				default:
					GUILayout.Label(string.Format(UnknownViewModeError, viewMode));
				break;
			}
		}


		public void Refresh()
		{
			if (!Utility.ActiveRepository)
			{
				return;
			}

			switch (viewMode)
			{
				case ViewMode.History:
					historyView.Refresh();
				break;
				case ViewMode.Changes:
					changesView.Refresh();
				break;
				case ViewMode.Settings:
					GitListRemotesTask.Schedule();
					GitStatusTask.Schedule();
				break;
			}
		}


		void OnSelectionChange()
		{
			if (viewMode != ViewMode.History)
			{
				return;
			}

			historyView.OnSelectionChange();
		}
	}
}
