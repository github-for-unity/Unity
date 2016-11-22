using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;


namespace GitHub.Unity
{
	public class Window : EditorWindow
	{
		enum ViewMode
		{
			History,
			Changes
		}


		[Serializable]
		class GitCommitTarget
		{
			public bool All = false;
			// TODO: Add line tracking here


			public bool Any
			{
				get
				{
					return All; // TODO: Add line tracking here
				}
			}


			public void Clear()
			{
				All = false;
				// TODO: Add line tracking here
			}
		}


		const string
			Title = "GitHub",
			LaunchMenu = "Window/GitHub",
			ViewModeHistoryTab = "History",
			ViewModeChangesTab = "Changes",
			RefreshButton = "Refresh",
			UnknownViewModeError = "Unsupported view mode: {0}",
			SummaryLabel = "Summary",
			DescriptionLabel = "Description",
			CommitButton = "Commit to <b>{0}</b>",
			ChangedFilesLabel = "{0} changed files",
			OneChangedFileLabel = "1 changed file";
		const float
			CommitAreaMinHeight = 16f,
			CommitAreaDefaultRatio = .4f,
			CommitAreaMaxHeight = 10 * 15f,
			MinCommitTreePadding = 20f;


		[MenuItem(LaunchMenu)]
		static void Launch()
		{
			GetWindow<Window>().Show();
		}


		[SerializeField] ViewMode viewMode = ViewMode.History;
		[SerializeField] List<GitStatusEntry> entries = new List<GitStatusEntry>();
		[SerializeField] List<GitCommitTarget> entryCommitTargets = new List<GitCommitTarget>();
		[SerializeField] Vector2
			verticalCommitScroll,
			horizontalCommitScroll;
		[SerializeField] string
			commitMessage = "",
			commitBody = "",
			currentBranch = "placeholder-placeholder"; // TODO: Ask for branch into updates as well


		bool lockCommit = true;
		GUIStyle commitButtonStyle;
		float commitTreeHeight;


		GUIStyle CommitButtonStyle
		{
			get
			{
				if (commitButtonStyle == null)
				{
					commitButtonStyle = new GUIStyle(GUI.skin.button);
					commitButtonStyle.name = "CommitButtonStyle";
					commitButtonStyle.richText = true;
				}

				return commitButtonStyle;
			}
		}


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
			// Remove what got nuked
			for (int index = 0; index < entries.Count;)
			{
				if (!update.Contains(entries[index]))
				{
					entries.RemoveAt(index);
					entryCommitTargets.RemoveAt(index);
				}
				else
				{
					++index;
				}
			}

			// Add new stuff
			for (int index = 0; index < update.Count; ++index)
			{
				GitStatusEntry entry = update[index];
				if (!entries.Contains(entry))
				{
					entries.Add(entry);
					entryCommitTargets.Add(new GitCommitTarget());
				}
			}

			// TODO: Perform sort dependent on setting, making sure to keep indices in sync between the two lists
			commitTreeHeight = 0f;

			lockCommit = false;

			Repaint();
		}


		void OnGUI()
		{
			// Set window title
			titleContent = new GUIContent(Title);

			// Subtabs & toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				viewMode = GUILayout.Toggle(viewMode == ViewMode.History, ViewModeHistoryTab, EditorStyles.toolbarButton) ? ViewMode.History : viewMode;
				viewMode = GUILayout.Toggle(viewMode == ViewMode.Changes, ViewModeChangesTab, EditorStyles.toolbarButton) ? ViewMode.Changes : viewMode;

				GUILayout.FlexibleSpace();

				if (GUILayout.Button(RefreshButton, EditorStyles.toolbarButton))
				{
					GitStatusTask.Schedule();
				}
			GUILayout.EndHorizontal();

			// Run the proper view mode
			switch(viewMode)
			{
				case ViewMode.History:
					OnHistoryGUI();
				break;
				case ViewMode.Changes:
					OnCommitGUI();
				break;
				default:
					GUILayout.Label(string.Format(UnknownViewModeError));
				break;
			}
		}


		void OnHistoryGUI()
		{
			GUILayout.Label("TODO");
		}


		void OnCommitGUI()
		{
			verticalCommitScroll = GUILayout.BeginScrollView(verticalCommitScroll);
				GUILayout.Label(entries.Count == 0 ? OneChangedFileLabel : string.Format(ChangedFilesLabel, entries.Count));

				// List commit states, paths, and statuses
				GUILayout.BeginVertical(GUI.skin.box);
					if (commitTreeHeight > 0)
					{
						horizontalCommitScroll = GUILayout.BeginScrollView(
							horizontalCommitScroll,
							GUILayout.MinHeight(commitTreeHeight),
							GUILayout.MaxHeight(100000f) // NOTE: This ugliness is necessary as unbounded MaxHeight appears impossible when MinHeight is specified
						);
					}
					else
					{
						horizontalCommitScroll = GUILayout.BeginScrollView(horizontalCommitScroll);
					}
						for (int index = 0; index < entries.Count; ++index)
						{
							GitStatusEntry entry = entries[index];
							GitCommitTarget target = entryCommitTargets[index];

							GUILayout.BeginHorizontal();
								target.All = GUILayout.Toggle(target.All, "");
								GUILayout.Label(entry.Path);

								GUILayout.FlexibleSpace();

								GUILayout.Label(entry.Status.ToString());
							GUILayout.EndHorizontal();
						}

						if (commitTreeHeight == 0f && Event.current.type == EventType.Repaint)
						{
							commitTreeHeight = GUILayoutUtility.GetLastRect().yMax + MinCommitTreePadding;
							Repaint();
						}
						GUILayout.FlexibleSpace();
					GUILayout.EndScrollView();
				GUILayout.EndVertical();

				// Do the commit details area
				OnCommitDetailsAreaGUI();
			GUILayout.EndScrollView();
		}


		void OnCommitDetailsAreaGUI()
		{
			GUILayout.BeginVertical(GUILayout.Height(Mathf.Clamp(position.height * CommitAreaDefaultRatio, CommitAreaMinHeight, CommitAreaMaxHeight)));
				GUILayout.Label(SummaryLabel);
				commitMessage = GUILayout.TextField(commitMessage);

				GUILayout.Label(DescriptionLabel);
				commitBody = EditorGUILayout.TextArea(commitBody, GUILayout.ExpandHeight(true));

				// Disable committing when already committing or if we don't have all the data needed
				EditorGUI.BeginDisabledGroup(lockCommit || string.IsNullOrEmpty(commitMessage) || !entryCommitTargets.Any(t => t.Any));
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(string.Format(CommitButton, currentBranch), CommitButtonStyle))
						{
							Commit();
						}
					GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			GUILayout.EndVertical();
		}


		void Commit()
		{
			// Do not allow new commits before we have received one successful update
			lockCommit = true;

			// Schedule the commit with the selected files
			GitCommitTask.Schedule(
				Enumerable.Range(0, entries.Count).Where(i => entryCommitTargets[i].All).Select(i => entries[i].Path),
				commitMessage,
				commitBody,
				() =>
				{
					commitMessage = "";
					commitBody = "";
					for (int index = 0; index < entries.Count; ++index)
					{
						entryCommitTargets[index].Clear();
					}
				},
				() => lockCommit = false
			);
		}
	}
}
