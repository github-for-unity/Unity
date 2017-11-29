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
    public abstract class Tree
    {
        public static float ItemHeight { get { return EditorGUIUtility.singleLineHeight; } }
        public static float ItemSpacing { get { return EditorGUIUtility.standardVerticalSpacing; } }

        [SerializeField] public Rect Margin = new Rect();
        [SerializeField] public Rect Padding = new Rect();

        [SerializeField] public GUIStyle FolderStyle;
        [SerializeField] public GUIStyle TreeNodeStyle;
        [SerializeField] public GUIStyle ActiveTreeNodeStyle;

        [SerializeField] private List<TreeNode> nodes = new List<TreeNode>();
        [SerializeField] private TreeNode selectedNode = null;
        [SerializeField] private TreeNode activeNode = null;
        [SerializeField] private List<string> foldersKeys = new List<string>();

        [NonSerialized] private Stack<bool> indents = new Stack<bool>();
        [NonSerialized] private Hashtable folders;

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
            SetNodeIcon(titleNode);
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
                        }

                        SetNodeIcon(node);

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

        public Rect Render(Rect rect, Vector2 scroll, Action<TreeNode> singleClick = null, Action<TreeNode> doubleClick = null, Action<TreeNode> rightClick = null)
        {
            Profiler.BeginSample("TreeControl");
            bool visible = true;
            var availableHeight = rect.y + rect.height;

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
            int i = 1;
            for (; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.Level > level && !node.IsHidden)
                {
                    Indent();
                }

                if (visible)
                {
                    var changed = node.Render(rect, Styles.TreeIndentation, selectedNode == node, FolderStyle, TreeNodeStyle, ActiveTreeNodeStyle);

                    if (node.IsFolder && changed)
                    {
                        // toggle visibility for all the nodes under this one
                        ToggleNodeVisibility(i, node);
                    }
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
                    if (visible)
                    {
                        RequiresRepaint = HandleInput(rect, node, i, singleClick, doubleClick, rightClick);
                    }
                    rect.y += ItemHeight + ItemSpacing;
                }
            }

            Unindent();

            Profiler.EndSample();
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
                    rightClick(currentNode);
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
            node.Load();
        }

        protected abstract Texture2D GetNodeIcon(TreeNode node);

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
        public string Name;
        public string Label;
        public int Level;
        public bool IsFolder;
        public bool IsCollapsed;
        public bool IsHidden;
        public bool IsActive;
        public GUIContent content;
        [NonSerialized] public Texture2D Icon;

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
                nodeStyle.Draw(fillRect, GUIContent.none, false, false, false, isSelected);
                if (IsFolder)
                {
                    style.Draw(nodeRect, content, false, false, !IsCollapsed, isSelected);
                }
                else
                {
                    style.Draw(nodeRect, content, false, false, false, isSelected);
                }
            }

            if (IsFolder)
            {
                var toggleRect = new Rect(nodeRect.x, nodeRect.y, style.border.horizontal, nodeRect.height);

                EditorGUI.BeginChangeCheck();
                GUI.Toggle(toggleRect, !IsCollapsed, GUIContent.none, GUIStyle.none);
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

    [Serializable]
    public class BranchesTree: Tree
    {
        [SerializeField] public bool IsRemote;
        
        [NonSerialized] public Texture2D ActiveNodeIcon;
        [NonSerialized] public Texture2D BranchIcon;
        [NonSerialized] public Texture2D FolderIcon;
        [NonSerialized] public Texture2D RemoteIcon;

        protected override Texture2D GetNodeIcon(TreeNode node)
        {
            Texture2D nodeIcon;
            if (node.IsActive)
            {
                nodeIcon = ActiveNodeIcon;
            }
            else if (node.IsFolder)
            {
                nodeIcon = IsRemote && node.Level == 1
                    ? RemoteIcon
                    : FolderIcon;
            }
            else
            {
                nodeIcon = BranchIcon;
            }
            return nodeIcon;
        }


        public void UpdateIcons(Texture2D activeBranchIcon, Texture2D branchIcon, Texture2D folderIcon, Texture2D rootFolderIcon)
        {
            var localsLoaded = false;

            if (ActiveNodeIcon == null)
            {
                localsLoaded = true;
                ActiveNodeIcon = activeBranchIcon;
            }

            if (BranchIcon == null)
            {
                localsLoaded = true;
                BranchIcon = branchIcon;
            }

            if (FolderIcon == null)
            {
                localsLoaded = true;
                FolderIcon = folderIcon;
            }

            if (RemoteIcon == null)
            {
                localsLoaded = true;
                RemoteIcon = rootFolderIcon;
            }

            if (localsLoaded)
            {
                LoadNodeIcons();
            }
        }
    }
}
