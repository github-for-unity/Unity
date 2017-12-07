using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    public enum CheckState
    {
        Empty,
        Checked,
        Mixed
    }

    public interface ITreeNode
    {
        string Path { get; set; }
        string Label { get; set; }
        int Level { get; set; }
        bool IsFolder { get; set; }
        bool IsCollapsed { get; set; }
        bool IsHidden { get; set; }
        bool IsActive { get; set; }
        bool TreeIsCheckable { get; set; }
        CheckState State { get; set; }
    }

    public abstract class TreeBase<TNode, TData> where TNode : class, ITreeNode where TData : struct, ITreeData
    {
        public abstract IEnumerable<string> GetCheckedFiles();

        public void Load(IEnumerable<TData> treeDatas)
        {
            var collapsedFolders = new HashSet<string>(GetCollapsedFolders());
            var selectedNodePath = SelectedNodePath;

            Clear();

            var displayRootLevel = DisplayRootNode ? 1 : 0;

            var isSelected = selectedNodePath != null && Title == selectedNodePath;
            AddNode(Title, Title, -1 + displayRootLevel, true, false, false, false, isSelected, null);

            var hideChildren = false;
            var hideChildrenBelowLevel = 0;

            var folders = new HashSet<string>();

            foreach (var treeData in treeDatas)
            {
                var parts = treeData.Path.Split(new[] { PathSeparator }, StringSplitOptions.None);
                for (var i = 0; i < parts.Length; i++)
                {
                    var label = parts[i];
                    var level = i + 1;
                    var nodePath = String.Join(PathSeparator, parts, 0, level);
                    var isFolder = i < parts.Length - 1;
                    var alreadyExists = folders.Contains(nodePath);
                    if (!alreadyExists)
                    {
                        var nodeIsHidden = false;
                        if (hideChildren)
                        {
                            if (level <= hideChildrenBelowLevel)
                            {
                                hideChildren = false;
                            }
                            else
                            {
                                nodeIsHidden = true;
                            }
                        }

                        var isActive = false;
                        var nodeIsCollapsed = false;
                        TData? treeNodeTreeData = null;

                        if (isFolder)
                        {
                            folders.Add(nodePath);

                            if (collapsedFolders.Contains(nodePath))
                            {
                                nodeIsCollapsed = true;

                                if (!hideChildren)
                                {
                                    hideChildren = true;
                                    hideChildrenBelowLevel = level;
                                }
                            }
                        }
                        else
                        {
                            isActive = treeData.IsActive;
                            treeNodeTreeData = treeData;
                        }

                        isSelected = selectedNodePath != null && nodePath == selectedNodePath;
                        AddNode(nodePath, label, i + displayRootLevel, isFolder, isActive, nodeIsHidden,
                            nodeIsCollapsed, isSelected, treeNodeTreeData);
                    }
                }
            }
        }

        protected abstract IEnumerable<string> GetCollapsedFolders();

        protected void AddNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden,
            bool isCollapsed, bool isSelected, TData? treeData)
        {
            var node = CreateTreeNode(path, label, level, isFolder, isActive, isHidden, isCollapsed, treeData);

            SetNodeIcon(node);
            Nodes.Add(node);

            if (isSelected)
            {
                SelectedNode = node;
            }
        }

        protected void Clear()
        {
            OnClear();
            Nodes.Clear();
            SelectedNode = null;
        }

        protected abstract void RemoveCheckedNode(TNode node);

        protected abstract void AddCheckedNode(TNode node);

        protected abstract TNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive,
            bool isHidden, bool isCollapsed, TData? treeData);

        protected abstract void OnClear();

        protected abstract void SetNodeIcon(TNode node);

        protected void ToggleNodeVisibility(int idx, TNode node)
        {
            var nodeLevel = node.Level;
            node.IsCollapsed = !node.IsCollapsed;
            idx++;
            for (; idx < Nodes.Count && Nodes[idx].Level > nodeLevel; idx++)
            {
                Nodes[idx].IsHidden = node.IsCollapsed;
                if (Nodes[idx].IsFolder && !node.IsCollapsed && Nodes[idx].IsCollapsed)
                {
                    var level = Nodes[idx].Level;
                    for (idx++; idx < Nodes.Count && Nodes[idx].Level > level; idx++)
                    { }

                    idx--;
                }
            }

            if (SelectedNode != null && SelectedNode.IsHidden)
            {
                SelectedNode = node;
            }
        }

        protected void ToggleNodeChecked(int idx, TNode node)
        {
            var isChecked = false;

            switch (node.State)
            {
                case CheckState.Mixed:
                case CheckState.Empty:
                    node.State = CheckState.Checked;
                    isChecked = true;
                    break;

                case CheckState.Checked:
                    node.State = CheckState.Empty;
                    break;
            }

            if (node.IsFolder)
            {
                ToggleChildrenChecked(idx, node, isChecked);
            }
            else
            {
                if (isChecked)
                {
                    AddCheckedNode(node);
                }
                else
                {
                    RemoveCheckedNode(node);
                }
            }

            ToggleParentFoldersChecked(idx, node, isChecked);
        }

        private void ToggleChildrenChecked(int idx, TNode node, bool isChecked)
        {
            for (var i = idx + 1; i < Nodes.Count && node.Level < Nodes[i].Level; i++)
            {
                var childNode = Nodes[i];
                var wasChecked = childNode.State == CheckState.Checked;
                childNode.State = isChecked ? CheckState.Checked : CheckState.Empty;

                if (childNode.IsFolder)
                {
                    ToggleChildrenChecked(i, childNode, isChecked);
                }
                else
                {
                    if (isChecked && !wasChecked)
                    {
                        AddCheckedNode(childNode);
                    }
                    else if (!isChecked && wasChecked)
                    {
                        RemoveCheckedNode(childNode);
                    }
                }
            }
        }

        private void ToggleParentFoldersChecked(int idx, TNode node, bool isChecked)
        {
            while (true)
            {
                if (node.Level > 0)
                {
                    var siblingsInSameState = true;
                    var firstSiblingIndex = idx;

                    for (var i = idx - 1; i > 0 && node.Level <= Nodes[i].Level; i--)
                    {
                        var previousNode = Nodes[i];
                        if (node.Level < previousNode.Level)
                        {
                            continue;
                        }

                        firstSiblingIndex = i;

                        if (siblingsInSameState)
                        {
                            var previousNodeIsChecked = previousNode.State == CheckState.Checked;

                            if (isChecked != previousNodeIsChecked)
                            {
                                siblingsInSameState = false;
                            }
                        }
                    }

                    if (siblingsInSameState)
                    {
                        for (var i = idx + 1; i < Nodes.Count && node.Level <= Nodes[i].Level; i++)
                        {
                            var followingNode = Nodes[i];
                            if (node.Level < followingNode.Level)
                            {
                                continue;
                            }

                            var followingNodeIsChecked = followingNode.State == CheckState.Checked;
                            if (isChecked != followingNodeIsChecked)
                            {
                                siblingsInSameState = false;
                                break;
                            }
                        }
                    }

                    var parentIndex = firstSiblingIndex - 1;
                    var parentNode = Nodes[parentIndex];
                    if (siblingsInSameState)
                    {
                        parentNode.State = isChecked ? CheckState.Checked : CheckState.Empty;
                    }
                    else
                    {
                        parentNode.State = CheckState.Mixed;
                    }

                    idx = parentIndex;
                    node = parentNode;
                    continue;
                }

                break;
            }
        }

        public string SelectedNodePath => SelectedNode?.Path;
        public abstract TNode SelectedNode { get; set; }
        protected abstract List<TNode> Nodes { get; }
        public abstract string Title { get; set; }
        public abstract bool DisplayRootNode { get; set; }
        public abstract bool IsCheckable { get; set; }
        public abstract string PathSeparator { get; set; }
    }
}
