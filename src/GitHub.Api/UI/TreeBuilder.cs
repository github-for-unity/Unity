using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    static class TreeBuilder
    {
        internal static void BuildChildNode(FileTreeNode parent, FileTreeNode node, HashSet<string> foldedTreeSet)
        {
            if (String.IsNullOrEmpty(node.Label))
            {
                // TODO: We should probably reassign this target onto the parent? Depends on how we want to handle .meta files for folders
                return;
            }

            node.RepositoryPath = parent.RepositoryPath.ToNPath().Combine(node.Label);
            parent.Open = !foldedTreeSet.Contains(parent.RepositoryPath);

            // Is this node inside a folder?
            var nodePath = node.Label.ToNPath();
            if (nodePath.Elements.Count() > 1)
            {
                // Figure out what the root folder is and chop it from the path
                var root = nodePath.Elements.First();
                node.Label = new NPath("").Combine(nodePath.Elements.Skip(1).ToArray());

                // Look for a branch matching our root in the existing children
                var found = false;
                foreach (var child in parent.Children)
                {
                    // If we found the branch, continue building from that branch
                    if (child.Label.Equals(root))
                    {
                        found = true;
                        BuildChildNode(child, node, foldedTreeSet);
                        break;
                    }
                }

                // No existing branch - we will have to add a new one to build from
                if (!found)
                {
                    var p = parent.RepositoryPath.ToNPath().Combine(root);
                    BuildChildNode(parent.Add(new FileTreeNode(root) { RepositoryPath = p }), node, foldedTreeSet);
                }
            }
            else if (nodePath.ExtensionWithDot == ".meta")
            {
                // Look for a branch matching our root in the existing children
                var found = false;
                var searchLabel = nodePath.Parent.Combine(nodePath.FileNameWithoutExtension);
                foreach (var child in parent.Children)
                {
                    // If we found the branch, continue building from that branch
                    if (child.Label.Equals(searchLabel))
                    {
                        found = true;
                        BuildChildNode(child, node, foldedTreeSet);
                        break;
                    }
                }
                if (!found)
                {
                    parent.Add(node);
                }
            }
            // Not inside a folder - just add this node right here
            else
            {
                parent.Add(node);
            }
        }

        internal static FileTreeNode BuildTreeRoot(IList<GitStatusEntry> newEntries, List<GitStatusEntry> gitStatusEntries, List<GitCommitTarget> gitCommitTargets, List<string> foldedTreeEntries, Func<string, object> iconLoaderFunc = null)
        {
            Guard.ArgumentNotNullOrEmpty(newEntries, "newEntries");

            var newEntriesSetByPath = new HashSet<string>(newEntries.Select(entry => entry.Path));
            var gitStatusEntriesSetByPath = new HashSet<string>(gitStatusEntries.Select(entry => entry.Path));

            // Remove what got nuked
            for (var index = 0; index < gitStatusEntries.Count;)
            {
                if (!newEntriesSetByPath.Contains(gitStatusEntries[index].Path))
                {
                    gitStatusEntries.RemoveAt(index);
                    gitCommitTargets.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // Remove folding state of nuked items
            for (var index = 0; index < foldedTreeEntries.Count;)
            {
                if (newEntries.All(e => e.Path.IndexOf(foldedTreeEntries[index], StringComparison.CurrentCulture) != 0))
                {
                    foldedTreeEntries.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            var foldedTreeSet = new HashSet<string>(foldedTreeEntries);

            // Add new stuff
            for (var index = 0; index < newEntries.Count; ++index)
            {
                var entry = newEntries[index];
                if (!gitStatusEntriesSetByPath.Contains(entry.Path))
                {
                    gitStatusEntries.Add(entry);
                    gitCommitTargets.Add(new GitCommitTarget());
                }
            }

            // TODO: In stead of completely rebuilding the tree structure, figure out a way to migrate open/closed states from the old tree to the new
            // Build tree structure

            var tree = new FileTreeNode(FileSystemHelpers.FindCommonPath(gitStatusEntries.Select(e => e.Path)));
            tree.RepositoryPath = tree.Path;

            for (var index1 = 0; index1 < gitStatusEntries.Count; index1++)
            {
                var gitStatusEntry = gitStatusEntries[index1];
                var entryPath = gitStatusEntry.Path.ToNPath();
                if (entryPath.IsChildOf(tree.Path)) entryPath = entryPath.RelativeTo(tree.Path.ToNPath());

                var node = new FileTreeNode(entryPath) { Target = gitCommitTargets[index1] };
                if (!String.IsNullOrEmpty(gitStatusEntry.ProjectPath))
                {
                    node.Icon = iconLoaderFunc?.Invoke(gitStatusEntry.ProjectPath);
                }

                BuildChildNode(tree, node, foldedTreeSet);
            }

            return tree;
        }
    }
}