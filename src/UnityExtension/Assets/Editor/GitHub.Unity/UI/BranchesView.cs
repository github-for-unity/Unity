using System;
using System.Collections;
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

        [NonSerialized] private int listID = -1;
        [NonSerialized] private BranchesMode targetMode;
        [NonSerialized] private List<string> favoritesList;

        [SerializeField] private Tree treeLocals = new Tree();
        [SerializeField] private Tree treeRemotes = new Tree();
        [SerializeField] private Tree treeFavorites = new Tree();
        [SerializeField] private BranchesMode mode = BranchesMode.Default;
        [SerializeField] private string newBranchName;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private bool disableDelete;

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
            if (!treeLocals.IsInitialized || branchCacheHasUpdate)
            {
                branchCacheHasUpdate = false;

                localBranches = Repository.LocalBranches.ToArray();
                remoteBranches = Repository.RemoteBranches.ToArray();


                BuildTree(localBranches, remoteBranches);
            }

            disableDelete = treeLocals.SelectedNode == null || treeLocals.SelectedNode.IsFolder || treeLocals.SelectedNode.IsActive;
        
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshBranchList();
        }

        public override void OnGUI()
        {
            Render();
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

        private void Render()
        {
            scroll = GUILayout.BeginScrollView(scroll, false, true);
            {
                listID = GUIUtility.GetControlID(FocusType.Keyboard);

                GUILayout.BeginHorizontal();
                {
                    OnButtonBarGUI();
                }
                GUILayout.EndHorizontal();

                var rect = GUILayoutUtility.GetLastRect();
                OnTreeGUI(new Rect(0f, rect.height + Styles.CommitAreaPadding, Position.width, Position.height - rect.height + Styles.CommitAreaPadding));
            }
            GUILayout.EndScrollView();
        }

        private void BuildTree(List<GitBranch> localBranches, List<GitBranch> remoteBranches)
        {
            localBranches.Sort(CompareBranches);
            remoteBranches.Sort(CompareBranches);
            treeLocals = new Tree();
            treeLocals.ActiveNodeIcon = Styles.ActiveBranchIcon;
            treeLocals.NodeIcon = Styles.BranchIcon;
            treeLocals.RootFolderIcon = Styles.RootFolderIcon;
            treeLocals.FolderIcon = Styles.FolderIcon;

            treeRemotes = new Tree();
            treeRemotes.ActiveNodeIcon = Styles.ActiveBranchIcon;
            treeRemotes.NodeIcon = Styles.BranchIcon;
            treeRemotes.RootFolderIcon = Styles.RootFolderIcon;
            treeRemotes.FolderIcon = Styles.FolderIcon;

            treeLocals.Load(localBranches.Cast<ITreeData>(), LocalTitle);
            treeRemotes.Load(remoteBranches.Cast<ITreeData>(), RemoteTitle);
            Redraw();
        }

        private void OnButtonBarGUI()
        {
            if (mode == BranchesMode.Default)
            {
                // Delete button
                // If the current branch is selected, then do not enable the Delete button
                EditorGUI.BeginDisabledGroup(disableDelete);
                {
                    if (GUILayout.Button(DeleteBranchButton, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        var selectedBranchName = treeLocals.SelectedNode.Name;
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
                    var cannotCreate = treeLocals.SelectedNode == null ||
                                       treeLocals.SelectedNode.IsFolder ||
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
                        GitClient.CreateBranch(newBranchName, treeLocals.SelectedNode.Name)
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

        private void OnTreeGUI(Rect rect)
        {
            if (!treeLocals.IsInitialized)
                RefreshBranchList();

            if (treeLocals.FolderStyle == null)
            {
                treeLocals.FolderStyle = Styles.Foldout;
                treeLocals.TreeNodeStyle = Styles.TreeNode;
                treeLocals.ActiveTreeNodeStyle = Styles.TreeNodeActive;
                treeRemotes.FolderStyle = Styles.Foldout;
                treeRemotes.TreeNodeStyle = Styles.TreeNode;
                treeRemotes.ActiveTreeNodeStyle = Styles.TreeNodeActive;
            }

            var treeHadFocus = treeLocals.SelectedNode != null;

            rect = treeLocals.Render(rect, _ => { }, node =>
                {
                    if (EditorUtility.DisplayDialog(ConfirmSwitchTitle, String.Format(ConfirmSwitchMessage, node.Name), ConfirmSwitchOK,
                            ConfirmSwitchCancel))
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
                });

            if (treeHadFocus && treeLocals.SelectedNode == null)
                treeRemotes.Focus();
            else if (!treeHadFocus && treeLocals.SelectedNode != null)
                treeRemotes.Blur();

            if (treeLocals.RequiresRepaint)
                Redraw();

            treeHadFocus = treeRemotes.SelectedNode != null;

            rect.y += Styles.TreePadding;

            treeRemotes.Render(rect, _ => {}, selectedNode =>
                {
                    var indexOfFirstSlash = selectedNode.Name.IndexOf('/');
                    var originName = selectedNode.Name.Substring(0, indexOfFirstSlash);
                    var branchName = selectedNode.Name.Substring(indexOfFirstSlash + 1);

                    if (Repository.LocalBranches.Any(localBranch => localBranch.Name == branchName))
                    {
                        EditorUtility.DisplayDialog(WarningCheckoutBranchExistsTitle,
                            String.Format(WarningCheckoutBranchExistsMessage, branchName),
                            WarningCheckoutBranchExistsOK);
                    }
                    else
                    {
                        var confirmCheckout = EditorUtility.DisplayDialog(ConfirmCheckoutBranchTitle,
                            String.Format(ConfirmCheckoutBranchMessage, selectedNode.Name, originName),
                            ConfirmCheckoutBranchOK,
                            ConfirmCheckoutBranchCancel);

                        if (confirmCheckout)
                        {
                            GitClient
                                .CreateBranch(branchName, selectedNode.Name)
                                .FinallyInUI((success, e) =>
                                {
                                    if (success)
                                    {
                                            Redraw();
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                            String.Format(Localization.SwitchBranchFailedDescription, selectedNode.Name),
                                            Localization.Ok);
                                    }
                                })
                                .Start();
                        }
                    }
                });

            if (treeHadFocus && treeRemotes.SelectedNode == null)
            {
                treeLocals.Focus();
            }
            else if (!treeHadFocus && treeRemotes.SelectedNode != null)
            {
                treeLocals.Blur();
            }

            if (treeRemotes.RequiresRepaint)
                Redraw();
        }

        private int CompareBranches(GitBranch a, GitBranch b)
        {
            //if (IsFavorite(a.Name))
            //{
            //    return -1;
            //}

            //if (IsFavorite(b.Name))
            //{
            //    return 1;
            //}

            if (a.Name.Equals("master"))
            {
                return -1;
            }

            if (b.Name.Equals("master"))
            {
                return 1;
            }

            return a.Name.CompareTo(b.Name);
        }

        //private bool IsFavorite(string branchName)
        //{
        //    return !String.IsNullOrEmpty(branchName) && favoritesList.Contains(branchName);
        //}

        //private void SetFavorite(TreeNode branch, bool favorite)
        //{
        //    if (string.IsNullOrEmpty(branch.Name))
        //    {
        //        return;
        //    }

        //    if (!favorite)
        //    {
        //        favorites.Remove(branch);
        //        Manager.LocalSettings.Set(FavoritesSetting, favorites.Select(x => x.Name).ToList());
        //    }
        //    else
        //    {
        //        favorites.Remove(branch);
        //        favorites.Add(branch);
        //        Manager.LocalSettings.Set(FavoritesSetting, favorites.Select(x => x.Name).ToList());
        //    }
        //}


        [Serializable]
        public class Tree
        {
            [SerializeField] private List<TreeNode> nodes = new List<TreeNode>();
            [SerializeField] private TreeNode selectedNode = null;
            [SerializeField] private TreeNode activeNode = null;
            [SerializeField] public float ItemHeight = EditorGUIUtility.singleLineHeight;
            [SerializeField] public float ItemSpacing = EditorGUIUtility.standardVerticalSpacing;
            [SerializeField] public float Indentation = 12f;
            [SerializeField] public Rect Margin = new Rect();
            [SerializeField] public Rect Padding = new Rect();
            [SerializeField] private List<string> foldersKeys = new List<string>();
            [SerializeField] public Texture2D ActiveNodeIcon;
            [SerializeField] public Texture2D NodeIcon;
            [SerializeField] public Texture2D FolderIcon;
            [SerializeField] public Texture2D RootFolderIcon;
            [SerializeField] public GUIStyle FolderStyle;
            [SerializeField] public GUIStyle TreeNodeStyle;
            [SerializeField] public GUIStyle ActiveTreeNodeStyle;

            [NonSerialized]
            private Stack<bool> indents = new Stack<bool>();
            [NonSerialized]
            private Hashtable folders;

            public bool IsInitialized { get { return nodes != null && nodes.Count > 0 && !String.IsNullOrEmpty(nodes[0].Name); } }
            public bool RequiresRepaint { get; private set; }

            public TreeNode SelectedNode
            {
                get
                {
                    if (selectedNode != null && String.IsNullOrEmpty(selectedNode.Name))
                        selectedNode = null;
                    return selectedNode;
                }
                private set
                {
                    selectedNode = value;
                }
            }

            public TreeNode ActiveNode { get { return activeNode; } }

            private Hashtable Folders
            {
                get
                {
                    if (folders == null)
                    {
                        folders = new Hashtable();
                        for (int i = 0; i < foldersKeys.Count; i++)
                        {
                            folders.Add(foldersKeys[i], null);
                        }
                    }
                    return folders;
                }
            }

            public void Load(IEnumerable<ITreeData> data, string title)
            {
                foldersKeys.Clear();
                Folders.Clear();
                nodes.Clear();

                var titleNode = new TreeNode()
                {
                    Name = title,
                    Label = title,
                    Level = 0,
                    IsFolder = true
                };
                titleNode.Load();
                nodes.Add(titleNode);

                foreach (var d in data)
                {
                    var parts = d.Name.Split('/');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var label = parts[i];
                        var name = String.Join("/", parts, 0, i + 1);
                        var isFolder = i < parts.Length - 1;
                        var alreadyExists = Folders.ContainsKey(name);
                        if (!alreadyExists)
                        {
                            var node = new TreeNode()
                            {
                                Name = name,
                                IsActive = d.IsActive,
                                Label = label,
                                Level = i + 1,
                                IsFolder = isFolder
                            };

                            if (node.IsActive)
                            {
                                activeNode = node;
                                node.Icon = ActiveNodeIcon;
                            }
                            else if (node.IsFolder)
                            {
                                if (node.Level == 1)
                                    node.Icon = RootFolderIcon;
                                else
                                    node.Icon = FolderIcon;
                            }
                            else
                            {
                                node.Icon = NodeIcon;
                            }

                            node.Load();

                            nodes.Add(node);
                            if (isFolder)
                            {
                                Folders.Add(name, null);
                            }
                        }
                    }
                }
                foldersKeys = Folders.Keys.Cast<string>().ToList();
            }

            public Rect Render(Rect rect, Action<TreeNode> singleClick = null, Action<TreeNode> doubleClick = null)
            {
                RequiresRepaint = false;
                rect = new Rect(0f, rect.y, rect.width, ItemHeight);

                var titleNode = nodes[0];
                bool selectionChanged = titleNode.Render(rect, 0f, selectedNode == titleNode, FolderStyle, TreeNodeStyle, ActiveTreeNodeStyle);

                if (selectionChanged)
                {
                    ToggleNodeVisibility(0, titleNode);
                }

                RequiresRepaint = HandleInput(rect, titleNode, 0);
                rect.y += ItemHeight + ItemSpacing;

                Indent();

                int level = 1;
                for (int i = 1; i < nodes.Count; i++)
                {
                    var node = nodes[i];

                    if (node.Level > level && !node.IsHidden)
                    {
                        Indent();
                    }

                    var changed = node.Render(rect, Indentation, selectedNode == node, FolderStyle, TreeNodeStyle, ActiveTreeNodeStyle);

                    if (node.IsFolder && changed)
                    {
                        // toggle visibility for all the nodes under this one
                        ToggleNodeVisibility(i, node);
                    }

                    if (node.Level < level)
                    {
                        for (; node.Level > level && indents.Count > 1; level--)
                        {
                            Unindent();
                        }
                    }
                    level = node.Level;

                    if (!node.IsHidden)
                    {
                        RequiresRepaint = HandleInput(rect, node, i, singleClick, doubleClick);
                        rect.y += ItemHeight + ItemSpacing;
                    }
                }

                Unindent();

                foldersKeys = Folders.Keys.Cast<string>().ToList();
                return rect;
            }

            public void Focus()
            {
                bool selectionChanged = false;
                if (Event.current.type == EventType.KeyDown)
                {
                    int directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                    int directionX = Event.current.keyCode == KeyCode.LeftArrow ? -1 : Event.current.keyCode == KeyCode.RightArrow ? 1 : 0;
                    if (directionY != 0 || directionX != 0)
                    {
                        if (directionY < 0 || directionY < 0)
                        {
                            SelectedNode = nodes[nodes.Count - 1];
                            selectionChanged = true;
                            Event.current.Use();
                        }
                        else if (directionY > 0 || directionX > 0)
                        {
                            SelectedNode = nodes[0];
                            selectionChanged = true;
                            Event.current.Use();
                        }
                    }
                }
                RequiresRepaint = selectionChanged;
            }

            public void Blur()
            {
                SelectedNode = null;
                RequiresRepaint = true;
            }

            private int ToggleNodeVisibility(int idx, TreeNode rootNode)
            {
                var rootNodeLevel = rootNode.Level;
                rootNode.IsCollapsed = !rootNode.IsCollapsed;
                idx++;
                for (; idx < nodes.Count && nodes[idx].Level > rootNodeLevel; idx++)
                {
                    nodes[idx].IsHidden = rootNode.IsCollapsed;
                    if (nodes[idx].IsFolder && !rootNode.IsCollapsed && nodes[idx].IsCollapsed)
                    {
                        var level = nodes[idx].Level;
                        for (idx++; idx < nodes.Count && nodes[idx].Level > level; idx++) { }
                        idx--;
                    }
                }
                if (SelectedNode != null && SelectedNode.IsHidden)
                {
                    SelectedNode = rootNode;
                }
                return idx;
            }

            private bool HandleInput(Rect rect, TreeNode currentNode, int index, Action<TreeNode> singleClick = null, Action<TreeNode> doubleClick = null)
            {
                bool selectionChanged = false;
                var clickRect = new Rect(0f, rect.y, rect.width, rect.height);
                if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    SelectedNode = currentNode;
                    selectionChanged = true;
                    var clickCount = Event.current.clickCount;
                    if (clickCount == 1 && singleClick != null)
                    {
                        singleClick(currentNode);
                    }
                    if (clickCount > 1 && doubleClick != null)
                    {
                        doubleClick(currentNode);
                    }
                }

                // Keyboard navigation if this child is the current selection
                if (currentNode == selectedNode && Event.current.type == EventType.KeyDown)
                {
                    int directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                    int directionX = Event.current.keyCode == KeyCode.LeftArrow ? -1 : Event.current.keyCode == KeyCode.RightArrow ? 1 : 0;
                    if (directionY != 0 || directionX != 0)
                    {
                        if (directionY > 0)
                        {
                            selectionChanged = SelectNext(index, false) != index;
                        }
                        else if (directionY < 0)
                        {
                            selectionChanged = SelectPrevious(index, false) != index;
                        }
                        else if (directionX > 0)
                        {
                            if (currentNode.IsFolder && currentNode.IsCollapsed)
                            {
                                ToggleNodeVisibility(index, currentNode);
                                Event.current.Use();
                            }
                            else
                            {
                                selectionChanged = SelectNext(index, true) != index;
                            }
                        }
                        else if (directionX < 0)
                        {
                            if (currentNode.IsFolder && !currentNode.IsCollapsed)
                            {
                                ToggleNodeVisibility(index, currentNode);
                                Event.current.Use();
                            }
                            else
                            {
                                selectionChanged = SelectPrevious(index, true) != index;
                            }
                        }
                    }
                }
                return selectionChanged;
            }

            private int SelectNext(int index, bool foldersOnly)
            {
                for (index++; index < nodes.Count; index++)
                {
                    if (nodes[index].IsHidden)
                        continue;
                    if (!nodes[index].IsFolder && foldersOnly)
                        continue;
                    break;
                }

                if (index < nodes.Count)
                {
                    SelectedNode = nodes[index];
                    Event.current.Use();
                }
                else
                {
                    SelectedNode = null;
                }
                return index;
            }

            private int SelectPrevious(int index, bool foldersOnly)
            {
                for (index--; index >= 0; index--)
                {
                    if (nodes[index].IsHidden)
                        continue;
                    if (!nodes[index].IsFolder && foldersOnly)
                        continue;
                    break;
                }

                if (index >= 0)
                {
                    SelectedNode = nodes[index];
                    Event.current.Use();
                }
                else
                {
                    SelectedNode = null;
                }
                return index;
            }

            private void Indent()
            {
                indents.Push(true);
            }

            private void Unindent()
            {
                indents.Pop();
            }
        }

        [Serializable]
        public class TreeNode
        {
            public string Name;
            public string Label;
            public int Level;
            public bool IsFolder;
            public bool IsCollapsed;
            public bool IsHidden;
            public bool IsActive;
            public GUIContent content;
            public Texture2D Icon;

            public void Load()
            {
                content = new GUIContent(Label, Icon);
            }

            public bool Render(Rect rect, float indentation, bool isSelected, GUIStyle folderStyle, GUIStyle nodeStyle, GUIStyle activeNodeStyle)
            {
                if (IsHidden)
                    return false;

                GUIStyle style;
                if (IsFolder)
                {
                    style = folderStyle;
                }
                else
                {
                    style = IsActive ? activeNodeStyle : nodeStyle;
                }

                bool changed = false;
                var fillRect = rect;
                var nodeRect = new Rect(Level * indentation, rect.y, rect.width, rect.height);

                if (Event.current.type == EventType.repaint)
                {
                    nodeStyle.Draw(fillRect, "", false, false, false, isSelected);
                    if (IsFolder)
                        style.Draw(nodeRect, content, false, false, !IsCollapsed, isSelected);
                    else
                    {
                        style.Draw(nodeRect, content, false, false, false, isSelected);
                    }
                }

                if (IsFolder)
                {
                    EditorGUI.BeginChangeCheck();
                    GUI.Toggle(nodeRect, !IsCollapsed, "", GUIStyle.none);
                    changed = EditorGUI.EndChangeCheck();
                }

                return changed;
            }

            public override string ToString()
            {
                return String.Format("name:{0} label:{1} level:{2} isFolder:{3} isCollapsed:{4} isHidden:{5} isActive:{6}",
                    Name, Label, Level, IsFolder, IsCollapsed, IsHidden, IsActive);
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
    }
}
