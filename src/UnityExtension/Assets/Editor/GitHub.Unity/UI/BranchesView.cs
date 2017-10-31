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
        private const string ConfirmCheckoutBranchTitle = "Confirm branch checkout";
        private const string ConfirmCheckoutBranchMessage = "Checkout branch {0} from {1}?";
        private const string ConfirmCheckoutBranchOK = "Checkout";
        private const string ConfirmCheckoutBranchCancel = "Cancel";
        private const string WarningCheckoutBranchExistsTitle = "Branch already exists";
        private const string WarningCheckoutBranchExistsMessage = "Branch {0} already exists";
        private const string WarningCheckoutBranchExistsOK = "Ok";
        private const string NewBranchCancelButton = "x";
        private const string NewBranchConfirmButton = "Create";
        private const string CreateBranchTitle = "Create Branch";
        private const string LocalTitle = "Local branches";
        private const string RemoteTitle = "Remote branches";
        private const string CreateBranchButton = "New Branch";
        private const string DeleteBranchMessageFormatString = "Are you sure you want to delete the branch: {0}?";
        private const string DeleteBranchTitle = "Delete Branch?";
        private const string DeleteBranchButton = "Delete";
        private const string CancelButtonLabel = "Cancel";

        private bool showLocalBranches = true;
        private bool showRemoteBranches = true;

        [NonSerialized] private int listID = -1;
        [NonSerialized] private BranchTreeNode newNodeSelection;
        [NonSerialized] private BranchesMode targetMode;

        [SerializeField] private BranchTreeNode activeBranchNode;
        [SerializeField] private BranchTreeNode localRoot;
        [SerializeField] private BranchesMode mode = BranchesMode.Default;
        [SerializeField] private string newBranchName;
        [SerializeField] private List<Remote> remotes = new List<Remote>();
        [SerializeField] private Vector2 scroll;
        [SerializeField] private BranchTreeNode selectedNode;

        [SerializeField] private CacheUpdateEvent branchUpdateEvent;
        [NonSerialized] private bool branchCacheHasUpdate;
        [SerializeField] private GitBranch[] localBranches;
        [SerializeField] private GitBranch[] remoteBranches;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            targetMode = mode;
        }

        private void Repository_BranchCacheUpdated(CacheUpdateEvent cacheUpdateEvent)
        {
            new ActionTask(TaskManager.Token, () => {
                    branchUpdateEvent = cacheUpdateEvent;
                    branchCacheHasUpdate = true;
                    Redraw();
                })
                { Affinity = TaskAffinity.UI }.Start();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                Repository.CheckBranchCacheEvent(branchUpdateEvent);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        private void MaybeUpdateData()
        {
            if (branchCacheHasUpdate)
            {
                branchCacheHasUpdate = false;

                localBranches = Repository.LocalBranches.ToArray();
                remoteBranches = Repository.RemoteBranches.ToArray();


                BuildTree(localBranches, remoteBranches);
            }
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;

            repository.BranchCacheUpdated += Repository_BranchCacheUpdated;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;

            repository.BranchCacheUpdated -= Repository_BranchCacheUpdated;
        }

        public override void OnGUI()
        {
            OnEmbeddedGUI();
        }

        public void OnEmbeddedGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                listID = GUIUtility.GetControlID(FocusType.Keyboard);

                GUILayout.BeginHorizontal();
                {
                    OnButtonBarGUI();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
                {
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

        private void BuildTree(IEnumerable<GitBranch> local, IEnumerable<GitBranch> remote)
        {
            //Clear the selected node
            selectedNode = null;
 
            // Sort
            var localBranches = new List<GitBranch>(local);
            var remoteBranches = new List<GitBranch>(remote);
            localBranches.Sort(CompareBranches);
            remoteBranches.Sort(CompareBranches);

            // Prepare for tracking
            var tracking = new List<KeyValuePair<int, int>>();
            var localBranchNodes = new List<BranchTreeNode>();

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

        private void OnButtonBarGUI()
        {
            if (mode == BranchesMode.Default)
            {
                // Delete button
                // If the current branch is selected, then do not enable the Delete button
                var disableDelete = selectedNode == null || selectedNode.Type == NodeType.Folder || activeBranchNode == selectedNode;
                EditorGUI.BeginDisabledGroup(disableDelete);
                {
                    if (GUILayout.Button(DeleteBranchButton, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        var selectedBranchName = selectedNode.Name;
                        var dialogMessage = string.Format(DeleteBranchMessageFormatString, selectedBranchName);
                        if (EditorUtility.DisplayDialog(DeleteBranchTitle, dialogMessage, DeleteBranchButton, CancelButtonLabel))
                        {
                            GitClient.DeleteBranch(selectedBranchName, true).Start();
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                // Create button
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
                                         Redraw();
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

            var selected = selectedNode == node;
            var keyboardFocus = GUIUtility.keyboardControl == listID;

            // Selection highlight and favorite toggle
            if (selected)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    style.Draw(clickRect, GUIContent.none, false, false, true, keyboardFocus);
                }
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
                    if (node.Type == NodeType.LocalBranch)
                    {
                        if (EditorUtility.DisplayDialog(ConfirmSwitchTitle, String.Format(ConfirmSwitchMessage, node.Name), ConfirmSwitchOK, ConfirmSwitchCancel))
                        {
                            GitClient.SwitchBranch(node.Name)
                                .FinallyInUI((success, e) =>
                                {
                                    if (success)
                                    {
                                        Redraw();
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                            String.Format(Localization.SwitchBranchFailedDescription, node.Name),
                                            Localization.Ok);
                                    }
                                }).Start();
                        }
                    }
                    else if (node.Type == NodeType.RemoteBranch)
                    {
                        var indexOfFirstSlash = selectedNode.Name.IndexOf('/');
                        var originName = selectedNode.Name.Substring(0, indexOfFirstSlash);
                        var branchName = selectedNode.Name.Substring(indexOfFirstSlash + 1);

                        if (localBranches.Any(localBranch => localBranch.Name == branchName))
                        {
                            EditorUtility.DisplayDialog(WarningCheckoutBranchExistsTitle, 
                                String.Format(WarningCheckoutBranchExistsMessage, branchName),
                                WarningCheckoutBranchExistsOK);
                        }
                        else
                        {
                            var confirmCheckout = EditorUtility.DisplayDialog(ConfirmCheckoutBranchTitle, 
                                String.Format(ConfirmCheckoutBranchMessage, node.Name, originName), 
                                ConfirmCheckoutBranchOK, ConfirmCheckoutBranchCancel);

                            if (confirmCheckout)
                            {
                                GitClient.CreateBranch(branchName, selectedNode.Name)
                                    .FinallyInUI((success, e) =>
                                    {
                                        if (success)
                                        {
                                            Redraw();
                                        }
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

        public override bool IsBusy
        {
            get { return false; }
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
