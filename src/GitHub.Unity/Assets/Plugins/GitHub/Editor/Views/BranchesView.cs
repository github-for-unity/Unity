using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;


namespace GitHub.Unity
{
	[System.Serializable]
	class BranchesView : Subview
	{
		enum NodeType
		{
			Folder,
			LocalBranch,
			RemoteBranch
		}


		enum BranchesMode
		{
			Default,
			Create
		}


		[Serializable]
		class BranchTreeNode
		{
			List<BranchTreeNode> children = new List<BranchTreeNode>();


			public string Label;
			public BranchTreeNode Tracking;


			public string Name { get; protected set; }
			public NodeType Type { get; protected set; }
			public bool Active { get; protected set; }
			public IList<BranchTreeNode> Children { get { return children; } }


			public BranchTreeNode(string name, NodeType type, bool active)
			{
				Label = Name = name;
				Type = type;
				Active = active;
			}
		}


		struct Remote
		{
			// TODO: Pull in and store more data from GitListRemotesTask
			public string Name;
			public BranchTreeNode Root;
		}


		const string
			ConfirmSwitchTitle = "Confirm branch switch",
			ConfirmSwitchMessage = "Switch branch to {0}?",
			ConfirmSwitchOK = "Switch",
			ConfirmSwitchCancel = "Cancel",
			NewBranchCancelButton = "x",
			NewBranchConfirmButton = "Create",
			FavouritesSetting = "Favourites",
			FavouritesTitle = "FAVOURITES",
			LocalTitle = "LOCAL BRANCHES",
			RemoteTitle = "REMOTE BRANCHES",
			CreateBranchButton = "+ New branch";
		static Regex BranchNameRegex = new Regex(@"^(?<name>[\w\d\/\-\_]+)$");


		[SerializeField] Vector2 scroll;
		[SerializeField] BranchTreeNode localRoot;
		[SerializeField] List<Remote> remotes = new List<Remote>();
		[SerializeField] BranchTreeNode selectedNode = null;
		[SerializeField] BranchTreeNode activeBranchNode = null;
		[SerializeField] BranchesMode mode = BranchesMode.Default;
		[SerializeField] string newBranchName;


		BranchTreeNode newNodeSelection = null;
		List<GitBranch> newLocalBranches;
		List<BranchTreeNode> favourites = new List<BranchTreeNode>();
		int listID = -1;
		BranchesMode targetMode;


		protected override void OnShow()
		{
			targetMode = mode;
		}


		public override void Refresh()
		{
			HistoryView historyView = ((Window)parent).HistoryTab;

			if (historyView.BroadMode)
			{
				historyView.Refresh();
			}
			else
			{
				RefreshEmbedded();
			}
		}


		public void RefreshEmbedded()
		{
			GitListBranchesTask.ScheduleLocal(OnLocalBranchesUpdate);
			GitListBranchesTask.ScheduleRemote(OnRemoteBranchesUpdate);
		}


		void OnLocalBranchesUpdate(IEnumerable<GitBranch> list)
		{
			newLocalBranches = new List<GitBranch>(list);
		}


		void OnRemoteBranchesUpdate(IEnumerable<GitBranch> list)
		{
			BuildTree(newLocalBranches, list);
			newLocalBranches.Clear();
		}


