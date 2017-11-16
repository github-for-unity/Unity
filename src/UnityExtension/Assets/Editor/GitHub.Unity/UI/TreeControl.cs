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
    public class Tree
    {
        [SerializeField] public float ItemHeight = EditorGUIUtility.singleLineHeight;
        [SerializeField] public float ItemSpacing = EditorGUIUtility.standardVerticalSpacing;
        [SerializeField] public float Indentation = 12f;
        [SerializeField] public Rect Margin = new Rect();
        [SerializeField] public Rect Padding = new Rect();

        [SerializeField] private SerializableTexture2D activeNodeIcon = new SerializableTexture2D();
        public Texture2D ActiveNodeIcon {  get { return activeNodeIcon.Texture; } set { activeNodeIcon.Texture = value; } }

        [SerializeField] private SerializableTexture2D nodeIcon = new SerializableTexture2D();
        public Texture2D NodeIcon {  get { return nodeIcon.Texture; } set { nodeIcon.Texture = value; } }

        [SerializeField] private SerializableTexture2D folderIcon = new SerializableTexture2D();
        public Texture2D FolderIcon {  get { return folderIcon.Texture; } set { folderIcon.Texture = value; } }

        [SerializeField] private SerializableTexture2D rootFolderIcon = new SerializableTexture2D();
        public Texture2D RootFolderIcon {  get { return rootFolderIcon.Texture; } set { rootFolderIcon.Texture = value; } }

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
                        }

                        ResetNodeIcons(node);

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

        public Rect Render(Rect rect, Vector2 scroll, Action<TreeNode> singleClick = null, Action<TreeNode> doubleClick = null)
        {
            Profiler.BeginSample("TreeControl");
            bool visible = true;
            var availableHeight = rect.y + rect.height;

            RequiresRepaint = false;
            rect = new Rect(0f, rect.y, rect.width, ItemHeight);

            var titleNode = nodes[0];
            ResetNodeIcons(titleNode);
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
                ResetNodeIcons(node);

                if (node.Level > level && !node.IsHidden)
                {
                    Indent();
                }

                if (visible)
                {
                    var changed = node.Render(rect, Indentation, selectedNode == node, FolderStyle, TreeNodeStyle, ActiveTreeNodeStyle);

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
                        RequiresRepaint = HandleInput(rect, node, i, singleClick, doubleClick);
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

        private void ResetNodeIcons(TreeNode node)
        {
            if (node.IsActive)
            {
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
}
