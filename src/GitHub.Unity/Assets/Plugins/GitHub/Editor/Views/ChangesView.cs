using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class ChangesView : Subview
	{
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
			SummaryLabel = "Commit summary",
			DescriptionLabel = "Commit description",
			CommitButton = "Commit to <b>{0}</b>",
			SelectAllButton = "All",
			SelectNoneButton = "None",
			ChangedFilesLabel = "{0} changed files",
			OneChangedFileLabel = "1 changed file",
			NoChangedFilesLabel = "No changed files",
			BasePathLabel = "{0}",
			NoChangesLabel = "No changes found";


		[SerializeField] List<GitStatusEntry> entries = new List<GitStatusEntry>();
		[SerializeField] List<GitCommitTarget> entryCommitTargets = new List<GitCommitTarget>();
		[SerializeField] Vector2
			verticalScroll,
			horizontalScroll;
		[SerializeField] string
			commitMessage = "",
			commitBody = "",
			currentBranch = "[unknown]";
		[SerializeField] FileTreeNode tree;
		[SerializeField] List<string> foldedTreeEntries = new List<string>();


		bool lockCommit = true;
		float treeHeight;


		protected override void OnShow()
		{
			GitStatusTask.RegisterCallback(OnStatusUpdate);
		}


		protected override void OnHide()
		{
			GitStatusTask.UnregisterCallback(OnStatusUpdate);
		}


		public void Refresh()
		{
			GitStatusTask.Schedule();
		}


		void OnStatusUpdate(GitStatus update)
		{
			// Set branch state
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
			tree = new FileTreeNode(Utility.FindCommonPath("" + Path.DirectorySeparatorChar, entries.Select(e => e.Path)));
			tree.RepositoryPath = tree.Path;
			for (int index = 0; index < entries.Count; ++index)
			{
				FileTreeNode node = new FileTreeNode(entries[index].Path.Substring(tree.Path.Length)){ Target = entryCommitTargets[index] };
				if (!string.IsNullOrEmpty(entries[index].ProjectPath))
				{
					node.Icon = AssetDatabase.GetCachedIcon(entries[index].ProjectPath);
				}

				BuildTree(tree, node);
			}

			lockCommit = false;

			OnCommitTreeChange();
		}


		void OnCommitTreeChange()
		{
			treeHeight = 0f;
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


		public override void OnGUI()
		{
			if (treeHeight > 0)
			{
				verticalScroll = GUILayout.BeginScrollView(verticalScroll);
			}
			else
			{
				GUILayout.BeginScrollView(verticalScroll);
			}
				GUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup(entries.Count == 0);
						if (GUILayout.Button(SelectAllButton, EditorStyles.miniButtonLeft))
						{
							SelectAll();
						}

						if (GUILayout.Button(SelectNoneButton, EditorStyles.miniButtonRight))
						{
							SelectNone();
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
					if (treeHeight > 0)
					// Specify a minimum height if we can - avoiding vertical scrollbars on both the outer and inner scroll view
					{
						horizontalScroll = GUILayout.BeginScrollView(
							horizontalScroll,
							GUILayout.MinHeight(treeHeight),
							GUILayout.MaxHeight(100000f) // NOTE: This ugliness is necessary as unbounded MaxHeight appears impossible when MinHeight is specified
						);
					}
					// If we have no minimum height to work with, just stretch and hope
					else
					{
						horizontalScroll = GUILayout.BeginScrollView(horizontalScroll);
					}
						// The file tree (when available)
						if (tree != null && entries.Any())
						{
							// Base path label
							if (!string.IsNullOrEmpty(tree.Path))
							{
								GUILayout.Label(string.Format(BasePathLabel, tree.Path));
							}

							GUILayout.BeginHorizontal();
								GUILayout.Space(Styles.TreeIndentation + Styles.TreeRootIndentation);
								GUILayout.BeginVertical();
									// Root nodes
									foreach (FileTreeNode node in tree.Children)
									{
										TreeNode(node);
									}
								GUILayout.EndVertical();
							GUILayout.EndHorizontal();

							if (treeHeight == 0f && Event.current.type == EventType.Repaint)
							// If we have no minimum height calculated, do that now and repaint so it can be used
							{
								treeHeight = GUILayoutUtility.GetLastRect().yMax + Styles.MinCommitTreePadding;
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


		void SelectAll()
		{
			for (int index = 0; index < entryCommitTargets.Count; ++index)
			{
				entryCommitTargets[index].All = true;
			}
		}


		void SelectNone()
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
	}
}
