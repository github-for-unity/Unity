using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GitHub.Unity
{
    [Serializable]
    public class ChangesTreeNodeDictionary : SerializableDictionary<string, ChangesTreeNode> { }

    [Serializable]
    public class ChangesTreeNode : TreeNode
    {
        [SerializeField] private bool isLocked;
        [SerializeField] private GitStatusEntry gitStatusEntry;

        public bool IsLocked
        {
            get { return isLocked; }
            set { isLocked = value; }
        }

        public GitStatusEntry GitStatusEntry
        {
            get { return gitStatusEntry; }
            set { gitStatusEntry = value; }
        }

        public string ProjectPath { get { return GitStatusEntry.projectPath; } }
        public GitFileStatus GitFileStatus { get { return GitStatusEntry.status; } }
    }

    [Serializable]
    public class ChangesTree : Tree<ChangesTreeNode, GitStatusEntryTreeData>
    {
        [NonSerialized] public Texture2D FolderIcon;
        [NonSerialized] private bool viewHasFocus;
        [NonSerialized] private Object lastActivatedObject;

        [SerializeField] public ChangesTreeNodeDictionary assets = new ChangesTreeNodeDictionary();
        [SerializeField] public ChangesTreeNodeDictionary folders = new ChangesTreeNodeDictionary();
        [SerializeField] public ChangesTreeNodeDictionary checkedFileNodes = new ChangesTreeNodeDictionary();
        [SerializeField] public string title = string.Empty;
        [SerializeField] public string pathSeparator = "/";
        [SerializeField] public bool displayRootNode = true;
        [SerializeField] public bool isSelectable = true;
        [SerializeField] public bool isCheckable = false;
        [SerializeField] public bool isUsingGlobalSelection = false;
        [SerializeField] private List<ChangesTreeNode> nodes = new List<ChangesTreeNode>();
        [SerializeField] private ChangesTreeNode selectedNode = null;

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
            get { return true; }
        }

        public override ChangesTreeNode SelectedNode
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

                if (!IsUsingGlobalSelection)
                {
                    return;
                }

                var activeObject = selectedNode != null
                    ? AssetDatabase.LoadMainAssetAtPath(selectedNode.Path)
                    : null;

                lastActivatedObject = activeObject;

                if (TreeHasFocus)
                {
                    Selection.activeObject = activeObject;
                }
            }
        }

        protected override List<ChangesTreeNode> Nodes
        {
            get { return nodes; }
        }

        public bool IsUsingGlobalSelection
        {
            get { return isUsingGlobalSelection; }
            set { isUsingGlobalSelection = value; }
        }

        public override bool ViewHasFocus
        {
            get { return viewHasFocus; }
            set { viewHasFocus = value; }
        }

        public void UpdateIcons(Texture2D folderIcon)
        {
            var needsLoad = FolderIcon == null;
            if (needsLoad)
            {
                FolderIcon = folderIcon;

                LoadNodeIcons();
            }
        }

        protected override void SetNodeIcon(ChangesTreeNode node)
        {
            node.Icon = GetNodeIcon(node);
            node.IconBadge = GetNodeIconBadge(node);
            node.Load();
        }

        protected Texture GetNodeIcon(ChangesTreeNode node)
        {
            Texture nodeIcon = null;
            if (node.IsFolder)
            {
                nodeIcon = FolderIcon;
            }
            else
            {
                if (!string.IsNullOrEmpty(node.ProjectPath))
                {
                    nodeIcon = UnityEditorInternal.InternalEditorUtility.GetIconForFile(node.ProjectPath);
                }

                if (nodeIcon != null)
                {
                    nodeIcon.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            return nodeIcon;
        }

        protected Texture GetNodeIconBadge(ChangesTreeNode node)
        {
            if (node.IsFolder)
            {
                return null;
            }

            var gitFileStatus = node.GitFileStatus;
            return Styles.GetFileStatusIcon(gitFileStatus, node.IsLocked);
        }

        protected override ChangesTreeNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, bool isChecked, GitStatusEntryTreeData? treeData, bool isContainer)
        {
            var gitStatusEntry = GitStatusEntry.Default;
            var isLocked = false;

            if (treeData.HasValue)
            {
                isLocked = treeData.Value.IsLocked;
                gitStatusEntry = treeData.Value.GitStatusEntry;
            }

            var node = new ChangesTreeNode
            {
                Path = path,
                Label = label,
                Level = level,
                IsFolder = isFolder,
                IsActive = isActive,
                IsHidden = isHidden,
                IsCollapsed = isCollapsed,
                TreeIsCheckable = IsCheckable,
                CheckState = isChecked ? CheckState.Checked : CheckState.Empty,
                GitStatusEntry = gitStatusEntry,
                IsLocked = isLocked,
            };

            if (isFolder && level >= 0)
            {
                folders.Add(node.Path, node);
            }

            if (IsUsingGlobalSelection)
            {
                if (node.Path != null)
                {
                    var assetGuid = AssetDatabase.AssetPathToGUID(node.Path);

                    if (!string.IsNullOrEmpty(assetGuid))
                    {
                        assets.Add(assetGuid, node);
                    }
                }
            }

            return node;
        }

        protected override void Clear()
        {
            assets.Clear();
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

        protected override void RemoveCheckedNode(ChangesTreeNode node)
        {
            checkedFileNodes.Remove(((ITreeNode)node).Path);
        }

        protected override void AddCheckedNode(ChangesTreeNode node)
        {
            checkedFileNodes.Add(((ITreeNode)node).Path, node);
        }

        public bool OnSelectionChange()
        {
            if (IsUsingGlobalSelection && !TreeHasFocus)
            {
                ChangesTreeNode assetNode = null;

                if (Selection.activeObject != lastActivatedObject)
                {
                    var activeAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    var activeAssetGuid = AssetDatabase.AssetPathToGUID(activeAssetPath);

                    assets.TryGetValue(activeAssetGuid, out assetNode);
                }

                SelectedNode = assetNode;
                return true;
            }

            return false;
        }
    }
}
