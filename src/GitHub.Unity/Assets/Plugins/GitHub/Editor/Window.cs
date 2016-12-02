using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;


namespace GitHub.Unity
{
	public class Window : EditorWindow, IView
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


		enum SubTab
		{
			History,
			Changes,
			Settings
		}


		readonly string[]
			TabLabels = new string[]
			{
				"History",
				"Changes",
				"Settings"
			};


		readonly Type[]
			TabTypes = new Type[]
			{
				typeof(HistoryView),
				typeof(ChangesView),
				typeof(SettingsView)
			};


		const SubTab LastTab = SubTab.Settings;


		static void ForEachTab(Action<SubTab> action)
		{
			for (int index = 0; index <= (int)LastTab; ++index)
			{
				action((SubTab)index);
			}
		}


		const string
			Title = "GitHub",
			LaunchMenu = "Window/GitHub",
			RefreshButton = "Refresh",
			UnknownSubTabError = "Unsupported view mode: {0}";


		[MenuItem(LaunchMenu)]
		static void Launch()
		{
			GetWindow<Window>().Show();
		}


		[SerializeField] SubTab activeTab = SubTab.History;
		[SerializeField] List<Subview> tabViews = new List<Subview>();


		void OnEnable()
		{
			if (tabViews.Count <= (int)LastTab)
			{
				tabViews.Clear();
				ForEachTab(tab => tabViews.Add(null));
			}

			ForEachTab(tab =>
			{
				if (tabViews[(int)tab] == null)
				{
					tabViews[(int)tab] = (Subview)CreateInstance(TabTypes[(int)tab]);
				}

				tabViews[(int)tab].Show(this);
			});

			Refresh();
		}


		public void OnGUI()
		{
			// Set window title
			titleContent = new GUIContent(Title, Styles.TitleIcon);

			// Initial state
			if (!Utility.ActiveRepository || !Utility.GitFound)
			{
				activeTab = SubTab.Settings; // If we do complete init, make sure that we return to the settings tab for further setup
				tabViews[(int)SubTab.Settings].OnGUI();
				return;
			}

			// Subtabs & toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUI.BeginChangeCheck();
					ForEachTab(tab => activeTab = GUILayout.Toggle(activeTab == tab, TabLabels[(int)tab], EditorStyles.toolbarButton) ? tab : activeTab);
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

			// GUI for the active tab
			tabViews[(int)activeTab].OnGUI();
		}


		public void Refresh()
		{
			if (Utility.ActiveRepository)
			{
				tabViews[(int)activeTab].Refresh();
			}
		}


		void OnSelectionChange()
		{
			tabViews[(int)activeTab].OnSelectionChange();
		}
	}
}