		void BuildTree(IEnumerable<GitBranch> local, IEnumerable<GitBranch> remote)
		{
			// Sort
			List<GitBranch>
				localBranches = new List<GitBranch>(local),
				remoteBranches = new List<GitBranch>(remote);
			localBranches.Sort(CompareBranches);
			remoteBranches.Sort(CompareBranches);

			// Prepare for tracking
			List<KeyValuePair<int, int>> tracking = new List<KeyValuePair<int, int>>();
			List<BranchTreeNode> localBranchNodes = new List<BranchTreeNode>();

			// Prepare for updated favourites listing
			favourites.Clear();

			// Just build directly on the local root, keep track of active branch
			localRoot = new BranchTreeNode("", NodeType.Folder, false);
			for (int index = 0; index < localBranches.Count; ++index)
			{
				GitBranch branch = localBranches[index];
				BranchTreeNode node = new BranchTreeNode(branch.Name, NodeType.LocalBranch, branch.Active);
				localBranchNodes.Add(node);

				// Keep active node for quick reference
				if (branch.Active)
				{
					activeBranchNode = node;
				}

				// Add to tracking
				if (!string.IsNullOrEmpty(branch.Tracking))
				{
					int trackingIndex = !remoteBranches.Any() ? -1 :
						Enumerable.Range(1, remoteBranches.Count + 1).FirstOrDefault(
							i => remoteBranches[i - 1].Name.Equals(branch.Tracking)
						) - 1;

					if (trackingIndex > -1)
					{
						tracking.Add(new KeyValuePair<int, int>(index, trackingIndex));
					}
				}

				// Add to favourites
				if (Settings.GetElementIndex(FavouritesSetting, branch.Name) > -1)
				{
					favourites.Add(node);
				}

				// Build into tree
				BuildTree(localRoot, node);
			}

			// Maintain list of remotes before building their roots, ignoring active state
			remotes.Clear();
			for (int index = 0; index < remoteBranches.Count; ++index)
			{
				GitBranch branch = remoteBranches[index];

				// Remote name is always the first level
				string remoteName = branch.Name.Substring(0, branch.Name.IndexOf('/'));

				// Get or create this remote
				int remoteIndex = Enumerable.Range(1, remotes.Count + 1).FirstOrDefault(i => remotes.Count > i - 1 && remotes[i - 1].Name.Equals(remoteName)) - 1;
				if (remoteIndex < 0)
				{
					remotes.Add(new Remote() { Name = remoteName, Root = new BranchTreeNode("", NodeType.Folder, false) });
					remoteIndex = remotes.Count - 1;
				}

				// Create the branch
				BranchTreeNode node = new BranchTreeNode(branch.Name, NodeType.RemoteBranch, false) { Label = branch.Name.Substring(remoteName.Length + 1) };

				// Establish tracking link
				for (int trackingIndex = 0; trackingIndex < tracking.Count; ++trackingIndex)
				{
					KeyValuePair<int, int> pair = tracking[trackingIndex];

					if (pair.Value == index)
					{
						localBranchNodes[pair.Key].Tracking = node;
					}
				}

				// Add to favourites
				if (Settings.GetElementIndex(FavouritesSetting, branch.Name) > -1)
				{
					favourites.Add(node);
				}

				// Build on the root of the remote, just like with locals
				BuildTree(remotes[remoteIndex].Root, node);
			}

			Repaint();
		}


		static int CompareBranches(GitBranch a, GitBranch b)
		{
			if (GetFavourite(a.Name))
			{
				return -1;
			}

			if (GetFavourite(b.Name))
			{
				return 1;
			}

			if (a.Name.Equals("master"))
			{
				return -1;
			}

			if (b.Name.Equals("master"))
			{
				return 1;
			}

			return 0;
		}


		void BuildTree(BranchTreeNode parent, BranchTreeNode child)
		{
			int firstSplit = child.Label.IndexOf('/');

			// No nesting needed here, this is just a straight add
			if (firstSplit < 0)
			{
				parent.Children.Add(child);
				return;
			}

			// Get or create the next folder level
			string folderName = child.Label.Substring(0, firstSplit);
			BranchTreeNode folder = parent.Children.FirstOrDefault(f => f.Label.Equals(folderName));
			if (folder == null)
			{
				folder = new BranchTreeNode("", NodeType.Folder, false) { Label = folderName };
				parent.Children.Add(folder);
			}

			// Pop the folder name from the front of the child label and add it to the folder
			child.Label = child.Label.Substring(folderName.Length + 1);
			BuildTree(folder, child);
		}


		static bool GetFavourite(BranchTreeNode branch)
		{
			return GetFavourite(branch.Name);
		}


		static bool GetFavourite(string branchName)
		{
			if (string.IsNullOrEmpty(branchName))
			{
				return false;
			}

			return Settings.GetElementIndex(FavouritesSetting, branchName) > -1;
		}


		void SetFavourite(BranchTreeNode branch, bool favourite)
		{
			if (string.IsNullOrEmpty(branch.Name))
			{
				return;
			}

			if (!favourite)
			{
				Settings.RemoveElement(FavouritesSetting, branch.Name);
				favourites.Remove(branch);
			}
			else
			{
				Settings.RemoveElement(FavouritesSetting, branch.Name, false);
				Settings.AddElement(FavouritesSetting, branch.Name);
				favourites.Remove(branch);
				favourites.Add(branch);
			}
		}


		public override void OnGUI()
		{
			HistoryView historyView = ((Window)parent).HistoryTab;

			if (historyView.BroadMode)
			{
				historyView.OnGUI();
			}
			else
			{
				OnEmbeddedGUI();

				if (Event.current.type == EventType.Repaint && historyView.EvaluateBroadMode())
				{
					Refresh();
				}
			}
		}

