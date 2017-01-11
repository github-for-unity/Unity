using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class ChangesView : Subview
    {
        private const string SummaryLabel = "Commit summary";
        private const string DescriptionLabel = "Commit description";
        private const string CommitButton = "Commit to <b>{0}</b>";
        private const string SelectAllButton = "All";
        private const string SelectNoneButton = "None";
        private const string ChangedFilesLabel = "{0} changed files";
        private const string OneChangedFileLabel = "1 changed file";
        private const string NoChangedFilesLabel = "No changed files";

        [NonSerialized] private bool lockCommit = true;
        [SerializeField] private string commitBody = "";

        [SerializeField] private string commitMessage = "";
        [SerializeField] private string currentBranch = "[unknown]";
        [SerializeField] private Vector2 horizontalScroll;
        [SerializeField] private ChangesetTreeView tree = new ChangesetTreeView();
        [SerializeField] private Vector2 verticalScroll;

        public override void Refresh()
        {
            GitStatusTask.Schedule();
        }

        public override void OnGUI()
        {
            var scroll = verticalScroll;
            scroll = GUILayout.BeginScrollView(verticalScroll);
            {
                if (tree.Height > 0)
                {
                    verticalScroll = scroll;
                }

                GUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(tree.Entries.Count == 0);
                    {
                        if (GUILayout.Button(SelectAllButton, EditorStyles.miniButtonLeft))
                        {
                            SelectAll();
                        }

                        if (GUILayout.Button(SelectNoneButton, EditorStyles.miniButtonRight))
                        {
                            SelectNone();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.FlexibleSpace();

                    GUILayout.Label(
                        tree.Entries.Count == 0
                            ? NoChangedFilesLabel
                            : tree.Entries.Count == 1 ? OneChangedFileLabel : String.Format(ChangedFilesLabel, tree.Entries.Count),
                        EditorStyles.miniLabel);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
                {
                    // Specify a minimum height if we can - avoiding vertical scrollbars on both the outer and inner scroll view
                    if (tree.Height > 0)
                    {
                        horizontalScroll = GUILayout.BeginScrollView(horizontalScroll, GUILayout.MinHeight(tree.Height),
                            GUILayout.MaxHeight(100000f)
                            // NOTE: This ugliness is necessary as unbounded MaxHeight appears impossible when MinHeight is specified
                            );
                    }

                    else // if we have no minimum height to work with, just stretch and hope
                    {
                        horizontalScroll = GUILayout.BeginScrollView(horizontalScroll);
                    }

                    {// scroll view block started above
                        tree.OnGUI();
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                // Do the commit details area
                OnCommitDetailsAreaGUI();
            }
            GUILayout.EndScrollView();
        }

        protected override void OnShow()
        {
            tree.Show(this);
            GitStatusTask.RegisterCallback(OnStatusUpdate);
        }

        protected override void OnHide()
        {
            GitStatusTask.UnregisterCallback(OnStatusUpdate);
        }

        private void OnStatusUpdate(GitStatus update)
        {
            // Set branch state
            currentBranch = update.LocalBranch;

            // (Re)build tree
            tree.Update(update.Entries);

            lockCommit = false;
        }

        private void OnCommitDetailsAreaGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(
                    Mathf.Clamp(position.height * Styles.CommitAreaDefaultRatio,
                    Styles.CommitAreaMinHeight,
                    Styles.CommitAreaMaxHeight))
            );
            {
                GUILayout.Label(SummaryLabel);
                commitMessage = GUILayout.TextField(commitMessage);

                GUILayout.Label(DescriptionLabel);
                commitBody = EditorGUILayout.TextArea(commitBody, Styles.CommitDescriptionFieldStyle, GUILayout.ExpandHeight(true));

                // Disable committing when already committing or if we don't have all the data needed
                EditorGUI.BeginDisabledGroup(lockCommit || string.IsNullOrEmpty(commitMessage) || !tree.CommitTargets.Any(t => t.Any));
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(String.Format(CommitButton, currentBranch), Styles.CommitButtonStyle))
                        {
                            Commit();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndVertical();
        }

        private void SelectAll()
        {
            for (var index = 0; index < tree.CommitTargets.Count; ++index)
            {
                tree.CommitTargets[index].All = true;
            }
        }

        private void SelectNone()
        {
            for (var index = 0; index < tree.CommitTargets.Count; ++index)
            {
                tree.CommitTargets[index].All = false;
            }
        }

        private void Commit()
        {
            // Do not allow new commits before we have received one successful update
            lockCommit = true;

            // Schedule the commit with the selected files
            GitCommitTask.Schedule(
                Enumerable.Range(0, tree.Entries.Count).Where(i => tree.CommitTargets[i].All).Select(i => tree.Entries[i].Path),
                commitMessage,
                commitBody,
                () => {
                    commitMessage = "";
                    commitBody = "";
                    for (var index = 0; index < tree.Entries.Count; ++index)
                    {
                        tree.CommitTargets[index].Clear();
                    }
                },
                () => lockCommit = false
            );
        }
    }
}
