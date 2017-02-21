#pragma warning disable 649

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
        [SerializeField] private Vector2 verticalScroll;
        [SerializeField] private ChangesetTreeView tree = new ChangesetTreeView();

        public override void Initialize(IView parent)
        {
            base.Initialize(parent);
            tree.Initialize(this);
        }

        public override void OnShow()
        {
            base.OnShow();
            EntryPoint.Environment.Repository.OnRepositoryChanged += RunStatusUpdateOnMainThread;
        }

        public override void OnHide()
        {
            base.OnHide();
            EntryPoint.Environment.Repository.OnRepositoryChanged -= RunStatusUpdateOnMainThread;
        }

        private void RunStatusUpdateOnMainThread(GitStatus status)
        {
            Tasks.ScheduleMainThread(() => OnStatusUpdate(status));
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

        private void OnStatusUpdate(GitStatus update)
        {
            if (update.Entries == null)
            {
                Refresh();
                return;
            }

            // Set branch state
            currentBranch = update.LocalBranch;

            // (Re)build tree
            tree.UpdateEntries(update.Entries);

            lockCommit = false;
        }

        private void OnCommitDetailsAreaGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(Styles.CommitAreaPadding);

                GUILayout.BeginVertical(GUILayout.Height(
                        Mathf.Clamp(Position.height * Styles.CommitAreaDefaultRatio,
                        Styles.CommitAreaMinHeight,
                        Styles.CommitAreaMaxHeight))
                );
                {
                    GUILayout.Space(Styles.CommitAreaPadding);

                    GUILayout.Label(SummaryLabel);
                    commitMessage = EditorGUILayout.TextField(commitMessage, Styles.TextFieldStyle);

                    GUILayout.Space(Styles.CommitAreaPadding * 2);

                    GUILayout.Label(DescriptionLabel);
                    commitBody = EditorGUILayout.TextArea(commitBody, Styles.CommitDescriptionFieldStyle, GUILayout.ExpandHeight(true));

                    GUILayout.Space(Styles.CommitAreaPadding);

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

                    GUILayout.Space(Styles.CommitAreaPadding);
                }
                GUILayout.EndVertical();

                GUILayout.Space(Styles.CommitAreaPadding);
            }
            GUILayout.EndHorizontal();
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

            // Schedule the commit with the added files
            var files = Enumerable.Range(0, tree.Entries.Count).Where(i => tree.CommitTargets[i].All).Select(i => tree.Entries[i].Path);

            var commitTask = new GitCommitTask(EntryPoint.Environment, EntryPoint.ProcessManager,
                                    new MainThreadTaskResultDispatcher<string>(_ => {
                                        commitMessage = "";
                                        commitBody = "";
                                        for (var index = 0; index < tree.Entries.Count; ++index)
                                        {
                                            tree.CommitTargets[index].Clear();
                                        }
                                    },
                                () => lockCommit = false),
                                commitMessage,
                                commitBody);

            // run add, then commit
            var addTask = new GitAddTask(EntryPoint.Environment, EntryPoint.ProcessManager,
                            TaskResultDispatcher.Default.GetDispatcher<string>(_ => Tasks.Add(commitTask)),
                            files);
            Tasks.Add(addTask);
        }
    }
}
