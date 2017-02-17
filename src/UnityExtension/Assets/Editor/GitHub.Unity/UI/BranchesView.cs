using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class BranchesView : Subview
    {
        private const string ConfirmSwitchTitle = "Confirm branch switch";
        private const string ConfirmSwitchMessage = "Switch branch to {0}?";
        private const string ConfirmSwitchOK = "Switch";
        private const string ConfirmSwitchCancel = "Cancel";
        private const string NewBranchCancelButton = "x";
        private const string NewBranchConfirmButton = "Create";
        private const string FavouritesSetting = "Favourites";
        private const string FavouritesTitle = "Favourites";
        private const string LocalTitle = "Local branches";
        private const string RemoteTitle = "Remote branches";
        private const string CreateBranchButton = "+ New branch";

        [NonSerialized] private List<BranchTreeNode> favourites = new List<BranchTreeNode>();
        [NonSerialized] private int listID = -1;
        [NonSerialized] private List<GitBranch> newLocalBranches;
        [NonSerialized] private BranchTreeNode newNodeSelection;
        [NonSerialized] private BranchesMode targetMode;

        [SerializeField] private BranchTreeNode activeBranchNode;
        [SerializeField] private BranchTreeNode localRoot;
        [SerializeField] private BranchesMode mode = BranchesMode.Default;
        [SerializeField] private string newBranchName;
        [SerializeField] private List<Remote> remotes = new List<Remote>();
        [SerializeField] private Vector2 scroll;
        [SerializeField] private BranchTreeNode selectedNode;

        public override void Initialize(IView parent)
        {
            base.Initialize(parent);
            targetMode = mode;
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void Refresh()
        {
            var historyView = ((Window)Parent).HistoryTab;

#if ENABLE_BROADMODE
            if (historyView.BroadMode)
                historyView.Refresh();
            else
#endif
                RefreshEmbedded();
        }

        public void RefreshEmbedded()
        {
            GitListBranchesTask.ScheduleLocal(OnLocalBranchesUpdate);
            GitListBranchesTask.ScheduleRemote(OnRemoteBranchesUpdate);
        }

        public override void OnGUI()
        {
            var historyView = ((Window)Parent).HistoryTab;

#if ENABLE_BROADMODE
            if (historyView.BroadMode)
                historyView.OnGUI();
            else
#endif
            {
                OnEmbeddedGUI();

#if ENABLE_BROADMODE
                if (Event.current.type == EventType.Repaint && historyView.EvaluateBroadMode())
                {
                    Refresh();
                }
#endif
            }
        }

        public void OnEmbeddedGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                listID = GUIUtility.GetControlID(FocusType.Keyboard);

                // Favourites list
                if (favourites.Count > 0)
                {
                    GUILayout.Label(FavouritesTitle);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(Styles.BranchListIndentation);
                        GUILayout.BeginVertical();
                        {
                            for (var index = 0; index < favourites.Count; ++index)
                            {
                                OnTreeNodeGUI(favourites[index]);
                            }
                        }

                        GUILayout.EndVertical();
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.Space(Styles.BranchListSeperation);
                }

                // Local branches and "create branch" button
                GUILayout.Label(LocalTitle, EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(Styles.BranchListIndentation);
                    GUILayout.BeginVertical();
                    {
                        OnTreeNodeChildrenGUI(localRoot);

                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                        OnCreateGUI();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(Styles.BranchListSeperation);

                // Remotes
                GUILayout.Label(RemoteTitle, EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(Styles.BranchListIndentation);
                    GUILayout.BeginVertical();
                    for (var index = 0; index < remotes.Count; ++index)
                    {
                        var remote = remotes[index];
                        GUILayout.Label(remote.Name);

                        // Branches of the remote
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(Styles.TreeIndentation);
                            GUILayout.BeginVertical();
                            {
                                OnTreeNodeChildrenGUI(remote.Root);
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(Styles.BranchListSeperation);
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                // Effectuating selection
                if (newNodeSelection != null)
                {
                    selectedNode = newNodeSelection;
                    newNodeSelection = null;
                    GUIUtility.keyboardControl = listID;
                    Redraw();
                }

                // Effectuating mode switch
                if (mode != targetMode)
                {
                    mode = targetMode;

                    if (mode == BranchesMode.Create)
                    {
                        selectedNode = activeBranchNode;
                    }

                    Redraw();
                }
            }
        }

        private static int CompareBranches(GitBranch a, GitBranch b)
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

        private static bool GetFavourite(BranchTreeNode branch)
        {
            return GetFavourite(branch.Name);
        }

        private static bool GetFavourite(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
            {
                return false;
            }

            return EntryPoint.LocalSettings.Get(FavouritesSetting, new List<string>()).Contains(branchName);
        }

        private void OnLocalBranchesUpdate(IEnumerable<GitBranch> list)
        {
            newLocalBranches = new List<GitBranch>(list);
        }

        private void OnRemoteBranchesUpdate(IEnumerable<GitBranch> list)
        {
            BuildTree(newLocalBranches, list);
            newLocalBranches.Clear();
        }

        private void BuildTree(IEnumerable<GitBranch> local, IEnumerable<GitBranch> remote)
        {
            // Sort
            var localBranches = new List<GitBranch>(local);
            var remoteBranches = new List<GitBranch>(remote);
            localBranches.Sort(CompareBranches);
            remoteBranches.Sort(CompareBranches);

            // Prepare for tracking
            var tracking = new List<KeyValuePair<int, int>>();
            var localBranchNodes = new List<BranchTreeNode>();

            // Prepare for updated favourites listing
            favourites.Clear();
            var cachedFavs = EntryPoint.LocalSettings.Get<List<string>>(FavouritesSetting, new List<string>());

            // Just build directly on the local root, keep track of active branch
            localRoot = new BranchTreeNode("", NodeType.Folder, false);
            for (var index = 0; index < localBranches.Count; ++index)
            {
                var branch = localBranches[index];
                var node = new BranchTreeNode(branch.Name, NodeType.LocalBranch, branch.Active);
                localBranchNodes.Add(node);

                // Keep active node for quick reference
                if (branch.Active)
                {
                    activeBranchNode = node;
                }

                // Add to tracking
                if (!string.IsNullOrEmpty(branch.Tracking))
                {
                    var trackingIndex = !remoteBranches.Any()
                        ? -1
                        : Enumerable.Range(0, remoteBranches.Count).FirstOrDefault(i => remoteBranches[i].Name.Equals(branch.Tracking));

                    if (trackingIndex > -1)
                    {
                        tracking.Add(new KeyValuePair<int, int>(index, trackingIndex));
                    }
                }

                // Add to favourites
                if (cachedFavs.Contains(branch.Name))
                {
                    favourites.Add(node);
                }

                // Build into tree
                BuildTree(localRoot, node);
            }

            // Maintain list of remotes before building their roots, ignoring active state
            remotes.Clear();
            for (var index = 0; index < remoteBranches.Count; ++index)
            {
                var branch = remoteBranches[index];

                // Remote name is always the first level
                var remoteName = branch.Name.Substring(0, branch.Name.IndexOf('/'));

                // Get or create this remote
                var remoteIndex = Enumerable.Range(1, remotes.Count + 1)
                    .FirstOrDefault(i => remotes.Count > i - 1 && remotes[i - 1].Name.Equals(remoteName)) - 1;
                if (remoteIndex < 0)
                {
                    remotes.Add(new Remote { Name = remoteName, Root = new BranchTreeNode("", NodeType.Folder, false) });
                    remoteIndex = remotes.Count - 1;
                }

                // Create the branch
                var node = new BranchTreeNode(branch.Name, NodeType.RemoteBranch, false) {
                    Label = branch.Name.Substring(remoteName.Length + 1)
                };

                // Establish tracking link
                for (var trackingIndex = 0; trackingIndex < tracking.Count; ++trackingIndex)
                {
                    var pair = tracking[trackingIndex];

                    if (pair.Value == index)
                    {
                        localBranchNodes[pair.Key].Tracking = node;
                    }
                }

                // Add to favourites
                if (cachedFavs.Contains(branch.Name))
                {
                    favourites.Add(node);
                }

                // Build on the root of the remote, just like with locals
                BuildTree(remotes[remoteIndex].Root, node);
            }

            Redraw();
        }

        private void BuildTree(BranchTreeNode parent, BranchTreeNode child)
        {
            var firstSplit = child.Label.IndexOf('/');

            // No nesting needed here, this is just a straight add
            if (firstSplit < 0)
            {
                parent.Children.Add(child);
                return;
            }

            // Get or create the next folder level
            var folderName = child.Label.Substring(0, firstSplit);
            var folder = parent.Children.FirstOrDefault(f => f.Label.Equals(folderName));
            if (folder == null)
            {
                folder = new BranchTreeNode("", NodeType.Folder, false) { Label = folderName };
                parent.Children.Add(folder);
            }

            // Pop the folder name from the front of the child label and add it to the folder
            child.Label = child.Label.Substring(folderName.Length + 1);
            BuildTree(folder, child);
        }

        private void SetFavourite(BranchTreeNode branch, bool favourite)
        {
            if (string.IsNullOrEmpty(branch.Name))
            {
                return;
            }

            if (!favourite)
            {
                favourites.Remove(branch);
                EntryPoint.LocalSettings.Set(FavouritesSetting, favourites.Select(x => x.Name).ToList());
            }
            else
            {
                favourites.Remove(branch);
                favourites.Add(branch);
                EntryPoint.LocalSettings.Set(FavouritesSetting, favourites.Select(x => x.Name).ToList());
            }
        }

        private void OnCreateGUI()
        {
            // Create button
            if (mode == BranchesMode.Default)
            {
                if (GUILayout.Button(CreateBranchButton, GUI.skin.label, GUILayout.ExpandWidth(false)))
                {
                    targetMode = BranchesMode.Create;
                }
            }
            // Branch name + cancel + create
            else if (mode == BranchesMode.Create)
            {
                GUILayout.BeginHorizontal();
                {
                    var createBranch = false;
                    var cancelCreate = false;
                    var cannotCreate = selectedNode == null ||
                                       selectedNode.Type == NodeType.Folder ||
                                       !Utility.BranchNameRegex.IsMatch(newBranchName);

                    // Create on return/enter or cancel on escape
                    var offsetID = GUIUtility.GetControlID(FocusType.Passive);
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
                    {
                        if (GUILayout.Button(NewBranchConfirmButton, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                        {
                            createBranch = true;
                        }
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
                }
                GUILayout.EndHorizontal();
            }
        }

        private void OnTreeNodeGUI(BranchTreeNode node)
        {
            // Content, style, and rects
            var content = new GUIContent(node.Label, node.Children.Count > 0 ? Styles.FolderIcon : Styles.DefaultAssetIcon);
            var style = node.Active ? Styles.BoldLabel : Styles.Label;
            var rect = GUILayoutUtility.GetRect(content, style, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
            var clickRect = new Rect(0f, rect.y, Position.width, rect.height);
            var favouriteRect = new Rect(clickRect.xMax - clickRect.height * 2f, clickRect.y, clickRect.height, clickRect.height);

            var selected = selectedNode == node;
            var keyboardFocus = GUIUtility.keyboardControl == listID;

            // Selection highlight and favourite toggle
            if (selected)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    style.Draw(clickRect, GUIContent.none, false, false, true, keyboardFocus);
                }

                if (node.Type != NodeType.Folder)
                {
                    var favourite = GetFavourite(node);
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
                var indicatorRect = new Rect(rect.x - rect.height, rect.y, rect.height, rect.height);

                // Being tracked by current selection mark
                if (selectedNode != null && selectedNode.Tracking == node)
                {
                    GUI.DrawTexture(indicatorRect, Styles.TrackingBranchIcon);
                }
                // Active branch mark
                else if (node.Active)
                {
                    GUI.DrawTexture(indicatorRect, Styles.ActiveBranchIcon);
                }
                // Tracking mark
                else if (node.Tracking != null)
                {
                    GUI.DrawTexture(indicatorRect, Styles.TrackingBranchIcon);
                }
            }

            // Children
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(Styles.TreeIndentation);
                GUILayout.BeginVertical();
                {
                    OnTreeNodeChildrenGUI(node);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            // Click selection of the node as well as branch switch
            if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
            {
                newNodeSelection = node;
                Event.current.Use();

                if (Event.current.clickCount > 1 && mode == BranchesMode.Default)
                {
                    if (node.Type == NodeType.LocalBranch &&
                        EditorUtility.DisplayDialog(ConfirmSwitchTitle, String.Format(ConfirmSwitchMessage, node.Name), ConfirmSwitchOK,
                            ConfirmSwitchCancel))
                    {
                        GitSwitchBranchesTask.Schedule(node.Name, Refresh);
                    }
                    else if (node.Type == NodeType.RemoteBranch)
                    {
                        GitBranchCreateTask.Schedule(selectedNode.Name.Substring(selectedNode.Name.IndexOf('/') + 1), selectedNode.Name,
                            Refresh);
                    }
                }
            }
        }

        private void OnTreeNodeChildrenGUI(BranchTreeNode node)
        {
            if (node == null || node.Children == null)
            {
                return;
            }

            for (var index = 0; index < node.Children.Count; ++index)
            {
                // The actual GUI of the child
                OnTreeNodeGUI(node.Children[index]);

                // Keyboard navigation if this child is the current selection
                if (selectedNode == node.Children[index] && GUIUtility.keyboardControl == listID && Event.current.type == EventType.KeyDown)
                {
                    int directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0,
                        directionX = Event.current.keyCode == KeyCode.LeftArrow ? -1 : Event.current.keyCode == KeyCode.RightArrow ? 1 : 0;

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
                    else if (directionX < 0)
                    {
                        newNodeSelection = node;
                        Event.current.Use();
                    }
                    else if (directionX > 0 && node.Children[index].Children.Count > 0)
                    {
                        newNodeSelection = node.Children[index].Children[0];
                        Event.current.Use();
                    }
                }
            }
        }

        private enum NodeType
        {
            Folder,
            LocalBranch,
            RemoteBranch
        }

        private enum BranchesMode
        {
            Default,
            Create
        }

        [Serializable]
        private class BranchTreeNode
        {
            private readonly List<BranchTreeNode> children = new List<BranchTreeNode>();

            public string Label;
            public BranchTreeNode Tracking;

            public BranchTreeNode(string name, NodeType type, bool active)
            {
                Label = Name = name;
                Type = type;
                Active = active;
            }

            public string Name { get; private set; }
            public NodeType Type { get; private set; }
            public bool Active { get; private set; }

            public IList<BranchTreeNode> Children { get { return children; } }
        }

        private struct Remote
        {
            // TODO: Pull in and store more data from GitListRemotesTask
            public string Name;
            public BranchTreeNode Root;
        }
    }
}
