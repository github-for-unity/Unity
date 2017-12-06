using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace GitHub.Unity
{
    [Serializable]
    public class TreeNodeDictionary : SerializableDictionary<string, TreeNode> { }


    [Serializable]
    public abstract class Tree: ITree
    {
        public static float ItemHeight { get { return EditorGUIUtility.singleLineHeight; } }
        public static float ItemSpacing { get { return EditorGUIUtility.standardVerticalSpacing; } }

        [SerializeField] public Rect Margin = new Rect();
        [SerializeField] public Rect Padding = new Rect();

        [SerializeField] public string title;
        [SerializeField] public string pathSeparator = "/";
        [SerializeField] public bool displayRootNode = true;
        [SerializeField] public bool isCheckable = false;
        [NonSerialized] public GUIStyle FolderStyle;
        [NonSerialized] public GUIStyle TreeNodeStyle;
        [NonSerialized] public GUIStyle ActiveTreeNodeStyle;

        [SerializeField] private List<TreeNode> nodes = new List<TreeNode>();
        [SerializeField] private TreeNode selectedNode = null;
        [SerializeField] private TreeNode activeNode = null;
        [SerializeField] private TreeNodeDictionary folders = new TreeNodeDictionary();

        [NonSerialized] private Stack<bool> indents = new Stack<bool>();
        [NonSerialized] private Action<TreeNode> rightClickNextRender;
        [NonSerialized] private TreeNode rightClickNextRenderNode;

        public bool IsInitialized { get { return nodes != null && nodes.Count > 0 && !String.IsNullOrEmpty(nodes[0].Path); } }
        public bool RequiresRepaint { get; private set; }

        public TreeNode SelectedNode
        {
            get
            {
                if (selectedNode != null && String.IsNullOrEmpty(selectedNode.Path))
                    selectedNode = null;
                return selectedNode;
            }
            private set
            {
                selectedNode = value;
            }
        }

        public string SelectedNodePath
        {
            get { return SelectedNode != null ? SelectedNode.Path : null; }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public bool DisplayRootNode
        {
            get { return displayRootNode; }
            set { displayRootNode = value; }
        }

        public bool IsCheckable
        {
            get { return isCheckable; }
            set { isCheckable = value; }
        }

        public string PathSeparator
        {
            get { return pathSeparator; }
            set { pathSeparator = value; }
        }

        public void AddNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, bool isSelected, int customIntTag = 0, string customStringTag = (string)null)
        {
            var node = new TreeNode
            {
                Path = path,
                Label = label,
                Level = level,
                IsFolder = isFolder,
                IsActive = isActive,
                IsHidden = isHidden,
                IsCollapsed = isCollapsed,
                TreeIsCheckable = IsCheckable,
                CustomStringTag = customStringTag,
                CustomIntTag = customIntTag
            };

            SetNodeIcon(node);
            nodes.Add(node);

            if (isActive)
            {
                activeNode = node;
            }

            if (isSelected)
            {
                SelectedNode = node;
            }

            if (isFolder)
            {
                folders.Add(node.Path, node);
            }
        }

        public void Clear()
        {
            folders.Clear();
            nodes.Clear();
            SelectedNode = null;
        }

        public HashSet<string> GetCollapsedFolders()
        {
            var collapsedFoldersEnumerable = folders.Where(pair => pair.Value.IsCollapsed).Select(pair => pair.Key);
            return new HashSet<string>(collapsedFoldersEnumerable);
        }

        public Rect Render(Rect containingRect, Rect rect, Vector2 scroll, Action<TreeNode> singleClick = null, Action<TreeNode> doubleClick = null, Action<TreeNode> rightClick = null)
        {
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
            var endDisplay = scroll.y + containingRect.height;

            RequiresRepaint = false;
            rect = new Rect(0f, rect.y, rect.width, ItemHeight);

            var level = 0;

            if (DisplayRootNode)
            {
                var titleNode = nodes[0];
                var renderResult = TreeNodeRenderResult.None;

                var titleDisplay = !(rect.y > endDisplay || rect.yMax < startDisplay);
                if (titleDisplay)
                {
                    renderResult = titleNode.Render(rect, Styles.TreeIndentation, selectedNode == titleNode, FolderStyle, TreeNodeStyle, ActiveTreeNodeStyle);
                }

                if (renderResult == TreeNodeRenderResult.VisibilityChange)
                {
                    ToggleNodeVisibility(0, titleNode);
                }
                else if (renderResult == TreeNodeRenderResult.CheckChange)
                {
                    ToggleNodeCheck(0, titleNode);
                }

                RequiresRepaint = HandleInput(rect, titleNode, 0);
                rect.y += ItemHeight + ItemSpacing;

                Indent();
                level = 1;
            }

            int i = 1;
            for (; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.Level > level && !node.IsHidden)
                {
                    Indent();
                }

                var renderResult = TreeNodeRenderResult.None;

                var display = !(rect.y > endDisplay || rect.yMax < startDisplay);
                if (display)
                {
                    renderResult = node.Render(rect, Styles.TreeIndentation, selectedNode == node, FolderStyle, TreeNodeStyle, ActiveTreeNodeStyle);
                }

                if (renderResult == TreeNodeRenderResult.VisibilityChange)
                {
                    ToggleNodeVisibility(i, node);
                }
                else if (renderResult == TreeNodeRenderResult.CheckChange)
                {
                    ToggleNodeCheck(i, node);
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

        public void Focus()
        {
            bool selectionChanged = false;
            if (Event.current.type == EventType.KeyDown)
            {
                int directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                int directionX = Event.current.keyCode == KeyCode.LeftArrow ? -1 : Event.current.keyCode == KeyCode.RightArrow ? 1 : 0;

                if (directionY < 0 || directionX < 0)
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
            RequiresRepaint = selectionChanged;
        }

        public void Blur()
        {
            SelectedNode = null;
            RequiresRepaint = true;
        }

        private void ToggleNodeCheck(int idx, TreeNode node)
        {
            if (node.IsFolder)
            {
                
            }
            else
            {
                switch (node.CheckState)
                {
                    case CheckState.Empty:
                        node.CheckState = CheckState.Checked;
                        break;

                    case CheckState.Checked:
                        node.CheckState = CheckState.Empty;
                        break;
                }

                Debug.LogFormat("Ripple CheckState index:{0} level:{1}", idx, node.Level);
            }
        }

        private void ToggleNodeVisibility(int idx, TreeNode node)
        {
            var nodeLevel = node.Level;
            node.IsCollapsed = !node.IsCollapsed;
            idx++;
            for (; idx < nodes.Count && nodes[idx].Level > nodeLevel; idx++)
            {
                nodes[idx].IsHidden = node.IsCollapsed;
                if (nodes[idx].IsFolder && !node.IsCollapsed && nodes[idx].IsCollapsed)
                {
                    var level = nodes[idx].Level;
                    for (idx++; idx < nodes.Count && nodes[idx].Level > level; idx++) { }
                    idx--;
                }
            }
            if (SelectedNode != null && SelectedNode.IsHidden)
            {
                SelectedNode = node;
            }
        }

        private bool HandleInput(Rect rect, TreeNode currentNode, int index, Action<TreeNode> singleClick = null, Action<TreeNode> doubleClick = null, Action<TreeNode> rightClick = null)
        {
            bool selectionChanged = false;
            var clickRect = new Rect(0f, rect.y, rect.width, rect.height);
            if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                SelectedNode = currentNode;
                selectionChanged = true;
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

        private void SetNodeIcon(TreeNode node)
        {
            node.Icon = GetNodeIcon(node);
            node.IconBadge = GetNodeIconBadge(node);
            node.Load();
        }

        protected abstract Texture GetNodeIcon(TreeNode node);
        protected abstract Texture GetNodeIconBadge(TreeNode node);

        protected void LoadNodeIcons()
        {
            foreach (var treeNode in nodes)
            {
                SetNodeIcon(treeNode);
            }
        }
    }

    [Serializable]
    public class TreeNode
    {
        public string Path;
        public string Label;
        public int Level;
        public bool IsFolder;
        public bool IsCollapsed;
        public bool IsHidden;
        public bool IsActive;
        public GUIContent content;
        public bool TreeIsCheckable;
        public CheckState CheckState;

        public string CustomStringTag;
        public int CustomIntTag;

        [NonSerialized] public Texture Icon;
        [NonSerialized] public Texture IconBadge;

        public void Load()
        {
            content = new GUIContent(Label, Icon);
        }

        public TreeNodeRenderResult Render(Rect rect, float indentation, bool isSelected, GUIStyle toggleStyle, GUIStyle nodeStyle, GUIStyle activeNodeStyle)
        {
            var renderResult = TreeNodeRenderResult.None;

            if (IsHidden)
                return renderResult;

            var fillRect = rect;
            var nodeStartX = Level * indentation * (TreeIsCheckable ? 2 : 1);

            if (TreeIsCheckable && Level > 0)
            {
                nodeStartX += 2 * Level;
            }

            var nodeRect = new Rect(nodeStartX, rect.y, rect.width, rect.height);

            var data = string.Format("Label: {0} ", Label);
            data += string.Format("Start: {0} ", nodeStartX);

            if (Event.current.type == EventType.repaint)
            {
                nodeStyle.Draw(fillRect, GUIContent.none, false, false, false, isSelected);
            }

            var styleOn = false;
            if (IsFolder)
            {
                data += string.Format("FolderStart: {0} ", nodeStartX);

                var toggleRect = new Rect(nodeStartX, nodeRect.y, indentation, nodeRect.height);
                nodeStartX += toggleRect.width;

                styleOn = !IsCollapsed;

                if (Event.current.type == EventType.repaint)
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
                data += string.Format("SelectStart: {0} ", nodeStartX);

                var selectRect = new Rect(nodeStartX, nodeRect.y, indentation, nodeRect.height);

                nodeStartX += selectRect.width + 2;

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
                    GUI.Toggle(selectRect, selectionValue, GUIContent.none, selectionStyle);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    renderResult = TreeNodeRenderResult.CheckChange;
                }
            }

            data += string.Format("ContentStart: {0} ", nodeStartX);
            var contentStyle = IsActive ? activeNodeStyle : nodeStyle;

            var contentRect = new Rect(nodeStartX, rect.y, rect.width, rect.height);
            if (Event.current.type == EventType.repaint)
            {
                contentStyle.Draw(contentRect, content, false, false, styleOn, isSelected);
            }

            if (IconBadge != null)
            {
                var statusRect = new Rect(
                    contentRect.x + 6,
                    contentRect.yMax - 7,
                    9,
                    9);

                GUI.DrawTexture(statusRect, IconBadge);
            }

            Debug.Log(data);

            return renderResult;
        }

        public override string ToString()
        {
            return String.Format("path:{0} label:{1} level:{2} isFolder:{3} isCollapsed:{4} isHidden:{5} isActive:{6}",
                Path, Label, Level, IsFolder, IsCollapsed, IsHidden, IsActive);
        }
    }

    [Serializable]
    public class BranchesTree : Tree
    {
        [SerializeField] public bool IsRemote;

        [NonSerialized] public Texture2D ActiveBranchIcon;
        [NonSerialized] public Texture2D BranchIcon;
        [NonSerialized] public Texture2D FolderIcon;
        [NonSerialized] public Texture2D GlobeIcon;

        protected override Texture GetNodeIcon(TreeNode node)
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

        protected override Texture GetNodeIconBadge(TreeNode node)
        {
            return null;
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
    }

    [Serializable]
    public class ChangesTree : Tree
    {
        [NonSerialized] public Texture2D FolderIcon;

        protected override Texture GetNodeIcon(TreeNode node)
        {
            Texture nodeIcon = null;
            if (node.IsFolder)
            {
                nodeIcon = FolderIcon;
            }
            else
            {
                if (!string.IsNullOrEmpty(node.CustomStringTag))
                {
                    nodeIcon = AssetDatabase.GetCachedIcon(node.CustomStringTag);
                }

                if (nodeIcon != null)
                {
                    nodeIcon.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    nodeIcon = Styles.DefaultAssetIcon;
                }
            }
            
            return nodeIcon;
        }

        protected override Texture GetNodeIconBadge(TreeNode node)
        {
            if (node.IsFolder)
            {
                return null;
            }

            var gitFileStatus = (GitFileStatus)node.CustomIntTag;
            return Styles.GetFileStatusIcon(gitFileStatus, false);
        }

        public void UpdateIcons(Texture2D activeBranchIcon, Texture2D branchIcon, Texture2D folderIcon, Texture2D globeIcon)
        {
            var needsLoad = FolderIcon == null;
            if (needsLoad)
            {
                FolderIcon = folderIcon;

                LoadNodeIcons();
            }
        }
    }

    public enum TreeNodeRenderResult
    {
        None,
        VisibilityChange,
        CheckChange
    }

    public enum CheckState
    {
        Empty,
        Checked,
        Mixed
    }
}
