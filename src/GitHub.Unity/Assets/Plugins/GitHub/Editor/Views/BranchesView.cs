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
			// Just build directly on the local root, keep track of active branch
			localRoot = new BranchTreeNode("", false);
			for (int index = 0; index < local.Branches.Length; ++index)
			{
				BuildTree(localRoot, new BranchTreeNode(local.Branches[index], index == local.ActiveIndex));
			}

			// Maintain list of remotes before building their roots, ignoring active state
			remotes.Clear();
			for (int index = 0; index < remote.Branches.Length; ++index)
			{
				// Remote name is always the first level
				string name = remote.Branches[index];
				name = name.Substring(0, name.IndexOf('/'));

				// Get or create this remote
				int remoteIndex = Enumerable.Range(1, remotes.Count + 1).FirstOrDefault(i => remotes.Count > i - 1 && remotes[i - 1].Name.Equals(name)) - 1;
				if (remoteIndex < 0)
				{
					remotes.Add(new Remote() { Name = name, Root = new BranchTreeNode("", false) });
					remoteIndex = remotes.Count - 1;
				}

				// Build on the root of the remote, just like with locals
				BuildTree(remotes[remoteIndex].Root, new BranchTreeNode(remote.Branches[index].Substring(name.Length + 1), false));
			}

			Repaint();
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
				folder = new BranchTreeNode(folderName, false);
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


		public override void OnGUI()
		{
			scroll = GUILayout.BeginScrollView(scroll);
				listID = GUIUtility.GetControlID(FocusType.Keyboard);

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

				GUILayout.Label(RemoteTitle);
				GUILayout.BeginHorizontal();
					GUILayout.Space(Styles.BranchListIndentation);
					GUILayout.BeginVertical();
						for (int index = 0; index < remotes.Count; ++index)
						{
							Remote remote = remotes[index];
							GUILayout.Label(remote.Name);

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

			if (Event.current.type == EventType.Repaint && newNodeSelection != null)
			{
				selectedNode = newNodeSelection;
				GUIUtility.keyboardControl = listID;
				Repaint();
			}
		}


		void OnTreeNodeGUI(BranchTreeNode node)
		{
			Rect clickRect = new Rect();

			GUIContent content = new GUIContent(node.Label, node.Children.Count > 0 ? Styles.FolderIcon : Styles.DefaultAssetIcon);
			GUIStyle style = node.Active ? EditorStyles.boldLabel : GUI.skin.label;
			Rect rect = GUILayoutUtility.GetRect(content, style, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
			clickRect = new Rect(0f, rect.y, position.width, rect.height);

			if (selectedNode == node)
			{
				GUI.Box(clickRect, GUIContent.none);
			}

			GUI.Label(rect, content, style);

			GUILayout.BeginHorizontal();
				GUILayout.Space(Styles.TreeIndentation);
				GUILayout.BeginVertical();
					OnTreeNodeChildrenGUI(node);
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
			{
				newNodeSelection = node;
				Event.current.Use();
			}
		}


		void OnTreeNodeChildrenGUI(BranchTreeNode node)
		{
			for (int index = 0; index < node.Children.Count; ++index)
			{
				OnTreeNodeGUI(node.Children[index]);

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
