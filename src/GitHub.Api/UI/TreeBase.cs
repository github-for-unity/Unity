using System;
using System.Collections.Generic;
using System.Linq;

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
        CheckState CheckState { get; set; }
    }

    public abstract class TreeBase<TNode, TData> where TNode : class, ITreeNode where TData : struct, ITreeData
    {
        protected ILogging Logger { get; }

        protected TreeBase()
        {
            Logger = Logging.GetLogger(GetType());
        }

        public abstract IEnumerable<string> GetCheckedFiles();

        public void Load(IEnumerable<TData> treeDatas)
        {
            Logger.Trace("Load");

            var collapsedFolders = new HashSet<string>(GetCollapsedFolders());
            var selectedNodePath = SelectedNodePath;
            var checkedFiles = new HashSet<string>(GetCheckedFiles());
            var pathSeparator = PathSeparator;

            Clear();

            var displayRootLevel = DisplayRootNode ? 1 : 0;

            var isCheckable = IsCheckable;

            var isSelected = IsSelectable && selectedNodePath != null && Title == selectedNodePath;
            AddNode(Title, Title, -1 + displayRootLevel, true, false, false, false, isSelected, false, null);

            var hideChildren = false;
            var hideChildrenBelowLevel = 0;

            var folders = new HashSet<string>();

            foreach (var treeData in treeDatas)
            {
                var parts = treeData.Path.Split(new[] { pathSeparator }, StringSplitOptions.None);
                for (var i = 0; i < parts.Length; i++)
                {
                    var label = parts[i];
                    var level = i + 1;
                    var nodePath = String.Join(pathSeparator, parts, 0, level);
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
                        var isChecked = false;
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
                            isChecked = isCheckable && checkedFiles.Contains(nodePath);
                        }

                        isSelected = selectedNodePath != null && nodePath == selectedNodePath;
                        AddNode(nodePath, label, i + displayRootLevel, isFolder, isActive, nodeIsHidden,
                            nodeIsCollapsed, isSelected, isChecked, treeNodeTreeData);
                    }
                }
            }

            if (isCheckable && checkedFiles.Any())
            {
                var nodes = Nodes;
                for (var index = nodes.Count - 1; index >= 0; index--)
                {
                    var node = nodes[index];
                    if (node.Level >= 0 && node.IsFolder)
                    {
                        bool? anyChecked = null;
                        bool? allChecked = null;

                        for (var i = index + 1; i < nodes.Count; i++)
                        {
                            var nodeCompare = nodes[i];
                            if (nodeCompare.Level < node.Level + 1)
                            {
                                break;
                            }

                            var nodeIsChecked = nodeCompare.CheckState == CheckState.Checked;
                            allChecked = (allChecked ?? true) && nodeIsChecked;
                            anyChecked = (anyChecked ?? false) || nodeIsChecked;
                        }

                        node.CheckState = anyChecked.Value 
                            ? (allChecked.Value ? CheckState.Checked : CheckState.Mixed)
                            : CheckState.Empty;
                    }
                }
            }
        }

        public void SetCheckStateOnAll(bool isChecked)
        {
            var nodeCheckState = isChecked ? CheckState.Checked : CheckState.Empty;
            foreach (var node in Nodes)
            {
                var wasChecked = node.CheckState == CheckState.Checked;
                node.CheckState = nodeCheckState;

                if (!node.IsFolder)
                {
                    if (isChecked && !wasChecked)
                    {
                        AddCheckedNode(node);
                    }
                    else if (!isChecked && wasChecked)
                    {
                        RemoveCheckedNode(node);
                    }
                }
            }
        }

        protected abstract IEnumerable<string> GetCollapsedFolders();

        protected void AddNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, bool isSelected, bool isChecked, TData? treeData)
        {
            var node = CreateTreeNode(path, label, level, isFolder, isActive, isHidden, isCollapsed, isChecked, treeData);

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

        protected abstract TNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, bool isChecked, TData? treeData);

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

            switch (node.CheckState)
            {
                case CheckState.Mixed:
                case CheckState.Empty:
                    node.CheckState = CheckState.Checked;
                    isChecked = true;
                    break;

                case CheckState.Checked:
                    node.CheckState = CheckState.Empty;
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
                var wasChecked = childNode.CheckState == CheckState.Checked;
                childNode.CheckState = isChecked ? CheckState.Checked : CheckState.Empty;

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
                            var previousNodeIsChecked = previousNode.CheckState == CheckState.Checked;

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

                            var followingNodeIsChecked = followingNode.CheckState == CheckState.Checked;
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
                        parentNode.CheckState = isChecked ? CheckState.Checked : CheckState.Empty;
                    }
                    else
                    {
                        parentNode.CheckState = CheckState.Mixed;
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
        public abstract bool IsSelectable { get; set; }
        public abstract bool IsCheckable { get; set; }
        public abstract string PathSeparator { get; set; }
    }
}
