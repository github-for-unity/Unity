using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    public interface ITree
    {
        void AddNode(string fullPath, string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed);
        void Clear();
        HashSet<string> GetCollapsedFolders();
        string Title { get; }
        bool DisplayRootNode { get; }
        bool IsCheckable { get; }
        string PathSeparator { get; }
    }

    public static class TreeLoader
    {
        public static void Load(ITree tree, IEnumerable<ITreeData> treeDatas)
        {
            var collapsedFolders = tree.GetCollapsedFolders();

            tree.Clear();

            var displayRootLevel = tree.DisplayRootNode ? 1 : 0;

            tree.AddNode(fullPath: tree.Title, path: tree.Title, label: tree.Title, level: -1 + displayRootLevel, isFolder: true, isActive: false, isHidden: false, isCollapsed: false);

            var hideChildren = false;
            var hideChildrenBelowLevel = 0;

            var folders = new HashSet<string>();

            foreach (var treeData in treeDatas)
            {
                var parts = treeData.Path.Split(new[] { tree.PathSeparator }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    var label = parts[i];
                    var level = i + 1;
                    var nodePath = String.Join(tree.PathSeparator, parts, 0, level);
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

                        var nodeIsCollapsed = false;
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

                        tree.AddNode(fullPath: treeData.FullPath, path: nodePath, label: label, level: i + displayRootLevel, isFolder: isFolder, isActive: treeData.IsActive, isHidden: nodeIsHidden, isCollapsed: nodeIsCollapsed);
                    }
                }
            }
        }

    }
}