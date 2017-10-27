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

        [NonSerialized] private bool isBusy = true;

        [SerializeField] private string commitBody = "";
        [SerializeField] private string commitMessage = "";
        [SerializeField] private string currentBranch = "[unknown]";
        [SerializeField] private Vector2 horizontalScroll;
        [SerializeField] private ChangesetTreeView tree = new ChangesetTreeView();

        [SerializeField] private UpdateDataEventData repositoryInfoCacheEvent;
        [NonSerialized] private bool repositoryInfoCacheHasChanged;

        [SerializeField] private UpdateDataEventData gitStatusCacheEvent;
        [NonSerialized] private bool gitStatusCacheHasChanged;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            tree.InitializeView(this);
        }

        private void Repository_GitStatusCacheChanged(UpdateDataEventData data)
        {
            new ActionTask(TaskManager.Token, () => {
                    gitStatusCacheEvent = data;
                    gitStatusCacheHasChanged = true;
                    Redraw();
                })
                { Affinity = TaskAffinity.UI }.Start();
        }

        private void Repository_RepositoryInfoCacheChanged(UpdateDataEventData data)
        {
            new ActionTask(TaskManager.Token, () => {
                    repositoryInfoCacheEvent = data;
                    repositoryInfoCacheHasChanged = true;
                    Redraw();
                })
                { Affinity = TaskAffinity.UI }.Start();
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.OnRepositoryInfoCacheChanged += Repository_RepositoryInfoCacheChanged;
            repository.OnGitStatusCacheChanged += Repository_GitStatusCacheChanged;
        }

        private void DetachHandlers(IRepository oldRepository)
        {
            if (oldRepository == null)
                return;

            oldRepository.OnRepositoryInfoCacheChanged -= Repository_RepositoryInfoCacheChanged;
            oldRepository.OnGitStatusCacheChanged -= Repository_GitStatusCacheChanged;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                Repository.CheckRepositoryInfoCacheEvent(repositoryInfoCacheEvent);
                Repository.CheckGitStatusCacheEvent(gitStatusCacheEvent);
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

        private void MaybeUpdateData()
        {
            if (repositoryInfoCacheHasChanged)
            {
                repositoryInfoCacheHasChanged = false;
                currentBranch = string.Format("[{0}]", Repository.CurrentBranchName);
            }

            if (gitStatusCacheHasChanged)
            {
                gitStatusCacheHasChanged = false;
                var gitStatus = Repository.CurrentStatus;
                tree.UpdateEntries(gitStatus.Entries.Where(x => x.Status != GitFileStatus.Ignored).ToList());
            }
        }

        public override void OnGUI()
        {
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

            horizontalScroll = GUILayout.BeginScrollView(horizontalScroll);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
            {
                tree.OnGUI();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();


            // Do the commit details area
            OnCommitDetailsAreaGUI();
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
                    EditorGUI.BeginDisabledGroup(isBusy || string.IsNullOrEmpty(commitMessage) || !tree.CommitTargets.Any(t => t.Any));
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
            isBusy = true;

            var files = Enumerable.Range(0, tree.Entries.Count)
                .Where(i => tree.CommitTargets[i].All)
                .Select(i => tree.Entries[i].Path)
                .ToList();

            ITask addTask;

            if (files.Count == tree.Entries.Count)
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
                        isBusy = false;
                    }).Start();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
