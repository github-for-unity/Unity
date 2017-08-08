using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Unity.Helpers;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

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
        private const string FavoritesSetting = "Favorites";
        private const string FavoritesTitle = "Favorites";
        private const string CreateBranchTitle = "Create Branch";
        private const string LocalTitle = "Local branches";
        private const string RemoteTitle = "Remote branches";
        private const string CreateBranchButton = "New Branch";

        private bool showLocalBranches = true;
        private bool showRemoteBranches = true;

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

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            targetMode = mode;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);
            DetachHandlers(oldRepository);
            AttachHandlers(Repository);
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;

            repository.OnLocalBranchListChanged += RunRefreshEmbeddedOnMainThread;
            repository.OnActiveBranchChanged += HandleRepositoryBranchChangeEvent;
            repository.OnActiveRemoteChanged += HandleRepositoryBranchChangeEvent;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.OnLocalBranchListChanged -= RunRefreshEmbeddedOnMainThread;
            repository.OnActiveBranchChanged -= HandleRepositoryBranchChangeEvent;
            repository.OnActiveRemoteChanged -= HandleRepositoryBranchChangeEvent;
        }

        private void RunRefreshEmbeddedOnMainThread()
        {
            new ActionTask(TaskManager.Token, _ => RefreshEmbedded())
                .ScheduleUI(TaskManager);
        }

        private void HandleRepositoryBranchChangeEvent(string obj)
        {
            RunRefreshEmbeddedOnMainThread();
        }

        public override void Refresh()
        {
            base.Refresh();
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
            if (Repository == null)
                return;

            OnLocalBranchesUpdate(Repository.LocalBranches);
            OnRemoteBranchesUpdate(Repository.RemoteBranches);
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

                GUILayout.BeginHorizontal();
                {
                    OnCreateGUI();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
                {
                    // Favourites list
                    if (favourites.Count > 0)
                    {
                        GUILayout.Label(FavoritesTitle);
                        GUILayout.BeginHorizontal();
                        {
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
                    showLocalBranches = EditorGUILayout.Foldout(showLocalBranches, LocalTitle);
                    if (showLocalBranches)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical();
                            {
                                OnTreeNodeChildrenGUI(localRoot);
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }

                    // Remotes
                    showRemoteBranches = EditorGUILayout.Foldout(showRemoteBranches, RemoteTitle);
                    if (showRemoteBranches)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical();
                            for (var index = 0; index < remotes.Count; ++index)
                            {
                                var remote = remotes[index];
                                GUILayout.Label(new GUIContent(remote.Name, Styles.FolderIcon), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));

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

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();
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

        private int CompareBranches(GitBranch a, GitBranch b)
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

        private bool GetFavourite(BranchTreeNode branch)
        {
            return GetFavourite(branch.Name);
        }

        private bool GetFavourite(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
            {
                return false;
            }

            return Manager.LocalSettings.Get(FavoritesSetting, new List<string>()).Contains(branchName);
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
            var cachedFavs = Manager.LocalSettings.Get<List<string>>(FavoritesSetting, new List<string>());

            // Just build directly on the local root, keep track of active branch
            localRoot = new BranchTreeNode("", NodeType.Folder, false);
            for (var index = 0; index < localBranches.Count; ++index)
            {
                var branch = localBranches[index];
                var node = new BranchTreeNode(branch.Name, NodeType.LocalBranch, branch.IsActive);
                localBranchNodes.Add(node);

                // Keep active node for quick reference
                if (branch.IsActive)
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
                Manager.LocalSettings.Set(FavoritesSetting, favourites.Select(x => x.Name).ToList());
            }
            else
            {
                favourites.Remove(branch);
                favourites.Add(branch);
                Manager.LocalSettings.Set(FavoritesSetting, favourites.Select(x => x.Name).ToList());
            }
        }

        private void OnCreateGUI()
        {
            // Create button
            if (mode == BranchesMode.Default)
            {
                // If the current branch is selected, then do not enable the Delete button
                var disableDelete = activeBranchNode == selectedNode;
                EditorGUI.BeginDisabledGroup(disableDelete);
                {
                    if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        var selectedBranchName = selectedNode.Name;
                        var dialogTitle = "Delete Branch: " + selectedBranchName;
                        var dialogMessage = "Are you sure you want to delete the branch: " + selectedBranchName + "?";
                        if (EditorUtility.DisplayDialog("Delete Branch?", dialogMessage, "Delete", "Cancel"))
                        {
                            GitClient.DeleteBranch(selectedBranchName, true).Start();
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(CreateBranchButton, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
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
                                       !Validation.IsBranchNameValid(newBranchName);

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
                    newBranchName = EditorGUILayout.TextField(newBranchName);

                    // Create
                    EditorGUI.BeginDisabledGroup(cannotCreate);
                    {
                        if (GUILayout.Button(NewBranchConfirmButton, EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false)))
                        {
                            createBranch = true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    // Cancel create
                    if (GUILayout.Button(NewBranchCancelButton, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                    {
                        cancelCreate = true;
                    }

                    // Effectuate create
                    if (createBranch)
                    {
                        GitClient.CreateBranch(newBranchName, selectedNode.Name)
                            .FinallyInUI((success, e) => {
                                     if (success)
                                     {
                                         Refresh();
                                     }
                                     else
                                     {
                                         var errorHeader = "fatal: ";
                                         var errorMessage = e.Message.StartsWith(errorHeader) ? e.Message.Remove(0, errorHeader.Length) : e.Message;

                                         EditorUtility.DisplayDialog(CreateBranchTitle,
                                             errorMessage,
                                             Localization.Ok);
                                     }
                            })
                            .Start();
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

            Texture2D iconContent;

            if (node.Active == true)
            {
                iconContent = Styles.ActiveBranchIcon;
            }
            else
            {
                if (node.Children.Count > 0)
                {
                    iconContent = Styles.FolderIcon;
                }
                else
                {
                    iconContent = Styles.BranchIcon;
                }
            }

            var content = new GUIContent(node.Label, iconContent);
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
                        GitClient.SwitchBranch(node.Name)
                            .FinallyInUI((success, e) =>
                            {
                                if (success)
                                    Refresh();
                                else
                                {
                                    EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                        String.Format(Localization.SwitchBranchFailedDescription, node.Name),
                                    Localization.Ok);
                                }
                            }).Start();
                    }
                    else if (node.Type == NodeType.RemoteBranch)
                    {
                        GitClient.CreateBranch(selectedNode.Name.Substring(selectedNode.Name.IndexOf('/') + 1), selectedNode.Name)
                            .FinallyInUI((success, e) =>
                            {
                                if (success)
                                    Refresh();
                                else
                                {
                                    EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                        String.Format(Localization.SwitchBranchFailedDescription, node.Name),
                                    Localization.Ok);
                                }
                            }).Start();
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
