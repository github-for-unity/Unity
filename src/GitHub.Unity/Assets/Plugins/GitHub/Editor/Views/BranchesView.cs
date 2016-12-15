using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;


namespace GitHub.Unity
{
	[System.Serializable]
	class BranchesView : Subview
	{
		[Serializable]
		class BranchTreeNode
		{
			List<BranchTreeNode> children = new List<BranchTreeNode>();


			public string Label;


			public string Name { get; protected set; }
			public bool Active { get; protected set; }
			public IList<BranchTreeNode> Children { get { return children; } }


			public BranchTreeNode(string name, bool active)
			{
				Label = Name = name;
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
			FavouritesSetting = "Favourites",
			LocalTitle = "LOCAL BRANCHES",
			RemoteTitle = "REMOTE BRANCHES",
			CreateBranchButton = "+ New branch";


		[SerializeField] Vector2 scroll;
		[SerializeField] BranchTreeNode localRoot;
		[SerializeField] List<Remote> remotes = new List<Remote>();
		[SerializeField] BranchTreeNode selectedNode = null;


		BranchTreeNode newNodeSelection = null;
		GitBranchList newLocalBranches;
		int listID = -1;


		public override void Refresh()
		{
			GitListBranchesTask.ScheduleLocal(OnLocalBranchesUpdate);
			GitListBranchesTask.ScheduleRemote(OnRemoteBranchesUpdate);
		}


		void OnLocalBranchesUpdate(GitBranchList list)
		{
			newLocalBranches = list;
		}


		void OnRemoteBranchesUpdate(GitBranchList list)
		{
			BuildTree(newLocalBranches, list);
		}


		void BuildTree(GitBranchList local, GitBranchList remote)
		{
			// Sort
			string activeBranch = local.Branches[local.ActiveIndex];
			List<string>
				localBranches = new List<string>(local.Branches),
				remoteBranches = new List<string>(remote.Branches);
			localBranches.Sort(CompareBranches);
			remoteBranches.Sort(CompareBranches);

			// Just build directly on the local root, keep track of active branch
			localRoot = new BranchTreeNode("", false);
			for (int index = 0; index < localBranches.Count; ++index)
			{
				BuildTree(localRoot, new BranchTreeNode(localBranches[index], localBranches[index].Equals(activeBranch)));
			}

			// Maintain list of remotes before building their roots, ignoring active state
			remotes.Clear();
			for (int index = 0; index < remoteBranches.Count; ++index)
			{
				// Remote name is always the first level
				string branchName = remoteBranches[index];
				string remoteName = branchName.Substring(0, branchName.IndexOf('/'));

				// Get or create this remote
				int remoteIndex = Enumerable.Range(1, remotes.Count + 1).FirstOrDefault(i => remotes.Count > i - 1 && remotes[i - 1].Name.Equals(remoteName)) - 1;
				if (remoteIndex < 0)
				{
					remotes.Add(new Remote() { Name = remoteName, Root = new BranchTreeNode("", false) });
					remoteIndex = remotes.Count - 1;
				}

				// Build on the root of the remote, just like with locals
				BuildTree(remotes[remoteIndex].Root, new BranchTreeNode(branchName, false) { Label = branchName.Substring(remoteName.Length + 1) });
			}

			Repaint();
		}


		static int CompareBranches(string a, string b)
		{
			if (GetFavourite(a))
			{
				return -1;
			}

			if (GetFavourite(b))
			{
				return 1;
			}

			if (a.Equals("master"))
			{
				return -1;
			}

			if (b.Equals("master"))
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
				folder = new BranchTreeNode("", false) { Label = folderName };
				parent.Children.Add(folder);
			}

			// Pop the folder name from the front of the child label and add it to the folder
			child.Label = child.Label.Substring(folderName.Length + 1);
			BuildTree(folder, child);
		}


		void Branch()
		{
			Debug.Log("TODO: Switch to branch creation view");
		}


		static bool GetFavourite(string branch)
		{
			if (string.IsNullOrEmpty(branch))
			{
				return false;
			}

			return Settings.GetElementIndex(FavouritesSetting, branch) > -1;
		}


		static void SetFavourite(string branch, bool favourite)
		{
			if (string.IsNullOrEmpty(branch))
			{
				return;
			}

			if (!favourite)
			{
				Settings.RemoveElement(FavouritesSetting, branch);
			}
			else
			{
				Settings.RemoveElement(FavouritesSetting, branch, false);
				Settings.AddElement(FavouritesSetting, branch);
			}
		}


		public override void OnGUI()
		{
			scroll = GUILayout.BeginScrollView(scroll);
				listID = GUIUtility.GetControlID(FocusType.Keyboard);

				// Local branches and "create branch" button
				GUILayout.Label(LocalTitle);
				GUILayout.BeginHorizontal();
					GUILayout.Space(Styles.BranchListIndentation);
					GUILayout.BeginVertical();
						OnTreeNodeChildrenGUI(localRoot);

						GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

						if (GUILayout.Button(CreateBranchButton, GUI.skin.label, GUILayout.ExpandWidth(false)))
						{
							Branch();
						}
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

			// Effectuating selection
			if (Event.current.type == EventType.Repaint && newNodeSelection != null)
			{
				selectedNode = newNodeSelection;
				GUIUtility.keyboardControl = listID;
				Repaint();
			}
		}


		void OnTreeNodeGUI(BranchTreeNode node)
		{
			// Content, style, and rects
			GUIContent content = new GUIContent(node.Label, node.Children.Count > 0 ? Styles.FolderIcon : Styles.DefaultAssetIcon);
			GUIStyle style = node.Active ? EditorStyles.boldLabel : GUI.skin.label;
			Rect rect = GUILayoutUtility.GetRect(content, style, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
			Rect clickRect = new Rect(0f, rect.y, position.width, rect.height);
			Rect favouriteRect = new Rect(clickRect.xMax - clickRect.height * 2f, clickRect.y, clickRect.height, clickRect.height);

			// Selection highlight and favourite toggle
			if (selectedNode == node)
			{
				GUI.Box(clickRect, GUIContent.none);

				if (!string.IsNullOrEmpty(node.Name))
				{
					bool favourite = GetFavourite(node.Name);
					if (Event.current.type == EventType.Repaint)
					{
						GUI.DrawTexture(favouriteRect, favourite ? Styles.FavouriteIconOn : Styles.FavouriteIconOff);
					}
					else if (Event.current.type == EventType.MouseDown && favouriteRect.Contains(Event.current.mousePosition))
					{
						SetFavourite(node.Name, !favourite);
						Event.current.Use();
					}
				}
			}
			// Favourite status
			else if (Event.current.type == EventType.Repaint && !string.IsNullOrEmpty(node.Name) && GetFavourite(node.Name))
			{
				GUI.DrawTexture(favouriteRect, Styles.FavouriteIconOn);
			}

			// The actual icon and label
			GUI.Label(rect, content, style);

			// Children
			GUILayout.BeginHorizontal();
				GUILayout.Space(Styles.TreeIndentation);
				GUILayout.BeginVertical();
					OnTreeNodeChildrenGUI(node);
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			// Click selection of the node
			if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
			{
				newNodeSelection = node;
				Event.current.Use();
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
				if (selectedNode == node.Children[index] && Event.current.GetTypeForControl(listID) == EventType.KeyDown)
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
