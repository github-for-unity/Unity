using System;
using System.Collections.Generic;
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

        [NonSerialized] private bool currentBranchHasUpdate;
        [NonSerialized] private bool currentStatusHasUpdate;
        [NonSerialized] private bool isBusy;

        [SerializeField] private string commitBody = "";
        [SerializeField] private string commitMessage = "";
        [SerializeField] private string currentBranch = "[unknown]";
        [SerializeField] private Vector2 scroll;
        [SerializeField] private CacheUpdateEvent lastCurrentBranchChangedEvent;
        [SerializeField] private CacheUpdateEvent lastStatusChangedEvent;
        [SerializeField] private Tree treeChanges;
        [SerializeField] private List<GitStatusEntry> gitStatusEntries;

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                Repository.CheckCurrentBranchChangedEvent(lastCurrentBranchChangedEvent);
                Repository.CheckStatusChangedEvent(lastStatusChangedEvent);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();

            MaybeUpdateData();
        }

        public override void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(false);
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

//                GUILayout.Label(
//                    tree.Entries.Count == 0
//                        ? NoChangedFilesLabel
//                        : tree.Entries.Count == 1
//                            ? OneChangedFileLabel
//                            : String.Format(ChangedFilesLabel, tree.Entries.Count), EditorStyles.miniLabel);
            }
            GUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            scroll = GUILayout.BeginScrollView(scroll);
            {
                OnTreeGUI(new Rect(0f, 0f, Position.width, Position.height - rect.height + Styles.CommitAreaPadding));
            }
            GUILayout.EndScrollView();

            // Do the commit details area
            OnCommitDetailsAreaGUI();
        }

        private void OnTreeGUI(Rect rect)
        {
            var initialRect = rect;

            if (treeChanges.FolderStyle == null)
            {
                treeChanges.FolderStyle = Styles.Foldout;
                treeChanges.TreeNodeStyle = Styles.TreeNode;
                treeChanges.ActiveTreeNodeStyle = Styles.TreeNodeActive;
            }

            var treeHadFocus = treeChanges.SelectedNode != null;

            rect = treeChanges.Render(rect, scroll,
                node => { },
                node => {
                },
                node => {
                });

            if (treeHadFocus && treeChanges.SelectedNode == null)
                treeChanges.Focus();
            else if (!treeHadFocus && treeChanges.SelectedNode != null)
                treeChanges.Blur();

            if (treeChanges.RequiresRepaint)
                Redraw();

            GUILayout.Space(rect.y - initialRect.y);
        }

        private void RepositoryOnStatusChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                lastStatusChangedEvent = cacheUpdateEvent;
                currentStatusHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnCurrentBranchChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentBranchChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCurrentBranchChangedEvent = cacheUpdateEvent;
                currentBranchHasUpdate = true;
                Redraw();
            }
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentBranchChanged += RepositoryOnCurrentBranchChanged;
            repository.StatusChanged += RepositoryOnStatusChanged;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentBranchChanged -= RepositoryOnCurrentBranchChanged;
            repository.StatusChanged -= RepositoryOnStatusChanged;
        }

        private void MaybeUpdateData()
        {
            if (currentBranchHasUpdate)
            {
                currentBranchHasUpdate = false;
                currentBranch = string.Format("[{0}]", Repository.CurrentBranchName);
            }

            if (currentStatusHasUpdate)
            {
                currentStatusHasUpdate = false;
                gitStatusEntries = Repository.CurrentStatus.Entries;

                BuildTree();
            }
        }

        private void BuildTree()
        {
            if (treeChanges == null)
            {
                treeChanges = new Tree();
                treeChanges.ActiveNodeIcon = Styles.ActiveBranchIcon;
                treeChanges.NodeIcon = Styles.BranchIcon;
                treeChanges.FolderIcon = Styles.FolderIcon;
                treeChanges.DisplayRootNode = false;
                treeChanges.PathIgnoreRoot = Environment.RepositoryPath + Environment.FileSystem.DirectorySeparatorChar;
                treeChanges.PathSeparator = Environment.FileSystem.DirectorySeparatorChar.ToString();
            }

            treeChanges.Load(gitStatusEntries.Select(entry => new GitStatusEntryTreeData(entry)).Cast<ITreeData>(), "Changes");
            Redraw();
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
                    //EditorGUI.BeginDisabledGroup(isBusy || string.IsNullOrEmpty(commitMessage) || !tree.CommitTargets.Any(t => t.Any));
                    EditorGUI.BeginDisabledGroup(isBusy || string.IsNullOrEmpty(commitMessage));
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(String.Format(CommitButton, currentBranch), Styles.CommitButtonStyle))
                            {
                                GUI.FocusControl(null);
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
//            for (var index = 0; index < tree.CommitTargets.Count; ++index)
//            {
//                tree.CommitTargets[index].All = true;
//            }
        }

        private void SelectNone()
        {
//            for (var index = 0; index < tree.CommitTargets.Count; ++index)
//            {
//                tree.CommitTargets[index].All = false;
//            }
        }

        private void Commit()
        {
            // Do not allow new commits before we have received one successful update
//            isBusy = true;
//
//            var files = Enumerable.Range(0, tree.Entries.Count)
//                .Where(i => tree.CommitTargets[i].All)
//                .Select(i => tree.Entries[i].Path)
//                .ToList();
//
//            ITask addTask;
//
//            if (files.Count == tree.Entries.Count)
//            {
//                addTask = Repository.CommitAllFiles(commitMessage, commitBody);
//            }
//            else
//            {
//                addTask = Repository.CommitFiles(files, commitMessage, commitBody);
//            }
//
//            addTask
//                .FinallyInUI((b, exception) => 
//                    {
//                        commitMessage = "";
//                        commitBody = "";
//                        isBusy = false;
//                    }).Start();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
