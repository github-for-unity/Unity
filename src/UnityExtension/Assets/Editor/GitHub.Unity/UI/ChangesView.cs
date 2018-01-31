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
        [NonSerialized] private bool currentStatusEntriesHasUpdate;
        [NonSerialized] private bool currentLocksHasUpdate;
        [NonSerialized] private bool isBusy;

        [NonSerialized] private GUIContent revertGuiContent;

        [SerializeField] private string commitBody = "";
        [SerializeField] private string commitMessage = "";
        [SerializeField] private string currentBranch = "[unknown]";

        [SerializeField] private Vector2 treeScroll;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { DisplayRootNode = false, IsCheckable = true, IsUsingGlobalSelection = true };

        [SerializeField] private HashSet<string> gitLocks;
        [SerializeField] private List<GitStatusEntry> gitStatusEntries;

        [SerializeField] private string changedFilesText = NoChangedFilesLabel;

        [SerializeField] private CacheUpdateEvent lastCurrentBranchChangedEvent;
        [SerializeField] private CacheUpdateEvent lastStatusEntriesChangedEvent;
        [SerializeField] private CacheUpdateEvent lastLocksChangedEvent;

        public override void OnEnable()
        {
            base.OnEnable();

            if (treeChanges != null)
            {
                treeChanges.ViewHasFocus = HasFocus;
                treeChanges.UpdateIcons(Styles.FolderIcon);
            }

            AttachHandlers(Repository);
            Repository.CheckCurrentBranchChangedEvent(lastCurrentBranchChangedEvent);
            Repository.CheckStatusEntriesChangedEvent(lastStatusEntriesChangedEvent);
            Repository.CheckLocksChangedEvent(lastLocksChangedEvent);
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
                EditorGUI.BeginDisabledGroup(gitStatusEntries == null || !gitStatusEntries.Any());
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

                GUILayout.Label(changedFilesText, EditorStyles.miniLabel);
            }
            GUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
            {
                treeScroll = GUILayout.BeginScrollView(treeScroll);
                {
                    OnTreeGUI(new Rect(0f, 0f, Position.width, Position.height - rect.height + Styles.CommitAreaPadding));
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // Do the commit details area
            OnCommitDetailsAreaGUI();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            if (treeChanges.OnSelectionChange())
            {
                Redraw();
            }
        }

        public override void OnFocusChanged()
        {
            Logger.Debug("OnFocusChanged: {0}", HasFocus);

            base.OnFocusChanged();
            var hasFocus = HasFocus;
            if (treeChanges.ViewHasFocus != hasFocus)
            {
                treeChanges.ViewHasFocus = hasFocus;
                Redraw();
            }
        }

        private void OnTreeGUI(Rect rect)
        {
            if (treeChanges != null)
            {
                treeChanges.FolderStyle = Styles.Foldout;
                treeChanges.TreeNodeStyle = Styles.TreeNode;
                treeChanges.ActiveTreeNodeStyle = Styles.ActiveTreeNode;
                treeChanges.FocusedTreeNodeStyle = Styles.FocusedTreeNode;
                treeChanges.FocusedActiveTreeNodeStyle = Styles.FocusedActiveTreeNode;

                var treeRenderRect = treeChanges.Render(rect, treeScroll, 
                    node => { }, 
                    node => { },
                    node => {
                        var menu = CreateContextMenu(node);
                        menu.ShowAsContext();
                    });

                if (treeChanges.RequiresRepaint)
                    Redraw();

                GUILayout.Space(treeRenderRect.y - rect.y);
            }
        }

        private GenericMenu CreateContextMenu(ChangesTreeNode node)
        {
            var genericMenu = new GenericMenu();
            var canDiscard = false;

            if (!node.isFolder)
            {
                canDiscard = node.GitFileStatus == GitFileStatus.Added
                    || node.GitFileStatus == GitFileStatus.Modified
                    || node.GitFileStatus == GitFileStatus.Deleted
                    || node.GitFileStatus == GitFileStatus.Renamed
                    || node.GitFileStatus == GitFileStatus.Untracked;
            }

            if (canDiscard)
            {
                if (revertGuiContent == null)
                {
                    revertGuiContent = new GUIContent("Discard");
                }

                genericMenu.AddItem(revertGuiContent, false, () => {
                    Repository.DiscardChanges(new List<GitStatusEntry> { node.GitStatusEntry })
                        .Start();
                });
            }

            return genericMenu;
        }

        private void RepositoryOnStatusEntriesChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastStatusEntriesChangedEvent.Equals(cacheUpdateEvent))
            {
                lastStatusEntriesChangedEvent = cacheUpdateEvent;
                currentStatusEntriesHasUpdate = true;
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

        private void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                currentLocksHasUpdate = true;
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
            repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
            repository.LocksChanged += RepositoryOnLocksChanged;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentBranchChanged -= RepositoryOnCurrentBranchChanged;
            repository.StatusEntriesChanged -= RepositoryOnStatusEntriesChanged;
            repository.LocksChanged -= RepositoryOnLocksChanged;
        }

        private void MaybeUpdateData()
        {
            if (currentBranchHasUpdate)
            {
                currentBranchHasUpdate = false;
                currentBranch = string.Format("[{0}]", Repository.CurrentBranchName);
            }

            if (currentStatusEntriesHasUpdate || currentLocksHasUpdate)
            {
                currentStatusEntriesHasUpdate = false;
                currentLocksHasUpdate = false;

                gitLocks = new HashSet<string>(Repository.CurrentLocks.Select(gitLock => gitLock.Path));
                gitStatusEntries = Repository.CurrentChanges.Where(x => x.Status != GitFileStatus.Ignored).ToList();

                changedFilesText = gitStatusEntries.Count == 0
                    ? NoChangedFilesLabel
                    : gitStatusEntries.Count == 1
                        ? OneChangedFileLabel
                        : String.Format(ChangedFilesLabel, gitStatusEntries.Count);

                BuildTree();
            }
        }

        private void BuildTree()
        {
            treeChanges.PathSeparator = Environment.FileSystem.DirectorySeparatorChar.ToString();
            treeChanges.Load(gitStatusEntries.Select(entry => new GitStatusEntryTreeData(entry, gitLocks.Contains(entry.Path))));
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
                    EditorGUI.BeginDisabledGroup(IsBusy || string.IsNullOrEmpty(commitMessage) || !treeChanges.GetCheckedFiles().Any());
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
            this.treeChanges.SetCheckStateOnAll(true);
        }

        private void SelectNone()
        {
            this.treeChanges.SetCheckStateOnAll(false);
        }

        private void Commit()
        {
            // Do not allow new commits before we have received one successful update
            SetBusy(true);

            var files = treeChanges.GetCheckedFiles().ToList();
            ITask addTask;

            if (files.Count == gitStatusEntries.Count)
            {
                addTask = Repository.CommitAllFiles(commitMessage, commitBody);
            }
            else
            {
                addTask = Repository.CommitFiles(files, commitMessage, commitBody);
            }

            addTask
                .FinallyInUI((b, exception) => 
                    {
                        commitMessage = "";
                        commitBody = "";
                        SetBusy(false);
                    }).Start();
        }

        private void SetBusy(bool value)
        {
            treeChanges.IsBusy = value;
            isBusy = value;
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
