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


		enum LogEntryState
		{
			Normal,
			Local,
			Remote
		}


		enum CommitState
		{
			None,
			Some,
			All
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


		[Serializable]
		class FileTreeNode
		{
			public string Label, RepositoryPath;
			public bool Open = true;
			public GitCommitTarget Target;
			public Texture Icon;


			[SerializeField] string path;
			[SerializeField] List<FileTreeNode> children = new List<FileTreeNode>();


			public CommitState State
			{
				get
				{
					if (Target != null)
					{
						return Target.All ? CommitState.All : Target.Any ? CommitState.Some : CommitState.None;
					}
					else
					{
						int allCount = children.Count(c => c.State == CommitState.All);

						if (allCount == children.Count)
						{
							return CommitState.All;
						}

						if (allCount > 0)
						{
							return CommitState.Some;
						}

						return children.Count(c => c.State == CommitState.Some) > 0 ? CommitState.Some : CommitState.None;
					}
				}
				set
				{
					if (value == CommitState.Some || value == State)
					{
						return;
					}

					if (Target != null)
					{
						if (value == CommitState.None)
						{
							Target.Clear();
						}
						else
						{
							Target.All = true;
						}
					}
					else
					{
						for (int index = 0; index < children.Count; ++index)
						{
							children[index].State = value;
						}
					}
				}
			}


			public FileTreeNode(string path)
			{
				this.path = path;
				Label = path;
			}


			public string Path { get { return path; } }
			public IEnumerable<FileTreeNode> Children { get { return children; } }


			public FileTreeNode Add(FileTreeNode child)
			{
				children.Add(child);

				return child;
			}
		}


		const string
			Title = "GitHub",
			LaunchMenu = "Window/GitHub",
			NoActiveRepositoryTitle = "No repository found",
			NoActiveRepositoryMessage = "Your current project is not currently in an active git repository:",
			GitInitBrowseTitle = "Pick desired repository root",
			GitInitButton = "Set up git",
			InvalidInitDirectoryTitle = "Invalid repository root",
			InvalidInitDirectoryMessage = "Your selected folder '{0}' is not a valid repository root for your current project.",
			InvalidInitDirectoryOK = "OK",
			GitInstallTitle = "Git install",
			GitInstallMissingMessage = "GitHub was unable to locate a valid git install. Please specify install location or install git.",
			GitInstallBrowseTitle = "Select git binary",
			GitInstallPickInvalidTitle = "Invalid git install",
			GitInstallPickInvalidMessage = "The selected file is not a valid git install.",
			GitInstallPickInvalidOK = "OK",
			GitInstallFindButton = "Find install",
			GitInstallButton = "New git install",
			GitInstallURL = "http://desktop.github.com",
			ViewModeHistoryTab = "History",
			ViewModeChangesTab = "Changes",
			ViewModeSettingsTab = "Settings",
			RefreshButton = "Refresh",
			UnknownViewModeError = "Unsupported view mode: {0}",
			HistoryFocusAll = "(All)",
			HistoryFocusSingle = "Focus: <b>{0}</b>",
			PullButton = "Pull",
			PullButtonCount = "Pull ({<b>0</b>})",
			PushButton = "Push",
			PushButtonCount = "Push (<b>{0}</b>)",
			PullConfirmTitle = "Pull Changes?",
			PullConfirmDescription = "Would you like to pull changes from remote '{0}'?",
			PullConfirmYes = "Pull",
			PullConfirmCancel = "Cancel",
			PushConfirmTitle = "Push Changes?",
			PushConfirmDescription = "Would you like to push changes to remote '{0}'?",
			PushConfirmYes = "Push",
			PushConfirmCancel = "Cancel",
			SummaryLabel = "Commit summary",
			DescriptionLabel = "Commit description",
			CommitButton = "Commit to <b>{0}</b>",
			CommitSelectAllButton = "All",
			CommitSelectNoneButton = "None",
			ChangedFilesLabel = "{0} changed files",
			OneChangedFileLabel = "1 changed file",
			NoChangedFilesLabel = "No changed files",
			BasePathLabel = "{0}",
			NoChangesLabel = "No changes found",
			RemotesTitle = "Remotes",
			RemoteNameTitle = "Name",
			RemoteUserTitle = "User",
			RemoteHostTitle = "Host",
			RemoteAccessTitle = "Access";
		const int
			HistoryExtraItemCount = 10;


		[MenuItem(LaunchMenu)]
		static void Launch()
		{
			GetWindow<Window>().Show();
		}


		[SerializeField] ViewMode viewMode = ViewMode.History;
		[SerializeField] List<GitStatusEntry> entries = new List<GitStatusEntry>();
		[SerializeField] List<GitCommitTarget> entryCommitTargets = new List<GitCommitTarget>();
		[SerializeField] Vector2
			historyScroll,
			verticalCommitScroll,
			horizontalCommitScroll;
		[SerializeField] string
			commitMessage = "",
			commitBody = "",
			currentBranch = "[unknown]",
			currentRemote = "placeholder";
		[SerializeField] FileTreeNode commitTree;
		[SerializeField] List<string> foldedTreeEntries = new List<string>();
		[SerializeField] List<GitLogEntry> history = new List<GitLogEntry>();
		[SerializeField] bool historyLocked = true;
		[SerializeField] Object historyTarget = null;
		[SerializeField] List<GitRemote> remotes = new List<GitRemote>();


		bool lockCommit = true;
		float commitTreeHeight;
		int
			historyStartIndex,
			historyStopIndex,
			statusAhead,
			statusBehind;
		string initDirectory;


		void OnEnable()
		{
			GitStatusTask.RegisterCallback(OnStatusUpdate);
			GitLogTask.RegisterCallback(OnLogUpdate);
			GitListRemotesTask.RegisterCallback(OnRemotesUpdate);
			Refresh();
		}


		void OnDisable()
		{
			GitListRemotesTask.UnregisterCallback(OnRemotesUpdate);
			GitLogTask.UnregisterCallback(OnLogUpdate);
			GitStatusTask.UnregisterCallback(OnStatusUpdate);
		}


		void OnStatusUpdate(GitStatus update)
		{
			// Set branch state
			currentBranch = update.LocalBranch;
			statusAhead = update.Ahead;
			statusBehind = update.Behind;

			if (viewMode != ViewMode.Changes)
			// No need to update the rest unless we're in the changes view
			{
				return;
			}

			// Remove what got nuked
			for (int index = 0; index < entries.Count;)
			{
				if (!update.Entries.Contains(entries[index]))
				{
					entries.RemoveAt(index);
					entryCommitTargets.RemoveAt(index);
				}
				else
				{
					++index;
				}
			}

			// Remove folding state of nuked items
			for (int index = 0; index < foldedTreeEntries.Count;)
			{
				if (!update.Entries.Any(e => e.Path.IndexOf(foldedTreeEntries[index]) == 0))
				{
					foldedTreeEntries.RemoveAt(index);
				}
				else
				{
					++index;
				}
			}

			// Add new stuff
			for (int index = 0; index < update.Entries.Count; ++index)
			{
				GitStatusEntry entry = update.Entries[index];
				if (!entries.Contains(entry))
				{
					entries.Add(entry);
					entryCommitTargets.Add(new GitCommitTarget());
				}
			}

			// TODO: Filter .meta files - consider adding them as children of the asset or folder they're supporting

			// TODO: In stead of completely rebuilding the tree structure, figure out a way to migrate open/closed states from the old tree to the new

			// Build tree structure
			commitTree = new FileTreeNode(Utility.FindCommonPath("" + Path.DirectorySeparatorChar, entries.Select(e => e.Path)));
			commitTree.RepositoryPath = commitTree.Path;
			for (int index = 0; index < entries.Count; ++index)
			{
				FileTreeNode node = new FileTreeNode(entries[index].Path.Substring(commitTree.Path.Length)){ Target = entryCommitTargets[index] };
				if (!string.IsNullOrEmpty(entries[index].ProjectPath))
				{
					node.Icon = AssetDatabase.GetCachedIcon(entries[index].ProjectPath);
				}

				BuildTree(commitTree, node);
			}

			lockCommit = false;

			OnCommitTreeChange();
		}


		void OnLogUpdate(IList<GitLogEntry> entries)
		{
			history.Clear();
			history.AddRange(entries);
			CullHistory();
			Repaint();
		}


		void OnRemotesUpdate(IList<GitRemote> entries)
		{
			remotes.Clear();
			remotes.AddRange(entries);
			Repaint();
		}


		void OnCommitTreeChange()
		{
			commitTreeHeight = 0f;
			Repaint();
			Repaint();
		}


		void BuildTree(FileTreeNode parent, FileTreeNode node)
		{
			if (string.IsNullOrEmpty(node.Label))
			{
				// TODO: We should probably reassign this target onto the parent? Depends on how we want to handle .meta files for folders
				return;
			}

			node.RepositoryPath = Path.Combine(parent.RepositoryPath, node.Label);
			parent.Open = !foldedTreeEntries.Contains(parent.RepositoryPath);

			// Is this node inside a folder?
			int index = node.Label.IndexOf(Path.DirectorySeparatorChar);
			if (index > 0)
			{
				// Figure out what the root folder is and chop it from the path
				string root = node.Label.Substring(0, index);
				node.Label = node.Label.Substring(index + 1);

				// Look for a branch matching our root in the existing children
				bool found = false;
				foreach (FileTreeNode child in parent.Children)
				{
					if (child.Label.Equals(root))
					// If we found the branch, continue building from that branch
					{
						found = true;
						BuildTree(child, node);
						break;
					}
				}

				// No existing branch - we will have to add a new one to build from
				if (!found)
				{
					BuildTree(parent.Add(new FileTreeNode(root){ RepositoryPath = Path.Combine(parent.RepositoryPath, root) }), node);
				}
			}
			// Not inside a folder - just add this node right here
			else
			{
				parent.Add(node);
			}
		}


		void OnGUI()
		{
			// Set window title
			titleContent = new GUIContent(Title, Styles.TitleIcon);

			// Initial state
			if (!Utility.ActiveRepository || !Utility.GitFound)
			{
				OnSettingsGUI();
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
					OnHistoryGUI();
				break;
				case ViewMode.Changes:
					OnCommitGUI();
				break;
				case ViewMode.Settings:
					OnSettingsGUI();
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
					if (historyTarget != null)
					{
						GitLogTask.Schedule(Utility.AssetPathToRepository(AssetDatabase.GetAssetPath(historyTarget)));
					}
					else
					{
						GitLogTask.Schedule();
					}
				break;
				case ViewMode.Settings:
					GitListRemotesTask.Schedule();
				break;
			}

			GitStatusTask.Schedule();
		}


		void ResetInitDirectory()
		{
			initDirectory = Utility.UnityDataPath.Substring(0, Utility.UnityDataPath.Length - "Assets".Length);
			GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
		}


		static bool ValidateInitDirectory(string path)
		{
			if (Utility.UnityDataPath.IndexOf(path) != 0)
			{
				EditorUtility.DisplayDialog(
					InvalidInitDirectoryTitle,
					string.Format(InvalidInitDirectoryMessage, path),
					InvalidInitDirectoryOK
				);

				return false;
			}

			return true;
		}


		static bool ValidateGitInstall(string path)
		{
			if (!FindGitTask.ValidateGitInstall(path))
			{
				EditorUtility.DisplayDialog(
					GitInstallPickInvalidTitle,
					string.Format(GitInstallPickInvalidMessage, path),
					GitInstallPickInvalidOK
				);

				return false;
			}

			return true;
		}


		void OnSettingsGUI()
		{
			if (!Utility.GitFound)
			{
				Styles.BeginInitialStateArea(GitInstallTitle, GitInstallMissingMessage);
					OnInstallPathGUI();
				Styles.EndInitialStateArea();

				return;
			}
			else if (!Utility.ActiveRepository)
			{
				// If we do run init, make sure that we return to the settings tab for further setup
				viewMode = ViewMode.Settings;

				Styles.BeginInitialStateArea(NoActiveRepositoryTitle, NoActiveRepositoryMessage);
					// Init directory path field
					Styles.PathField(ref initDirectory, () => EditorUtility.OpenFolderPanel(GitInitBrowseTitle, initDirectory, ""), ValidateInitDirectory);

					GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

					// Git init, which starts the config flow
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(GitInitButton, GUILayout.ExpandWidth(false)))
						{
							if (ValidateInitDirectory(initDirectory))
							{
								Init();
							}
							else
							{
								ResetInitDirectory();
							}
						}
					GUILayout.EndHorizontal();
				Styles.EndInitialStateArea();

				return;
			}

			GUILayout.Label("TODO: Favourite branches settings?");

			// Remotes

			float remotesWith = position.width - Styles.RemotesTotalHorizontalMargin;
			float
				nameWidth = remotesWith * Styles.RemotesNameRatio,
				userWidth = remotesWith * Styles.RemotesUserRatio,
				hostWidth = remotesWith * Styles.RemotesHostRation,
				accessWidth = remotesWith * Styles.RemotesAccessRatio;

			GUILayout.Label(RemotesTitle, EditorStyles.boldLabel);
			GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
					GUILayout.Label(RemoteNameTitle, EditorStyles.miniLabel, GUILayout.Width(nameWidth), GUILayout.MaxWidth(nameWidth));
					GUILayout.Label(RemoteUserTitle, EditorStyles.miniLabel, GUILayout.Width(userWidth), GUILayout.MaxWidth(userWidth));
					GUILayout.Label(RemoteHostTitle, EditorStyles.miniLabel, GUILayout.Width(hostWidth), GUILayout.MaxWidth(hostWidth));
					GUILayout.Label(RemoteAccessTitle, EditorStyles.miniLabel, GUILayout.Width(accessWidth), GUILayout.MaxWidth(accessWidth));
				GUILayout.EndHorizontal();

				for (int index = 0; index < remotes.Count; ++index)
				{
					GitRemote remote = remotes[index];
					GUILayout.BeginHorizontal();
						GUILayout.Label(remote.Name, EditorStyles.miniLabel, GUILayout.Width(nameWidth), GUILayout.MaxWidth(nameWidth));
						GUILayout.Label(remote.User, EditorStyles.miniLabel, GUILayout.Width(userWidth), GUILayout.MaxWidth(userWidth));
						GUILayout.Label(remote.Host, EditorStyles.miniLabel, GUILayout.Width(hostWidth), GUILayout.MaxWidth(hostWidth));
						GUILayout.Label(remote.Function.ToString(), EditorStyles.miniLabel, GUILayout.Width(accessWidth), GUILayout.MaxWidth(accessWidth));
					GUILayout.EndHorizontal();
				}
			GUILayout.EndVertical();

			GUILayout.Label("TODO: GitHub login settings");
			GUILayout.Label("TODO: Auto-fetch toggle");
			GUILayout.Label("TODO: Auto-push toggle");

			// Install path

			GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);
			OnInstallPathGUI();
		}


		void OnInstallPathGUI()
		{
			// Install path field
			EditorGUI.BeginChangeCheck();
				string gitInstallPath = Utility.GitInstallPath;
				Styles.PathField(
					ref gitInstallPath,
					() => EditorUtility.OpenFilePanel(
						GitInstallBrowseTitle,
						Path.GetDirectoryName(FindGitTask.DefaultGitPath),
						Path.GetExtension(FindGitTask.DefaultGitPath)
					),
					ValidateGitInstall
				);
			if (EditorGUI.EndChangeCheck())
			{
				Utility.GitInstallPath = gitInstallPath;
			}

			GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

			GUILayout.BeginHorizontal();
				// Find button - for attempting to locate a new install
				if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
				{
					FindGitTask.Schedule(path =>
					{
						if (!string.IsNullOrEmpty(path))
						{
							Utility.GitInstallPath = path;
							GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
						}
					});
				}
				GUILayout.FlexibleSpace();

				// Install button if git is not installed or we want a new install
				if (GUILayout.Button(GitInstallButton, GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(GitInstallURL);
				}
			GUILayout.EndHorizontal();
		}


		void OnHistoryGUI()
		{
			// History toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				// Target indicator / clear button
				EditorGUI.BeginDisabledGroup(historyTarget == null);
					if (GUILayout.Button(
						historyTarget == null ? HistoryFocusAll : string.Format(HistoryFocusSingle, historyTarget.name),
						Styles.HistoryToolbarButtonStyle
					))
					{
						historyTarget = null;
						Refresh();
					}
				EditorGUI.EndDisabledGroup();

				GUILayout.FlexibleSpace();

				// Pull / Push buttons
				if (
					GUILayout.Button(statusBehind > 0 ? string.Format(PullButtonCount, statusBehind) : PullButton, Styles.HistoryToolbarButtonStyle) &&
					EditorUtility.DisplayDialog(
						PullConfirmTitle,
						string.Format(PullConfirmDescription, currentRemote),
						PullConfirmYes,
						PullConfirmCancel
					)
				)
				{
					Pull();
				}
				if (
					GUILayout.Button(statusAhead > 0 ? string.Format(PushButtonCount, statusAhead) : PushButton, Styles.HistoryToolbarButtonStyle) &&
					EditorUtility.DisplayDialog(
						PushConfirmTitle,
						string.Format(PushConfirmDescription, currentRemote),
						PushConfirmYes,
						PushConfirmCancel
					)
				)
				{
					Push();
				}

				// Target lock button
				EditorGUI.BeginChangeCheck();
					historyLocked = GUILayout.Toggle(historyLocked, GUIContent.none, Styles.HistoryLockStyle);
				if (EditorGUI.EndChangeCheck())
				{
					OnSelectionChange();
				}
			GUILayout.EndHorizontal();

			// When history scroll actually changes, store time value of topmost visible entry. This is the value we use to reposition scroll on log update - not the pixel value.
			if (history.Any())
			{
				historyScroll = GUILayout.BeginScrollView(historyScroll);
					// Handle only the selected range of history items - adding spacing for the rest
					float totalEntryHeight = Styles.HistoryEntryHeight + Styles.HistoryEntryPadding;
					GUILayout.Space(historyStartIndex * totalEntryHeight);
					for (int index = historyStartIndex; index < historyStopIndex; ++index)
					{
						LogEntryState entryState = (historyTarget == null ? (index < statusAhead ? LogEntryState.Local : LogEntryState.Normal) : LogEntryState.Normal);

						HistoryEntry(history[index], entryState);

						GUILayout.Space(Styles.HistoryEntryPadding);
					}
					GUILayout.Space((history.Count - historyStopIndex) * totalEntryHeight);
			}
			else
			{
				GUILayout.BeginScrollView(historyScroll);
			}

			GUILayout.EndScrollView();

			if (Event.current.type == EventType.Repaint)
			{
				CullHistory();
			}
		}


		void CullHistory()
		// Recalculate the range of history items to handle - based on what is visible, plus a bit of padding for fast scrolling
		{
			float totalEntryHeight = Styles.HistoryEntryHeight + Styles.HistoryEntryPadding;
			historyStartIndex = (int)Mathf.Clamp(historyScroll.y / totalEntryHeight - HistoryExtraItemCount, 0, history.Count);
			historyStopIndex = (int)Mathf.Clamp(historyStartIndex + position.height / totalEntryHeight + 1 + HistoryExtraItemCount, 0, history.Count);
		}


		void HistoryEntry(GitLogEntry entry, LogEntryState state)
		{
			Rect entryRect = GUILayoutUtility.GetRect(Styles.HistoryEntryHeight, Styles.HistoryEntryHeight);
			Rect
				summaryRect = new Rect(entryRect.x, entryRect.y, entryRect.width, Styles.HistorySummaryHeight),
				timestampRect = new Rect(entryRect.x, entryRect.yMax - Styles.HistoryDetailsHeight, entryRect.width * .5f, Styles.HistoryDetailsHeight);
			Rect authorRect = new Rect(timestampRect.xMax, timestampRect.y, timestampRect.width, timestampRect.height);

			if (!string.IsNullOrEmpty(entry.MergeA))
			{
				const float MergeIndicatorSize = 40f;
				Rect mergeIndicatorRect = new Rect(summaryRect.x, summaryRect.y, MergeIndicatorSize, summaryRect.height);
				GUI.Label(mergeIndicatorRect, "Merge:", Styles.HistoryEntryDetailsStyle);
				summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorSize, summaryRect.height);
			}

			if (state == LogEntryState.Local)
			{
				const float LocalIndicatorSize = 40f;
				Rect localIndicatorRect = new Rect(summaryRect.x, summaryRect.y, LocalIndicatorSize, summaryRect.height);
				GUI.Label(localIndicatorRect, "Local:", Styles.HistoryEntryDetailsStyle);
				summaryRect.Set(localIndicatorRect.xMax, summaryRect.y, summaryRect.width - LocalIndicatorSize, summaryRect.height);
			}

			GUI.Label(summaryRect, entry.Summary);
			GUI.Label(timestampRect, entry.PrettyTimeString, Styles.HistoryEntryDetailsStyle);
			GUI.Label(authorRect, entry.AuthorName, Styles.HistoryEntryDetailsRightStyle);
		}


		void OnSelectionChange()
		{
			if (viewMode != ViewMode.History || historyLocked)
			{
				return;
			}

			if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject)))
			{
				historyTarget = Selection.activeObject;
				Refresh();
			}
		}


		void OnCommitGUI()
		{
			if (commitTreeHeight > 0)
			{
				verticalCommitScroll = GUILayout.BeginScrollView(verticalCommitScroll);
			}
			else
			{
				GUILayout.BeginScrollView(verticalCommitScroll);
			}
				GUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup(entries.Count == 0);
						if (GUILayout.Button(CommitSelectAllButton, EditorStyles.miniButtonLeft))
						{
							CommitSelectAll();
						}

						if (GUILayout.Button(CommitSelectNoneButton, EditorStyles.miniButtonRight))
						{
							CommitSelectNone();
						}
					EditorGUI.EndDisabledGroup();

					GUILayout.FlexibleSpace();

					GUILayout.Label(
						entries.Count == 0 ? NoChangedFilesLabel :
							entries.Count == 1 ? OneChangedFileLabel :
								string.Format(ChangedFilesLabel, entries.Count),
						EditorStyles.miniLabel
					);
				GUILayout.EndHorizontal();

				GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
					if (commitTreeHeight > 0)
					// Specify a minimum height if we can - avoiding vertical scrollbars on both the outer and inner scroll view
					{
						horizontalCommitScroll = GUILayout.BeginScrollView(
							horizontalCommitScroll,
							GUILayout.MinHeight(commitTreeHeight),
							GUILayout.MaxHeight(100000f) // NOTE: This ugliness is necessary as unbounded MaxHeight appears impossible when MinHeight is specified
						);
					}
					// If we have no minimum height to work with, just stretch and hope
					else
					{
						horizontalCommitScroll = GUILayout.BeginScrollView(horizontalCommitScroll);
					}
						// The file tree (when available)
						if (commitTree != null && entries.Any())
						{
							// Base path label
							if (!string.IsNullOrEmpty(commitTree.Path))
							{
								GUILayout.Label(string.Format(BasePathLabel, commitTree.Path));
							}

							GUILayout.BeginHorizontal();
								GUILayout.Space(Styles.TreeIndentation + Styles.TreeRootIndentation);
								GUILayout.BeginVertical();
									// Root nodes
									foreach (FileTreeNode node in commitTree.Children)
									{
										TreeNode(node);
									}
								GUILayout.EndVertical();
							GUILayout.EndHorizontal();

							if (commitTreeHeight == 0f && Event.current.type == EventType.Repaint)
							// If we have no minimum height calculated, do that now and repaint so it can be used
							{
								commitTreeHeight = GUILayoutUtility.GetLastRect().yMax + Styles.MinCommitTreePadding;
								Repaint();
							}

							GUILayout.FlexibleSpace();
						}
						else
						{
							GUILayout.FlexibleSpace();
							GUILayout.BeginHorizontal();
								GUILayout.FlexibleSpace();
								GUILayout.Label(NoChangesLabel);
								GUILayout.FlexibleSpace();
							GUILayout.EndHorizontal();
							GUILayout.FlexibleSpace();
						}
					GUILayout.EndScrollView();
				GUILayout.EndVertical();

				// Do the commit details area
				OnCommitDetailsAreaGUI();
			GUILayout.EndScrollView();
		}


		void TreeNode(FileTreeNode node)
		{
			GitCommitTarget target = node.Target;
			bool isFolder = node.Children.Any();

			GUILayout.BeginHorizontal();
				// Commit inclusion toggle
				CommitState state = node.State;
				bool toggled = state == CommitState.All;

				EditorGUI.BeginChangeCheck();
					toggled = GUILayout.Toggle(toggled, "", state == CommitState.Some ? Styles.ToggleMixedStyle : GUI.skin.toggle, GUILayout.ExpandWidth(false));
				if (EditorGUI.EndChangeCheck())
				{
					node.State = toggled ? CommitState.All : CommitState.None;
				}

				// Foldout
				if (isFolder)
				{
					Rect foldoutRect = GUILayoutUtility.GetLastRect();
					foldoutRect.Set(foldoutRect.x - Styles.FoldoutWidth + Styles.FoldoutIndentation, foldoutRect.y, Styles.FoldoutWidth, foldoutRect.height);

					EditorGUI.BeginChangeCheck();
						node.Open = GUI.Toggle(foldoutRect, node.Open, "", EditorStyles.foldout);
					if (EditorGUI.EndChangeCheck())
					{
						if (!node.Open && !foldedTreeEntries.Contains(node.RepositoryPath))
						{
							foldedTreeEntries.Add(node.RepositoryPath);
						}
						else if (node.Open)
						{
							foldedTreeEntries.Remove(node.RepositoryPath);
						}

						OnCommitTreeChange();
					}
				}

				// Node icon and label
				GUILayout.BeginHorizontal();
					GUILayout.Space(Styles.CommitIconHorizontalPadding);
					Rect iconRect = GUILayoutUtility.GetRect(Styles.CommitIconSize, Styles.CommitIconSize, GUILayout.ExpandWidth(false));
					if (Event.current.type == EventType.Repaint)
					{
						GUI.DrawTexture(iconRect, node.Icon ?? (isFolder ? Styles.FolderIcon : Styles.DefaultAssetIcon), ScaleMode.ScaleToFit);
					}
					GUILayout.Space(Styles.CommitIconHorizontalPadding);
				GUILayout.EndHorizontal();
				GUILayout.Label(new GUIContent(node.Label, node.RepositoryPath), GUILayout.ExpandWidth(true));

				GUILayout.FlexibleSpace();

				// Current status (if any)
				if (target != null)
				{
					GUILayout.Label(entries[entryCommitTargets.IndexOf(target)].Status.ToString(), GUILayout.ExpandWidth(false));
				}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
				// Render children (if any and folded out)
				if (isFolder && node.Open)
				{
						GUILayout.Space(Styles.TreeIndentation);
						GUILayout.BeginVertical();
							foreach (FileTreeNode child in node.Children)
							{
								TreeNode(child);
							}
						GUILayout.EndVertical();
				}
			GUILayout.EndHorizontal();
		}


		void OnCommitDetailsAreaGUI()
		{
			GUILayout.BeginVertical(
				GUILayout.Height(Mathf.Clamp(position.height * Styles.CommitAreaDefaultRatio, Styles.CommitAreaMinHeight, Styles.CommitAreaMaxHeight))
			);
				GUILayout.Label(SummaryLabel);
				commitMessage = GUILayout.TextField(commitMessage);

				GUILayout.Label(DescriptionLabel);
				commitBody = EditorGUILayout.TextArea(commitBody, Styles.CommitDescriptionFieldStyle, GUILayout.ExpandHeight(true));

				// Disable committing when already committing or if we don't have all the data needed
				EditorGUI.BeginDisabledGroup(lockCommit || string.IsNullOrEmpty(commitMessage) || !entryCommitTargets.Any(t => t.Any));
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(string.Format(CommitButton, currentBranch), Styles.CommitButtonStyle))
						{
							Commit();
						}
					GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			GUILayout.EndVertical();
		}


		void Init()
		{
			Debug.LogFormat("TODO: Init '{0}'", initDirectory);
		}


		void CommitSelectAll()
		{
			for (int index = 0; index < entryCommitTargets.Count; ++index)
			{
				entryCommitTargets[index].All = true;
			}
		}


		void CommitSelectNone()
		{
			for (int index = 0; index < entryCommitTargets.Count; ++index)
			{
				entryCommitTargets[index].All = false;
			}
		}


		void Pull()
		{
			Debug.Log("TODO: Pull");
		}


		void Push()
		{
			Debug.Log("TODO: Push");
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
