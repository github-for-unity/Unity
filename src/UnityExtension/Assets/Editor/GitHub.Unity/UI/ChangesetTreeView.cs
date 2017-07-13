#pragma warning disable 649

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class ChangesetTreeView : Subview
    {
        private const string BasePathLabel = "{0}";
        private const string NoChangesLabel = "No changes found";

        [SerializeField] private List<GitStatusEntry> entries = new List<GitStatusEntry>();
        [SerializeField] private List<GitCommitTarget> entryCommitTargets = new List<GitCommitTarget>();
        [SerializeField] private List<string> foldedTreeEntries = new List<string>();
        [NonSerialized] private FileTreeNode tree;

        public override void OnGUI()
        {
            GUILayout.BeginVertical();
            {
                // The file tree (when available)
                if (tree != null && entries.Any())
                {
                    // Base path label
                    if (!string.IsNullOrEmpty(tree.Path))
                    {
                        GUILayout.BeginHorizontal();
                        {
                            var iconRect = GUILayoutUtility.GetRect(Styles.CommitIconSize, Styles.CommitIconSize, GUILayout.ExpandWidth(false));
                            iconRect.y += 2;
                            iconRect.x += 2;

                            GUI.DrawTexture(iconRect, Styles.FolderIcon, ScaleMode.ScaleToFit);

                            GUILayout.Label(string.Format(BasePathLabel, tree.Path));
                        }
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(Styles.TreeIndentation + Styles.TreeRootIndentation);
                        GUILayout.BeginVertical();
                        {
                            // Root nodes
                            foreach (var node in tree.Children)
                            {
                                TreeNode(node);
                            }
                        }

                        GUILayout.EndVertical();
                    }

                    GUILayout.EndHorizontal();

                    // If we have no minimum height calculated, do that now and repaint so it can be used
                    if (Height == 0f && Event.current.type == EventType.Repaint)
                    {
                        Height = GUILayoutUtility.GetLastRect().yMax + Styles.MinCommitTreePadding;
                        Redraw();
                    }

                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(NoChangesLabel);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.EndVertical();
        }

        private void OnCommitTreeChange()
        {
            Height = 0f;
            Redraw();
            Redraw();
        }

        public void UpdateEntries(IList<GitStatusEntry> newEntries)
        {
            // Handle the empty list scenario
            if (!newEntries.Any())
            {
                entries.Clear();
                entryCommitTargets.Clear();
                tree = null;
                foldedTreeEntries.Clear();

                OnCommitTreeChange();

                return;
            }

            // Remove what got nuked
            for (var index = 0; index < entries.Count;)
            {
                if (!newEntries.Contains(entries[index]))
                {
                    entries.RemoveAt(index);
                    entryCommitTargets.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // Remove folding state of nuked items
            for (var index = 0; index < foldedTreeEntries.Count;)
            {
                if (!newEntries.Any(e => e.Path.IndexOf(foldedTreeEntries[index]) == 0))
                {
                    foldedTreeEntries.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // Add new stuff
            for (var index = 0; index < newEntries.Count; ++index)
            {
                var entry = newEntries[index];
                if (!entries.Contains(entry))
                {
                    entries.Add(entry);
                    entryCommitTargets.Add(new GitCommitTarget());
                }
            }

            // TODO: Filter .meta files - consider adding them as children of the asset or folder they're supporting

            // TODO: In stead of completely rebuilding the tree structure, figure out a way to migrate open/closed states from the old tree to the new

            // Build tree structure

            tree = new FileTreeNode(FileSystemHelpers.FindCommonPath(entries.Select(e => e.Path)));
            tree.RepositoryPath = tree.Path;
            for (var index = 0; index < entries.Count; index++)
            {
                var entryPath = entries[index].Path.ToNPath();
                if (entryPath.IsChildOf(tree.Path))
                    entryPath = entryPath.RelativeTo(tree.Path.ToNPath());

                var node = new FileTreeNode(entryPath) { Target = entryCommitTargets[index] };
                if (!string.IsNullOrEmpty(entries[index].ProjectPath))
                {
                    node.Icon = AssetDatabase.GetCachedIcon(entries[index].ProjectPath);
                }

                BuildTree(tree, node);
            }

            OnCommitTreeChange();
        }

        private void BuildTree(FileTreeNode parent, FileTreeNode node)
        {
            if (string.IsNullOrEmpty(node.Label))
            {
                // TODO: We should probably reassign this target onto the parent? Depends on how we want to handle .meta files for folders
                return;
            }

            node.RepositoryPath = parent.RepositoryPath.ToNPath().Combine(node.Label);
            parent.Open = !foldedTreeEntries.Contains(parent.RepositoryPath);

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
                        BuildTree(child, node);
                        break;
                    }
                }

                // No existing branch - we will have to add a new one to build from
                if (!found)
                {
                    var p = parent.RepositoryPath.ToNPath().Combine(root);
                    BuildTree(parent.Add(new FileTreeNode(root) { RepositoryPath = p }), node);
                }
            }
            else if (nodePath.ExtensionWithDot == ".meta")
            {
                // Look for a branch matching our root in the existing children
                var found = false;
                foreach (var child in parent.Children)
                {
                    // If we found the branch, continue building from that branch
                    if (child.Label.Equals(nodePath.Parent.Combine(nodePath.FileNameWithoutExtension)))
                    {
                        found = true;
                        BuildTree(child, node);
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

        private void TreeNode(FileTreeNode node)
        {
            GUILayout.Space(Styles.TreeVerticalSpacing);
            var target = node.Target;
            var isFolder = node.Children.Any();

            GUILayout.BeginHorizontal();
            {
                if (!Readonly)
                {
                    // Commit inclusion toggle
                    var state = node.State;
                    var toggled = state == CommitState.All;

                    EditorGUI.BeginChangeCheck();
                    {
                        toggled = GUILayout.Toggle(toggled, "", state == CommitState.Some ? Styles.ToggleMixedStyle : GUI.skin.toggle,
                            GUILayout.ExpandWidth(false));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        node.State = toggled ? CommitState.All : CommitState.None;
                    }
                }

                // Foldout
                if (isFolder)
                {
                    Rect foldoutRect;

                    if (Readonly)
                    {
                        foldoutRect = GUILayoutUtility.GetRect(1, 1);
                        foldoutRect.Set(foldoutRect.x - 7f, foldoutRect.y + 3f, 0f, EditorGUIUtility.singleLineHeight);
                    }
                    else
                    {
                        foldoutRect = GUILayoutUtility.GetLastRect();
                    }

                    foldoutRect.Set(foldoutRect.x - Styles.FoldoutWidth + Styles.FoldoutIndentation, foldoutRect.y, Styles.FoldoutWidth,
                        foldoutRect.height);

                    EditorGUI.BeginChangeCheck();
                    {
                        node.Open = GUI.Toggle(foldoutRect, node.Open, "", EditorStyles.foldout);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!node.Open && !foldedTreeEntries.Contains(node.RepositoryPath))
                        {
                            foldedTreeEntries.Add(node.RepositoryPath);
                        }
                        else if (node.Open)
                        {
                            foldedTreeEntries.Remove(node.RepositoryPath);
                        }

                        OnCommitTreeChange();
                    }
                }

                GitFileStatus? status = null;

                // Node icon and label
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(Styles.CommitIconHorizontalPadding);
                    var iconRect = GUILayoutUtility.GetRect(Styles.CommitIconSize, Styles.CommitIconSize, GUILayout.ExpandWidth(false));
                    iconRect.y += 2;
                    iconRect.x -= 2;

                    if (Event.current.type == EventType.Repaint)
                    {
                        var icon = node.Icon ?? (isFolder ? Styles.FolderIcon : Styles.DefaultAssetIcon);
                        if (icon != null)
                        {
                            GUI.DrawTexture(iconRect,
                                icon,
                                ScaleMode.ScaleToFit);
                        }
                    }

                    var statusRect = new Rect(
                        iconRect.xMax - 9,
                        iconRect.yMax - 7,
                        9,
                        9);

                    // Current status (if any)
                    if (target != null)
                    {
                        var idx = entryCommitTargets.IndexOf(target);
                        if (idx > -1)
                        {
                            status = entries[idx].Status;
                            var statusIcon = Styles.GetFileStatusIcon(entries[idx].Status, false);
                            if (statusIcon != null)
                                GUI.DrawTexture(statusRect, statusIcon);
                        }
                    }

                    GUILayout.Space(Styles.CommitIconHorizontalPadding);
                }
                GUILayout.EndHorizontal();

                // Make the text gray and strikethrough if the file is deleted
                if (status == GitFileStatus.Deleted)
                {
                    GUILayout.Label(new GUIContent(node.Label, node.RepositoryPath), Styles.DeletedFileLabel, GUILayout.ExpandWidth(true));
                    var labelRect = GUILayoutUtility.GetLastRect();
                    var strikeRect = new Rect(labelRect.xMin, labelRect.center.y, labelRect.width, 1);
                    EditorGUI.DrawRect(strikeRect, Color.gray);
                }
                else
                {
                    GUILayout.Label(new GUIContent(node.Label, node.RepositoryPath), GUILayout.ExpandWidth(true));
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                // Render children (if any and folded out)
                if (isFolder && node.Open)
                {
                    GUILayout.Space(Styles.TreeIndentation);
                    GUILayout.BeginVertical();
                    {
                        foreach (var child in node.Children)
                        {
                            TreeNode(child);
                        }
                    }

                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndHorizontal();
        }

        public float Height { get; protected set; }
        public bool Readonly { get; set; }

        public IList<GitStatusEntry> Entries
        {
            get { return entries; }
        }

        public IList<GitCommitTarget> CommitTargets
        {
            get { return entryCommitTargets; }
        }

        private enum CommitState
        {
            None,
            Some,
            All
        }

        private class FileTreeNode
        {
            private List<FileTreeNode> children;
            private string path;
            private CommitState state;

            public Texture Icon;
            public string Label;
            public bool Open = true;
            public string RepositoryPath;
            public GitCommitTarget Target { get; set; }

            public FileTreeNode(string path)
            {
                this.path = path ?? "";
                Label = this.path;
                children = new List<FileTreeNode>();
            }

            public FileTreeNode Add(FileTreeNode child)
            {
                children.Add(child);
                return child;
            }

            public CommitState State
            {
                get
                {
                    if (children == null)
                        return state;

                    var commitState = CommitState.None;
                    if (Target != null)
                    {
                        commitState = Target.All ? CommitState.All : Target.Any ? CommitState.Some : CommitState.None;
                        if (!children.Any())
                        {
                            state = commitState;
                            return state;
                        }
                    }

                    var allCount = children.Count(c => c.State == CommitState.All);

                    if (allCount == children.Count && (commitState == CommitState.All || Target == null))
                    {
                        state = CommitState.All;
                        return state;
                    }

                    if (allCount > 0 || commitState == CommitState.Some)
                    {
                        state = CommitState.Some;
                        return state;
                    }

                    var someCount = children.Count(c => c.State == CommitState.Some);
                    if (someCount > 0 || commitState == CommitState.Some)
                    {
                        state = CommitState.Some;
                        return state;
                    }
                    state = CommitState.None;
                    return state;
                }

                set
                {
                    if (value == state)
                    {
                        return;
                    }

                    if (Target != null)
                    {
                        if (value == CommitState.None)
                        {
                            Target.Clear();
                        }
                        else if (value == CommitState.All)
                        {
                            Target.All = true;
                        }
                    }

                    state = value;

                    if (children == null)
                    {
                        return;
                    }

                    for (var index = 0; index < children.Count; ++index)
                    {
                        children[index].State = value;
                    }
                }
            }

            public string Path
            {
                get { return path; }
            }

            public IEnumerable<FileTreeNode> Children
            {
                get {
                    if (children == null)
                        children = new List<FileTreeNode>();
                    return children;
                }
            }

            private ILogging logger;
            protected ILogging Logger
            {
                get
                {
                    if (logger == null)
                        logger = Logging.GetLogger(GetType());
                    return logger;
                }
            }
        }
    }
}