		public void OnEmbeddedGUI()
		{
			scroll = GUILayout.BeginScrollView(scroll);
				listID = GUIUtility.GetControlID(FocusType.Keyboard);

				// Favourites list
				if (favourites.Count > 0)
				{
					GUILayout.Label(FavouritesTitle);
					GUILayout.BeginHorizontal();
						GUILayout.Space(Styles.BranchListIndentation);
						GUILayout.BeginVertical();
							for (int index = 0; index < favourites.Count; ++index)
							{
								OnTreeNodeGUI(favourites[index]);
							}
						GUILayout.EndVertical();
					GUILayout.EndHorizontal();

					GUILayout.Space(Styles.BranchListSeperation);
				}

				// Local branches and "create branch" button
				GUILayout.Label(LocalTitle);
				GUILayout.BeginHorizontal();
					GUILayout.Space(Styles.BranchListIndentation);
					GUILayout.BeginVertical();
						OnTreeNodeChildrenGUI(localRoot);

						GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

						OnCreateGUI();
					GUILayout.EndVertical();
				GUILayout.EndHorizontal();

				GUILayout.Space(Styles.BranchListSeperation);

				// Remotes
				GUILayout.Label(RemoteTitle);
				GUILayout.BeginHorizontal();
					GUILayout.Space(Styles.BranchListIndentation);
					GUILayout.BeginVertical();
						for (int index = 0; index < remotes.Count; ++index)
						{
							Remote remote = remotes[index];
							GUILayout.Label(remote.Name);

							// Branches of the remote
							GUILayout.BeginHorizontal();
								GUILayout.Space(Styles.TreeIndentation);
								GUILayout.BeginVertical();
									OnTreeNodeChildrenGUI(remote.Root);
								GUILayout.EndVertical();
							GUILayout.EndHorizontal();

							GUILayout.Space(Styles.BranchListSeperation);
						}
					GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			GUILayout.EndScrollView();

			if (Event.current.type == EventType.Repaint)
			{
				// Effectuating selection
				if (newNodeSelection != null)
				{
					selectedNode = newNodeSelection;
					newNodeSelection = null;
					GUIUtility.keyboardControl = listID;
					Repaint();
				}

				// Effectuating mode switch
				if (mode != targetMode)
				{
					mode = targetMode;

					if (mode == BranchesMode.Create)
					{
						selectedNode = activeBranchNode;
					}

					Repaint();
				}
			}
		}


