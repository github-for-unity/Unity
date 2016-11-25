using UnityEngine;
using UnityEditor;
using System;
using System.IO;
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


		[Serializable]
		class FileTreeNode
		{
			public string Label;
			public bool Open = true;
			public GitCommitTarget Target;
			public Texture Icon;


			[SerializeField] string path;
			[SerializeField] List<FileTreeNode> children = new List<FileTreeNode>();


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
			ViewModeHistoryTab = "History",
			ViewModeChangesTab = "Changes",
			RefreshButton = "Refresh",
			UnknownViewModeError = "Unsupported view mode: {0}",
			SummaryLabel = "Commit summary",
			DescriptionLabel = "Commit description",
			CommitButton = "Commit to <b>{0}</b>",
			CommitSelectAllButton = "All",
			CommitSelectNoneButton = "None",
			ChangedFilesLabel = "{0} changed files",
			OneChangedFileLabel = "1 changed file",
			NoChangedFilesLabel = "No changed files",
			BasePathLabel = "{0}",
			NoChangesLabel = "No changes found";
		const float
			HistorySummaryPadding = 22f,
			HistoryEntryPadding = 16f,
			CommitAreaMinHeight = 16f,
			CommitAreaDefaultRatio = .4f,
			CommitAreaMaxHeight = 10 * 15f,
			MinCommitTreePadding = 20f,
			FoldoutWidth = 16f,
			TreeIndentation = 18f,
			CommitIconSize = 16f,
			CommitIconHorizontalPadding = -5f,
			CommitFilePrefixSpacing = 2;


		static GUIStyle
			historyEntryDetailsStyle,
			commitFileAreaStyle,
			commitButtonStyle,
			commitDescriptionFieldStyle;
		static Texture2D
			defaultAssetIcon,
			folderIcon;


		static GUIStyle HistoryEntryDetailsStyle
		{
			get
			{
				if (historyEntryDetailsStyle == null)
				{
					historyEntryDetailsStyle = new GUIStyle(EditorStyles.miniLabel);
					historyEntryDetailsStyle.name = "HistoryEntryDetailsStyle";
					Color c = EditorStyles.miniLabel.normal.textColor;
					historyEntryDetailsStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.7f);
				}

				return historyEntryDetailsStyle;
			}
		}


		static GUIStyle CommitFileAreaStyle
		{
			get
			{
				if (commitFileAreaStyle == null)
				{
					commitFileAreaStyle = new GUIStyle(GUI.skin.box);
					commitFileAreaStyle.name = "CommitFileAreaStyle";
					commitFileAreaStyle.margin = new RectOffset(0, 0, 0, 0);
				}

				return commitFileAreaStyle;
			}
		}


		static GUIStyle CommitButtonStyle
		{
			get
			{
				if (commitButtonStyle == null)
				{
					commitButtonStyle = new GUIStyle(GUI.skin.button);
					commitButtonStyle.name = "CommitButtonStyle";
					commitButtonStyle.richText = true;
					commitButtonStyle.wordWrap = true;
				}

				return commitButtonStyle;
			}
		}


		GUIStyle CommitDescriptionFieldStyle
		{
			get
			{
				if (commitDescriptionFieldStyle == null)
				{
					commitDescriptionFieldStyle = new GUIStyle(GUI.skin.textArea);
					commitDescriptionFieldStyle.name = "CommitDescriptionFieldStyle";
					commitDescriptionFieldStyle.wordWrap = true;
				}

				return commitDescriptionFieldStyle;
			}
		}


		Texture2D DefaultAssetIcon
		{
			get
			{
				if (defaultAssetIcon == null)
				{
					defaultAssetIcon = EditorGUIUtility.FindTexture("DefaultAsset Icon");
				}

				return defaultAssetIcon;
			}
		}


		Texture2D FolderIcon
		{
			get
			{
				if (folderIcon == null)
				{
					folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
				}

				return folderIcon;
			}
		}


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
			currentBranch = "placeholder-placeholder"; // TODO: Ask for branch into updates as well
		[SerializeField] FileTreeNode commitTree;
		[SerializeField] List<GitLogEntry> history = new List<GitLogEntry>();


		bool lockCommit = true;
		float commitTreeHeight;


		void OnEnable()
		{
			GitStatusTask.RegisterCallback(OnStatusUpdate);
			GitLogTask.RegisterCallback(OnLogUpdate);
			Refresh();
		}


		void OnDisable()
		{
			GitLogTask.UnregisterCallback(OnLogUpdate);
			GitStatusTask.UnregisterCallback(OnStatusUpdate);
		}


		void OnStatusUpdate(GitStatus update)
		{
			// Set branch
			currentBranch = update.LocalBranch;

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
			commitTree = new FileTreeNode(FindCommonPath("" + Path.DirectorySeparatorChar, entries.Select(e => e.Path)));
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
			history.AddRange(entries);
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
					BuildTree(parent.Add(new FileTreeNode(root)), node);
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
			titleContent = new GUIContent(Title);

			// Subtabs & toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUI.BeginChangeCheck();
					viewMode = GUILayout.Toggle(viewMode == ViewMode.History, ViewModeHistoryTab, EditorStyles.toolbarButton) ? ViewMode.History : viewMode;
					viewMode = GUILayout.Toggle(viewMode == ViewMode.Changes, ViewModeChangesTab, EditorStyles.toolbarButton) ? ViewMode.Changes : viewMode;
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
				default:
					GUILayout.Label(string.Format(UnknownViewModeError));
				break;
			}
		}


		void Refresh()
		{
			if (viewMode == ViewMode.History)
			{
				GitLogTask.Schedule();
			}
			else if (viewMode == ViewMode.Changes)
			{
				GitStatusTask.Schedule();
			}
		}


		void OnHistoryGUI()
		{
			historyScroll = GUILayout.BeginScrollView(historyScroll);
				foreach (GitLogEntry entry in history)
				{
					GUILayout.Label(entry.Summary, GUILayout.MaxWidth(position.width - HistorySummaryPadding));

					GUILayout.BeginHorizontal();
						GUILayout.Label(entry.PrettyTimeString, HistoryEntryDetailsStyle);
						GUILayout.FlexibleSpace();
						GUILayout.Label(entry.AuthorName, HistoryEntryDetailsStyle);
					GUILayout.EndHorizontal();

					GUILayout.Space(HistoryEntryPadding);
				}
			GUILayout.EndScrollView();
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

				GUILayout.BeginVertical(CommitFileAreaStyle);
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
						if (commitTree != null || entries.Count < 1)
						{
							// Base path label
							if (!string.IsNullOrEmpty(commitTree.Path))
							{
								GUILayout.Label(string.Format(BasePathLabel, commitTree.Path));
							}

							// Root nodes
							foreach (FileTreeNode node in commitTree.Children)
							{
								TreeNode(node);
							}

							if (commitTreeHeight == 0f && Event.current.type == EventType.Repaint)
							// If we have no minimum height calculated, do that now and repaint so it can be used
							{
								commitTreeHeight = GUILayoutUtility.GetLastRect().yMax + MinCommitTreePadding;
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
				// Foldout or space for it
				if (isFolder)
				{
					EditorGUI.BeginChangeCheck();
						node.Open = GUILayout.Toggle(node.Open, "", EditorStyles.foldout, GUILayout.Width(FoldoutWidth));
					if (EditorGUI.EndChangeCheck())
					{
						OnCommitTreeChange();
					}
				}
				else
				{
					GUILayout.Space(CommitFilePrefixSpacing);
				}

				// Commit inclusion toggle
				if (target != null)
				{
					target.All = GUILayout.Toggle(target.All, "");
				}

				// Node icon and label
				GUILayout.BeginHorizontal();
					GUILayout.Space(CommitIconHorizontalPadding);
					Rect iconRect = GUILayoutUtility.GetRect(CommitIconSize, CommitIconSize);
					if (Event.current.type == EventType.Repaint)
					{
						GUI.DrawTexture(iconRect, node.Icon ?? (isFolder ? FolderIcon : DefaultAssetIcon), ScaleMode.ScaleToFit);
					}
					GUILayout.Space(CommitIconHorizontalPadding);
				GUILayout.EndHorizontal();
				GUILayout.Label(node.Label);

				GUILayout.FlexibleSpace();

				// Current status (if any)
				if (target != null)
				{
					GUILayout.Label(entries[entryCommitTargets.IndexOf(target)].Status.ToString());
				}
			GUILayout.EndHorizontal();

			// Render children (if any and folded out)
			if (isFolder && node.Open)
			{
				GUILayout.BeginHorizontal();
					GUILayout.Space(TreeIndentation);
					GUILayout.BeginVertical();
						foreach (FileTreeNode child in node.Children)
						{
							TreeNode(child);
						}
					GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}


		void OnCommitDetailsAreaGUI()
		{
			GUILayout.BeginVertical(GUILayout.Height(Mathf.Clamp(position.height * CommitAreaDefaultRatio, CommitAreaMinHeight, CommitAreaMaxHeight)));
				GUILayout.Label(SummaryLabel);
				commitMessage = GUILayout.TextField(commitMessage);

				GUILayout.Label(DescriptionLabel);
				commitBody = EditorGUILayout.TextArea(commitBody, CommitDescriptionFieldStyle, GUILayout.ExpandHeight(true));

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


		// Based on: https://www.rosettacode.org/wiki/Find_common_directory_path#C.23
		static string FindCommonPath(string separator, IEnumerable<string> paths)
		{
			string commonPath = string.Empty;
			List<string> separatedPath = paths
				.First(first => first.Length == paths.Max(second => second.Length))
				.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries)
				.ToList();

			foreach (string pathSegment in separatedPath.AsEnumerable())
			{
				string pathExtension = pathSegment + separator;

				if (commonPath.Length == 0 && paths.All(path => path.StartsWith(pathExtension)))
				{
					commonPath = pathExtension;
				}
				else if (paths.All(path => path.StartsWith(commonPath + pathExtension)))
				{
					commonPath += pathExtension;
				}
				else
				{
					break;
				}
			}

			return commonPath;
		}
	}
}
