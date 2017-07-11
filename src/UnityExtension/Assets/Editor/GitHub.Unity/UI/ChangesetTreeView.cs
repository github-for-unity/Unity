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
        [NonSerialized] private Action<FileTreeNode> stateChangeCallback;

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

            tree = TreeBuilder.BuildTreeRoot(newEntries, entries, entryCommitTargets, foldedTreeEntries, stateChangeCallback);

            OnCommitTreeChange();
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
                        var filesAdded = new List<string>();
                        var filesRemoved = new List<string>();
                        stateChangeCallback = new Action<FileTreeNode>(s =>
                        {
                            if (s.State == CommitState.None)
                                filesRemoved.Add(s.Path);
                            else
                                filesAdded.Add(s.Path);
                        });
                        node.State = toggled ? CommitState.All : CommitState.None;
                        if (filesAdded.Count > 0)
                            GitClient.Add(filesAdded);
                        if (filesRemoved.Count > 0)
                            GitClient.Remove(filesAdded);
                        if (filesAdded.Count > 0|| filesRemoved.Count > 0)
                        {
                            GitClient.Status();
                        }
                        // we might need to run git status after these calls
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
                        var icon = (Texture) node.Icon ?? (isFolder ? Styles.FolderIcon : Styles.DefaultAssetIcon);
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
    }
}
