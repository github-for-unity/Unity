using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    public class TreeNodeDictionary : SerializableDictionary<string, TreeNode> { }

    [Serializable]
    public abstract class Tree<TNode, TData>: TreeBase<TNode, TData>
        where TNode : TreeNode 
        where TData : struct, ITreeData
    {
        public static float ItemHeight { get { return EditorGUIUtility.singleLineHeight; } }
        public static float ItemSpacing { get { return EditorGUIUtility.standardVerticalSpacing; } }

        [NonSerialized] public GUIStyle FolderStyle;
        [NonSerialized] public GUIStyle TreeNodeStyle;
        [NonSerialized] public GUIStyle ActiveTreeNodeStyle;
        [NonSerialized] public GUIStyle FocusedTreeNodeStyle;
        [NonSerialized] public GUIStyle FocusedActiveTreeNodeStyle;

        [NonSerialized] private Stack<bool> indents = new Stack<bool>();
        [NonSerialized] private Action<TNode> rightClickNextRender;
        [NonSerialized] private TNode rightClickNextRenderNode;

        [NonSerialized] private int controlId;
        [NonSerialized] private bool isBusy;

        public bool IsInitialized { get { return Nodes != null && Nodes.Count > 0 && !String.IsNullOrEmpty(Nodes[0].Path); } }
        public bool RequiresRepaint { get; private set; }

        public Rect Render(Rect treeDisplayRect, Vector2 scroll, Action<TNode> singleClick = null, Action<TNode> doubleClick = null, Action<TNode> rightClick = null)
        {
            controlId = GUIUtility.GetControlID(FocusType.Keyboard);

            if (!Nodes.Any())
                return new Rect(treeDisplayRect.x, treeDisplayRect.y, 0f, 0f);

            var treeNodeStyle = TreeNodeStyle;
            var activeTreeNodeStyle = ActiveTreeNodeStyle;

            if (ViewHasFocus && TreeHasFocus)
            {
                treeNodeStyle = FocusedTreeNodeStyle;
                activeTreeNodeStyle = FocusedActiveTreeNodeStyle;
            }

            if (Event.current.type != EventType.Repaint)
            {
                if (rightClickNextRender != null)
                {
                    rightClickNextRender.Invoke(rightClickNextRenderNode);
                    rightClickNextRender = null;
                    rightClickNextRenderNode = null;
                }
            }

            var startDisplay = scroll.y;
            var endDisplay = scroll.y + treeDisplayRect.height;

            RequiresRepaint = false;
            var rect = new Rect(treeDisplayRect.x, treeDisplayRect.y, treeDisplayRect.width, ItemHeight);

            var level = 0;

            if (DisplayRootNode)
            {
                var titleNode = Nodes[0];
                var renderResult = TreeNodeRenderResult.None;

                var titleDisplay = !(rect.y > endDisplay || rect.yMax < startDisplay);
                if (titleDisplay)
                {
                    var isSelected = SelectedNode != null && SelectedNode.Path == titleNode.Path;
                    renderResult = titleNode.Render(rect, Styles.TreeIndentation, isSelected, IsBusy, FolderStyle, treeNodeStyle, activeTreeNodeStyle);
                }

                if (renderResult == TreeNodeRenderResult.VisibilityChange)
                {
                    ToggleNodeVisibility(0, titleNode);
                }
                else if (renderResult == TreeNodeRenderResult.CheckChange)
                {
                    ToggleNodeChecked(0, titleNode);
                }

                RequiresRepaint = HandleInput(rect, titleNode, 0);
                rect.y += ItemHeight + ItemSpacing;

                Indent();
                level = 1;
            }

            int i = 1;
            for (; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node.Level > level && !node.IsHidden)
                {
                    Indent();
                }

                var renderResult = TreeNodeRenderResult.None;

                var display = !(rect.y > endDisplay || rect.yMax < startDisplay);
                if (display)
                {
                    var isSelected = SelectedNode != null && SelectedNode.Path == node.Path;
                    renderResult = node.Render(rect, Styles.TreeIndentation, isSelected, IsBusy, FolderStyle, treeNodeStyle, activeTreeNodeStyle);
                }

                if (renderResult == TreeNodeRenderResult.VisibilityChange)
                {
                    ToggleNodeVisibility(i, node);
                }
                else if (renderResult == TreeNodeRenderResult.CheckChange)
                {
                    ToggleNodeChecked(i, node);
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
                    RequiresRepaint = HandleInput(rect, node, i, singleClick, doubleClick, rightClick);
                    rect.y += ItemHeight + ItemSpacing;
                }
            }

            if (DisplayRootNode)
            {
                Unindent();
            }

            return rect;
        }

        protected bool TreeHasFocus
        {
            get { return GUIUtility.keyboardControl == controlId; }
        }

        public abstract bool ViewHasFocus { get; set; }

        public bool IsBusy
        {
            get { return isBusy; }
            set { isBusy = value; }
        }

        public void Focus()
        {
            bool selectionChanged = false;
            if (IsSelectable && Event.current.type == EventType.KeyDown)
            {
                int directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                int directionX = Event.current.keyCode == KeyCode.LeftArrow ? -1 : Event.current.keyCode == KeyCode.RightArrow ? 1 : 0;

                if (directionY < 0 || directionX < 0)
                {
                    SelectedNode = Nodes[Nodes.Count - 1];
                    selectionChanged = true;
                }
                else if (directionY > 0 || directionX > 0)
                {
                    SelectedNode = Nodes[0];
                    selectionChanged = true;
                }
            }
            RequiresRepaint = selectionChanged;
        }

        public void Blur()
        {
            SelectedNode = null;
            RequiresRepaint = true;
        }

        private bool HandleInput(Rect rect, TNode currentNode, int index, Action<TNode> singleClick = null, Action<TNode> doubleClick = null, Action<TNode> rightClick = null)
        {
            var requiresRepaint = false;
            var clickRect = new Rect(0f, rect.y, rect.width, rect.height);
            if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                GUIUtility.keyboardControl = controlId;

                if (IsSelectable)
                {
                    SelectedNode = currentNode;
                }

                requiresRepaint = true;
                var clickCount = Event.current.clickCount;
                var mouseButton = Event.current.button;

                if (mouseButton == 0 && clickCount == 1 && singleClick != null)
                {
                    singleClick(currentNode);
                }
                if (mouseButton == 0 && clickCount > 1 && doubleClick != null)
                {
                    doubleClick(currentNode);
                }
                if (mouseButton == 1 && clickCount == 1 && rightClick != null)
                {
                    rightClickNextRender = rightClick;
                    rightClickNextRenderNode = currentNode;
                }
            }

            // Keyboard navigation if this child is the current selection
            if (IsSelectable && GUIUtility.keyboardControl == controlId && currentNode == SelectedNode && Event.current.type == EventType.KeyDown)
            {
                int directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                int directionX = Event.current.keyCode == KeyCode.LeftArrow ? -1 : Event.current.keyCode == KeyCode.RightArrow ? 1 : 0;
                if (directionY != 0 || directionX != 0)
                {
                    Event.current.Use();

                    if (directionY > 0)
                    {
                        requiresRepaint = SelectNext(index, false) != index;
                    }
                    else if (directionY < 0)
                    {
                        requiresRepaint = SelectPrevious(index, false) != index;
                    }
                    else if (directionX > 0)
                    {
                        if (currentNode.IsFolderOrContainer && currentNode.IsCollapsed)
                        {
                            ToggleNodeVisibility(index, currentNode);
                        }
                        else
                        {
                            requiresRepaint = SelectNext(index, true) != index;
                        }
                    }
                    else if (directionX < 0)
                    {
                        if (currentNode.IsFolderOrContainer && !currentNode.IsCollapsed)
                        {
                            ToggleNodeVisibility(index, currentNode);
                        }
                        else
                        {
                            requiresRepaint = SelectPrevious(index, true) != index;
                        }
                    }
                }

                if (IsCheckable && Event.current.keyCode == KeyCode.Space)
                {
                    Event.current.Use();

                    ToggleNodeChecked(index, currentNode);
                    requiresRepaint = true;
                }
            }

            return requiresRepaint;
        }

        private int SelectNext(int index, bool foldersOrContainersOnly)
        {
            for (index++; index < Nodes.Count; index++)
            {
                if (Nodes[index].IsHidden)
                    continue;
                if (!Nodes[index].IsFolderOrContainer && foldersOrContainersOnly)
                    continue;
                break;
            }

            if (index < Nodes.Count)
            {
                SelectedNode = Nodes[index];
            }
            else
            {
                SelectedNode = null;
            }
            return index;
        }

        private int SelectPrevious(int index, bool foldersOrContainersOnly)
        {
            for (index--; index >= 0; index--)
            {
                if (Nodes[index].IsHidden)
                    continue;
                if (!Nodes[index].IsFolderOrContainer && foldersOrContainersOnly)
                    continue;
                break;
            }

            if (index >= 0)
            {
                SelectedNode = Nodes[index];
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

        protected void LoadNodeIcons()
        {
            foreach (var treeNode in Nodes)
            {
                SetNodeIcon(treeNode);
            }
        }
    }

    [Serializable]
    public class TreeNode : ITreeNode
    {
        public string path;
        public string label;
        public int level;
        public bool isFolder;
        public bool isContainer;
        public bool isCollapsed;
        public bool isHidden;
        public bool isActive;
        public bool treeIsCheckable;
        public CheckState checkState;

        [NonSerialized] public GUIContent content;
        [NonSerialized] public Texture Icon;
        [NonSerialized] public Texture IconBadge;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        public int Level
        {
            get { return level; }
            set { level = value; }
        }

        public bool IsContainer
        {
            get { return isContainer; }
            set { isContainer = value; }
        }

        public bool IsFolder
        {
            get { return isFolder; }
            set { isFolder = value; }
        }

        public bool IsFolderOrContainer
        {
            get { return IsFolder || IsContainer; }
        }

        public bool IsCollapsed
        {
            get { return isCollapsed; }
            set { isCollapsed = value; }
        }

        public bool IsHidden
        {
            get { return isHidden; }
            set { isHidden = value; }
        }

        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        public bool TreeIsCheckable
        {
            get { return treeIsCheckable; }
            set { treeIsCheckable = value; }
        }

        public CheckState CheckState
        {
            get { return checkState; }
            set { checkState = value; }
        }

        public void Load()
        {
            content = new GUIContent(Label, Icon);
        }

        public TreeNodeRenderResult Render(Rect rect, float indentation, bool isSelected, bool treeIsBusy, GUIStyle toggleStyle, GUIStyle nodeStyle, GUIStyle activeNodeStyle)
        {
            var renderResult = TreeNodeRenderResult.None;

            if (treeIsBusy)
            {
                GUI.enabled = false;
            }

            if (IsHidden)
                return renderResult;

            var fillRect = rect;
            var nodeStartX = Level * indentation + rect.x;
            nodeStartX += 2 * level;

            var nodeRect = new Rect(nodeStartX, rect.y, fillRect.width - nodeStartX, rect.height);

            var reserveToggleSpace = TreeIsCheckable || isFolder;
            var toggleRect = new Rect(nodeStartX, nodeRect.y, reserveToggleSpace ? indentation : 0, nodeRect.height);

            nodeStartX += toggleRect.width;
            if (reserveToggleSpace)
            {
                nodeStartX += 2;
            }

            var checkRect = new Rect(nodeStartX, nodeRect.y, TreeIsCheckable ? indentation : 0, nodeRect.height);

            nodeStartX += checkRect.width;
            if (TreeIsCheckable)
            {
                nodeStartX += 2;
            }

            var iconRect = new Rect(nodeStartX, nodeRect.y, fillRect.width - nodeStartX, nodeRect.height);
            var statusRect = new Rect(iconRect.x + 6, iconRect.yMax - 9, 9, 9);

            var contentStyle = IsActive ? activeNodeStyle : nodeStyle;

            if (Event.current.type == EventType.Repaint)
            {
                contentStyle.Draw(fillRect, GUIContent.none, false, false, false, isSelected);
            }

            var styleOn = false;
            if (IsFolderOrContainer)
            {
                styleOn = !IsCollapsed;

                if (Event.current.type == EventType.Repaint)
                {
                    toggleStyle.Draw(toggleRect, GUIContent.none, false, false, styleOn, isSelected);
                }

                EditorGUI.BeginChangeCheck();
                {
                    GUI.Toggle(toggleRect, !IsCollapsed, GUIContent.none, GUIStyle.none);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    renderResult = TreeNodeRenderResult.VisibilityChange;
                }
            }

            if (TreeIsCheckable)
            {
                var selectionStyle = GUI.skin.toggle;
                var selectionValue = false;

                if (CheckState == CheckState.Checked)
                {
                    selectionValue = true;
                }
                else if (CheckState == CheckState.Mixed)
                {
                    selectionStyle = Styles.ToggleMixedStyle;
                }

                EditorGUI.BeginChangeCheck();
                {
                    GUI.Toggle(checkRect, selectionValue, GUIContent.none, selectionStyle);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    renderResult = TreeNodeRenderResult.CheckChange;
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                contentStyle.Draw(iconRect, content, false, false, false, isSelected);
            }

            if (IconBadge != null)
            {
                GUI.DrawTexture(statusRect, IconBadge);
            }

            if (treeIsBusy)
            {
                GUI.enabled = true;
            }

            return renderResult;
        }

        public override string ToString()
        {
            return String.Format("path:{0} label:{1} level:{2} isFolder:{3} isCollapsed:{4} isHidden:{5} isActive:{6}",
                Path, Label, Level, IsFolder, IsCollapsed, IsHidden, IsActive);
        }
    }

    [Serializable]
    public class BranchesTree : Tree<TreeNode, GitBranchTreeData>
    {
        [SerializeField] public bool IsRemote;
        [SerializeField] public TreeNodeDictionary folders = new TreeNodeDictionary();
        [SerializeField] public TreeNodeDictionary checkedFileNodes = new TreeNodeDictionary();

        [NonSerialized] public Texture2D ActiveBranchIcon;
        [NonSerialized] public Texture2D BranchIcon;
        [NonSerialized] public Texture2D FolderIcon;
        [NonSerialized] public Texture2D GlobeIcon;
        [SerializeField] public string title = string.Empty;
        [SerializeField] public string pathSeparator = "/";
        [SerializeField] public bool displayRootNode = true;
        [SerializeField] public bool isSelectable = true;
        [SerializeField] public bool isCheckable = false;
        [SerializeField] private List<TreeNode> nodes = new List<TreeNode>();
        [SerializeField] private TreeNode selectedNode = null;
        [NonSerialized] private bool viewFocus;

        public override string Title
        {
            get { return title; }
            set { title = value; }
        }

        public override bool DisplayRootNode
        {
            get { return displayRootNode; }
            set { displayRootNode = value; }
        }

        public override bool IsCheckable
        {
            get { return isCheckable; }
            set { isCheckable = value; }
        }

        public override bool IsSelectable
        {
            get { return isSelectable; }
            set { isSelectable = value; }
        }

        public override string PathSeparator
        {
            get { return pathSeparator; }
            set { pathSeparator = value; }
        }

        protected override bool PromoteMetaFiles
        {
            get { return false; }
        }

        public override TreeNode SelectedNode
        {
            get
            {
                if (selectedNode != null && String.IsNullOrEmpty(selectedNode.Path))
                    selectedNode = null;

                return selectedNode;
            }
            set
            {
                selectedNode = value;
            }
        }

        protected override List<TreeNode> Nodes
        {
            get { return nodes; }
        }

        public override bool ViewHasFocus
        {
            get { return viewFocus; }
            set { viewFocus = value; }
        }

        public void UpdateIcons(Texture2D activeBranchIcon, Texture2D branchIcon, Texture2D folderIcon, Texture2D globeIcon)
        {
            var needsLoad = ActiveBranchIcon == null || BranchIcon == null || FolderIcon == null || GlobeIcon == null;
            if (needsLoad)
            {
                ActiveBranchIcon = activeBranchIcon;
                BranchIcon = branchIcon;
                FolderIcon = folderIcon;
                GlobeIcon = globeIcon;

                LoadNodeIcons();
            }
        }

        protected override void SetNodeIcon(TreeNode node)
        {
            node.Icon = GetNodeIcon(node);
            node.Load();
        }

        protected Texture GetNodeIcon(TreeNode node)
        {
            Texture2D nodeIcon;
            if (node.IsActive)
            {
                nodeIcon = ActiveBranchIcon;
            }
            else if (node.IsFolder)
            {
                nodeIcon = IsRemote && node.Level == 1
                    ? GlobeIcon
                    : FolderIcon;
            }
            else
            {
                nodeIcon = BranchIcon;
            }
            return nodeIcon;
        }

        protected override TreeNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, bool isChecked, GitBranchTreeData? treeData)
        {
            var node = new TreeNode {
                Path = path,
                Label = label,
                Level = level,
                IsFolder = isFolder,
                IsActive = isActive,
                IsHidden = isHidden,
                IsCollapsed = isCollapsed,
                TreeIsCheckable = IsCheckable,
                CheckState = isChecked ? CheckState.Checked: CheckState.Empty
            };

            if (isFolder && level >= 0)
            {
                folders.Add(node.Path, node);
            }

            return node;
        }

        protected override void Clear()
        {
            folders.Clear();
            checkedFileNodes.Clear();
            base.Clear();
        }

        protected override IEnumerable<string> GetCollapsedFolders()
        {
            return folders.Where(pair => pair.Value.IsCollapsed).Select(pair => pair.Key);
        }

        public override IEnumerable<string> GetCheckedFiles()
        {
            return checkedFileNodes.Where(pair => pair.Value.CheckState == CheckState.Checked).Select(pair => pair.Key);
        }

        protected override void RemoveCheckedNode(TreeNode node)
        {
            checkedFileNodes.Remove(((ITreeNode)node).Path);
        }

        protected override void AddCheckedNode(TreeNode node)
        {
            checkedFileNodes.Add(((ITreeNode)node).Path, node);
        }
    }

    public enum TreeNodeRenderResult
    {
        None,
        VisibilityChange,
        CheckChange
    }
}
