using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VersionControl.Git;
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

        [SerializeField] private bool currentBranchHasUpdate;
        [SerializeField] private bool currentStatusEntriesHasUpdate;
        [SerializeField] private bool currentLocksHasUpdate;

        [NonSerialized] private GUIContent discardGuiContent;
        [NonSerialized] private bool isBusy;

        [SerializeField] private string commitBody = "";
        [SerializeField] private string commitMessage = "";
        [SerializeField] private string currentBranch = "[unknown]";

        [SerializeField] private Vector2 treeScroll;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { DisplayRootNode = false, IsCheckable = true, IsUsingGlobalSelection = true };

        [SerializeField] private HashSet<NPath> gitLocks = new HashSet<NPath>();
        [SerializeField] private List<GitStatusEntry> gitStatusEntries = new List<GitStatusEntry>();

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
            ValidateCachedData(Repository);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void Refresh()
        {
            base.Refresh();
            Refresh(CacheType.GitStatus);
            Refresh(CacheType.RepositoryInfo);
            Refresh(CacheType.GitLocks);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnGUI()
        {
            DoButtonBarGUI();
            if (gitStatusEntries.Count == 0)
            {
                GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
                DoEmptyGUI();
                GUILayout.EndVertical();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(isBusy);
                DoChangesTreeGUI();
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.BeginDisabledGroup(isBusy);

            DoProgressGUI();

            // Do the commit details area
            DoCommitGUI();
            EditorGUI.EndDisabledGroup();
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
            base.OnFocusChanged();
            var hasFocus = HasFocus;
            if (treeChanges.ViewHasFocus != hasFocus)
            {
                treeChanges.ViewHasFocus = hasFocus;
                Redraw();
            }
        }

        private void DoChangesTreeGUI()
        {
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
        }

        private void DoButtonBarGUI()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(gitStatusEntries == null || gitStatusEntries.Count == 0);
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

            genericMenu.AddItem(new GUIContent("Show Diff"), false, () =>
            {
                ITask<NPath[]> calculateDiff = null;
                if (node.IsFolder)
                {
                    calculateDiff = CalculateFolderDiff(node);
                }
                else
                {
                    calculateDiff = CalculateFileDiff(node);
                }
                calculateDiff.FinallyInUI((s, ex, leftRight) =>
                {
                    if (s)
                        EditorUtility.InvokeDiffTool(
                            leftRight[0].IsInitialized ? leftRight[0].FileName : null,
                            leftRight[0].IsInitialized ? leftRight[0].MakeAbsolute().ToString() : null,
                            leftRight[1].IsInitialized ? leftRight[1].FileName : null,
                            leftRight[1].IsInitialized ? leftRight[1].MakeAbsolute().ToString() : null,
                            null, null);
                    else
                        ex.Rethrow();
                })
                .Start();
            });

            genericMenu.AddSeparator("");

            if (discardGuiContent == null)
            {
                discardGuiContent = new GUIContent("Discard");
            }

            genericMenu.AddItem(discardGuiContent, false, () =>
            {
                if (!EditorUtility.DisplayDialog(Localization.DiscardConfirmTitle,
                                Localization.DiscardConfirmDescription,
                                Localization.DiscardConfirmYes,
                                Localization.Cancel))
                    return;

                GitStatusEntry[] discardEntries;
                if (node.isFolder)
                {
                    discardEntries = treeChanges
                        .GetLeafNodes(node)
                        .Select(treeNode => treeNode.GitStatusEntry)
                        .ToArray();
                }
                else
                {
                    discardEntries = new [] { node.GitStatusEntry };
                }

                Repository.DiscardChanges(discardEntries)
                          .ThenInUI(AssetDatabase.Refresh)
                          .Start();
            });

            return genericMenu;
        }

        private ITask<NPath[]> CalculateFolderDiff(ChangesTreeNode node)
        {
            var rightFile = node.Path.ToNPath();
            var tmpDir = Manager.Environment.UnityProjectPath.Combine("Temp").CreateTempDirectory();
            var changedFiles = treeChanges.GetLeafNodes(node).Select(x => x.Path.ToNPath()).ToList();
            return new FuncTask<List<NPath>, NPath[]>(TaskManager.Token, (s, files) =>
            {
                var leftFolder = tmpDir.Combine("left", rightFile.FileName);
                var rightFolder = tmpDir.Combine("right", rightFile.FileName);
                foreach (var file in files)
                {
                    var txt = new SimpleProcessTask(TaskManager.Token, "show HEAD:\"" + file.ToString(SlashMode.Forward) + "\"")
                        .Configure(Manager.ProcessManager, false)
                        .Catch(_ => true)
                        .RunSynchronously();
                    if (txt != null)
                        leftFolder.Combine(file.RelativeTo(rightFile)).WriteAllText(txt);
                    if (file.FileExists())
                        rightFolder.Combine(file.RelativeTo(rightFile)).WriteAllText(file.ReadAllText());
                }
                return new NPath[] { leftFolder, rightFolder };
            }, () => changedFiles) { Message = "Calculating diff..." };
        }

        private ITask<NPath[]> CalculateFileDiff(ChangesTreeNode node)
        {
            var rightFile = node.Path.ToNPath();
            var tmpDir = Manager.Environment.UnityProjectPath.Combine("Temp", "ghu-diffs").EnsureDirectoryExists();
            var leftFile = tmpDir.Combine(rightFile.FileName + "_" + Repository.CurrentHead + rightFile.ExtensionWithDot);
            return new SimpleProcessTask(TaskManager.Token, "show HEAD:\"" + rightFile.ToString(SlashMode.Forward) + "\"")
                   .Configure(Manager.ProcessManager, false)
                   .Catch(_ => true)
                   .Then((success, txt) =>
                   {
                       // both files exist, just compare them
                       if (success && rightFile.FileExists())
                       {
                           leftFile.WriteAllText(txt);
                           return new NPath[] { leftFile, rightFile };
                       }

                       var leftFolder = tmpDir.Combine("left", leftFile.FileName).EnsureDirectoryExists();
                       var rightFolder = tmpDir.Combine("right", leftFile.FileName).EnsureDirectoryExists();
                       // file was deleted
                       if (!rightFile.FileExists())
                       {
                           leftFolder.Combine(rightFile).WriteAllText(txt);
                       }

                       // file was created
                       if (!success)
                       {
                           rightFolder.Combine(rightFile).WriteAllText(rightFile.ReadAllText());
                       }
                       return new NPath[] { leftFolder, rightFolder };
                   });
        }

        private void RepositoryOnStatusEntriesChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastStatusEntriesChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastStatusEntriesChangedEvent = cacheUpdateEvent;
                currentStatusEntriesHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnCurrentBranchChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentBranchChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastCurrentBranchChangedEvent = cacheUpdateEvent;
                currentBranchHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
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

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.RepositoryInfo, lastCurrentBranchChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitStatus, lastStatusEntriesChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLocks, lastLocksChangedEvent);
        }

        private void MaybeUpdateData()
        {
            if (currentBranchHasUpdate)
            {
                currentBranchHasUpdate = false;
                currentBranch = string.Format("[{0}]", Repository.CurrentBranchName);
            }

            if (currentLocksHasUpdate)
            {
                gitLocks = new HashSet<NPath>(Repository.CurrentLocks.Select(gitLock => gitLock.Path));
            }

            if (currentStatusEntriesHasUpdate)
            {
                gitStatusEntries = Repository.CurrentChanges.Where(x => x.Status != GitFileStatus.Ignored).ToList();

                changedFilesText = gitStatusEntries.Count == 0
                    ? NoChangedFilesLabel
                    : gitStatusEntries.Count == 1
                        ? OneChangedFileLabel
                        : String.Format(ChangedFilesLabel, gitStatusEntries.Count);
            }

            if (currentStatusEntriesHasUpdate || currentLocksHasUpdate)
            {
                currentStatusEntriesHasUpdate = false;
                currentLocksHasUpdate = false;

                BuildTree();
            }
        }

        private void BuildTree()
        {
            treeChanges.PathSeparator = Environment.FileSystem.DirectorySeparatorChar.ToString();
            treeChanges.Load(gitStatusEntries.Select(entry => new GitStatusEntryTreeData(entry, gitLocks.Contains(entry.Path.ToNPath()))));
            Redraw();
        }

        private void DoCommitGUI()
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
                    //Debug.LogFormat("IsBusy:{0} string.IsNullOrEmpty(commitMessage): {1} treeChanges.GetCheckedFiles().Any(): {2}",
                    //    IsBusy, string.IsNullOrEmpty(commitMessage), treeChanges.GetCheckedFiles().Any());
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
            isBusy = true;
            var files = treeChanges.GetCheckedFiles().ToList();
            ITask addTask = null;

            if (files.Count == gitStatusEntries.Count)
            {
                addTask = Repository.CommitAllFiles(commitMessage, commitBody);
            }
            else
            {
                ITask commit = Repository.CommitFiles(files, commitMessage, commitBody);

                // if there are files that have been staged outside of Unity, but they aren't selected for commit, remove them
                // from the index before commiting, otherwise the commit will take them along.
                var filesStagedButNotChecked = gitStatusEntries.Where(x => x.Staged).Select(x => x.Path).Except(files).ToList();
                if (filesStagedButNotChecked.Count > 0)
                    addTask = GitClient.Remove(filesStagedButNotChecked);
                addTask = addTask == null ? commit : addTask.Then(commit);
            }

            addTask
                .FinallyInUI((success, exception) =>
                    {
                        if (success)
                        {
                            UsageTracker.IncrementChangesViewButtonCommit();

                            commitMessage = "";
                            commitBody = "";
                        }
                        isBusy = false;
                    }).Start();
        }

        public override bool IsBusy
        {
            get { return isBusy || base.IsBusy; }
        }
    }
}
