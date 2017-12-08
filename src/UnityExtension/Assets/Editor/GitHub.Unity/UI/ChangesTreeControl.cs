using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    public class ChangesTreeNodeDictionary : SerializableDictionary<string, ChangesTreeNode> { }

    [Serializable]
    public class ChangesTreeNode : TreeNode
    {
        public string projectPath;
        public GitFileStatus gitFileStatus;

        public string ProjectPath
        {
            get { return projectPath; }
            set { projectPath = value; }
        }

        public GitFileStatus GitFileStatus
        {
            get { return gitFileStatus; }
            set { gitFileStatus = value; }
        }
    }

    [Serializable]
    public class ChangesTree : Tree<ChangesTreeNode, GitStatusEntryTreeData>
    {
        [SerializeField] public ChangesTreeNodeDictionary folders = new ChangesTreeNodeDictionary();
        [SerializeField] public ChangesTreeNodeDictionary checkedFileNodes = new ChangesTreeNodeDictionary();

        [NonSerialized] public Texture2D FolderIcon;

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
                    nodeIcon = AssetDatabase.GetCachedIcon(node.ProjectPath);
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

        protected Texture GetNodeIconBadge(ChangesTreeNode node)
        {
            if (node.IsFolder)
            {
                return null;
            }

            var gitFileStatus = node.GitFileStatus;
            return Styles.GetFileStatusIcon(gitFileStatus, false);
        }

        protected override ChangesTreeNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, GitStatusEntryTreeData? treeData)
        {
            var node = ScriptableObject.CreateInstance<ChangesTreeNode>();
            node.Path = path;
            node.Label = label;
            node.Level = level;
            node.IsFolder = isFolder;
            node.IsActive = isActive;
            node.IsHidden = isHidden;
            node.IsCollapsed = isCollapsed;
            node.TreeIsCheckable = IsCheckable;
            node.GitFileStatus = treeData.HasValue ? treeData.Value.FileStatus : GitFileStatus.None;
            node.ProjectPath = treeData.HasValue ? treeData.Value.ProjectPath : null;

            if (isFolder)
            {
                folders.Add(node.Path, node);
            }

            return node;
        }

        protected override void OnClear()
        {
            folders.Clear();
            checkedFileNodes.Clear();
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
    }
}