		void OnCreateGUI()
		{
			if (mode == BranchesMode.Default)
			// Create button
			{
				if (GUILayout.Button(CreateBranchButton, GUI.skin.label, GUILayout.ExpandWidth(false)))
				{
					targetMode = BranchesMode.Create;
				}
			}
			else if (mode == BranchesMode.Create)
			// Branch name + cancel + create
			{
				GUILayout.BeginHorizontal();
					bool
						createBranch = false,
						cancelCreate = false,
						cannotCreate = selectedNode == null || selectedNode.Type == NodeType.Folder || !BranchNameRegex.IsMatch(newBranchName);

					// Create on return/enter or cancel on escape
					int offsetID = GUIUtility.GetControlID(FocusType.Passive);
					if (Event.current.isKey && GUIUtility.keyboardControl == offsetID + 1)
					{
						if (Event.current.keyCode == KeyCode.Escape)
						{
							cancelCreate = true;
							Event.current.Use();
						}
						else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
						{
							if (cannotCreate)
							{
								EditorApplication.Beep();
							}
							else
							{
								createBranch = true;
							}
							Event.current.Use();
						}
					}
					newBranchName = EditorGUILayout.TextField(newBranchName, GUILayout.MaxWidth(200f));

					// Cancel create
					GUILayout.Space(-5f);
					if (GUILayout.Button(NewBranchCancelButton, EditorStyles.miniButtonMid, GUILayout.ExpandWidth(false)))
					{
						cancelCreate = true;
					}

					// Create
					EditorGUI.BeginDisabledGroup(cannotCreate);
						if (GUILayout.Button(NewBranchConfirmButton, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
						{
							createBranch = true;
						}
					EditorGUI.EndDisabledGroup();

					// Effectuate create
					if (createBranch)
					{
						GitBranchCreateTask.Schedule(newBranchName, selectedNode.Name, Refresh);
					}

					// Cleanup
					if (createBranch || cancelCreate)
					{
						newBranchName = "";
						GUIUtility.keyboardControl = -1;
						targetMode = BranchesMode.Default;
					}
				GUILayout.EndHorizontal();
			}
		}


		void OnTreeNodeGUI(BranchTreeNode node)
		{
			// Content, style, and rects
			GUIContent content = new GUIContent(node.Label, node.Children.Count > 0 ? Styles.FolderIcon : Styles.DefaultAssetIcon);
			GUIStyle style = node.Active ? Styles.BoldLabel : Styles.Label;
			Rect rect = GUILayoutUtility.GetRect(content, style, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
			Rect clickRect = new Rect(0f, rect.y, position.width, rect.height);
			Rect favouriteRect = new Rect(clickRect.xMax - clickRect.height * 2f, clickRect.y, clickRect.height, clickRect.height);

			bool
				selected = selectedNode == node,
				keyboardFocus = GUIUtility.keyboardControl == listID;

			// Selection highlight and favourite toggle
			if (selected)
			{
				if (Event.current.type == EventType.Repaint)
				{
					style.Draw(clickRect, GUIContent.none, false, false, true, keyboardFocus);
				}

				if (node.Type != NodeType.Folder)
				{
					bool favourite = GetFavourite(node);
					if (Event.current.type == EventType.Repaint)
					{
						GUI.DrawTexture(favouriteRect, favourite ? Styles.FavouriteIconOn : Styles.FavouriteIconOff);
					}
					else if (Event.current.type == EventType.MouseDown && favouriteRect.Contains(Event.current.mousePosition))
					{
						SetFavourite(node, !favourite);
						Event.current.Use();
					}
				}
			}
			// Favourite status
			else if (Event.current.type == EventType.Repaint && node.Type != NodeType.Folder && GetFavourite(node.Name))
			{
				GUI.DrawTexture(favouriteRect, Styles.FavouriteIconOn);
			}

			// The actual icon and label
			if (Event.current.type == EventType.Repaint)
			{
				style.Draw(rect, content, false, false, selected, keyboardFocus);
			}

			// State marks
			if (Event.current.type == EventType.Repaint)
			{
				Rect indicatorRect = new Rect(rect.x - rect.height, rect.y, rect.height, rect.height);

				if (selectedNode != null && selectedNode.Tracking == node)
				// Being tracked by current selection mark
				{
					GUI.DrawTexture(indicatorRect, Styles.TrackingBranchIcon);
				}
				else if (node.Active)
				// Active branch mark
				{
					GUI.DrawTexture(indicatorRect, Styles.ActiveBranchIcon);
				}
				else if (node.Tracking != null)
				// Tracking mark
				{
					GUI.DrawTexture(indicatorRect, Styles.TrackingBranchIcon);
				}
			}

			// Children
			GUILayout.BeginHorizontal();
				GUILayout.Space(Styles.TreeIndentation);
				GUILayout.BeginVertical();
					OnTreeNodeChildrenGUI(node);
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			// Click selection of the node as well as branch switch
			if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
			{
				newNodeSelection = node;
				Event.current.Use();

				if (Event.current.clickCount > 1 && mode == BranchesMode.Default)
				{
					if (node.Type == NodeType.LocalBranch && EditorUtility.DisplayDialog(
						ConfirmSwitchTitle,
						string.Format(ConfirmSwitchMessage, node.Name),
						ConfirmSwitchOK,
						ConfirmSwitchCancel
					))
					{
						GitSwitchBranchesTask.Schedule(node.Name, Refresh);
					}
					else if (node.Type == NodeType.RemoteBranch)
					{
						GitBranchCreateTask.Schedule(selectedNode.Name.Substring(selectedNode.Name.IndexOf('/') + 1), selectedNode.Name, Refresh);
					}
				}
			}
		}


		void OnTreeNodeChildrenGUI(BranchTreeNode node)
		{
			if (node == null || node.Children == null)
			{
				return;
			}

			for (int index = 0; index < node.Children.Count; ++index)
			{
				// The actual GUI of the child
				OnTreeNodeGUI(node.Children[index]);

				// Keyboard navigation if this child is the current selection
				if (selectedNode == node.Children[index] && GUIUtility.keyboardControl == listID && Event.current.type == EventType.KeyDown)
				{
					int directionY =
						Event.current.keyCode == KeyCode.UpArrow ?
							-1
						:
							Event.current.keyCode == KeyCode.DownArrow ?
								1
							:
								0,
						directionX =
						Event.current.keyCode == KeyCode.LeftArrow ?
							-1
						:
							Event.current.keyCode == KeyCode.RightArrow ?
								1
							:
								0;

					if (directionY < 0 && index > 0)
					{
						newNodeSelection = node.Children[index - 1];
						Event.current.Use();
					}
					else if (directionY > 0 && index < node.Children.Count - 1)
					{
						newNodeSelection = node.Children[index + 1];
						Event.current.Use();
					}
					else if(directionX < 0)
					{
						newNodeSelection = node;
						Event.current.Use();
					}
					else if(directionX > 0 && node.Children[index].Children.Count > 0)
					{
						newNodeSelection = node.Children[index].Children[0];
						Event.current.Use();
					}
				}
			}
		}
	}
}
