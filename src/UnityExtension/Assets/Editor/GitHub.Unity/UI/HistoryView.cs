using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class HistoryControl
    {
        private const string HistoryEntryDetailFormat = "{0}     {1}";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private List<GitLogEntry> entries = new List<GitLogEntry>();
        [SerializeField] private int statusAhead;
        [SerializeField] private int selectedIndex = -1;

        [NonSerialized] private Action<GitLogEntry> rightClickNextRender;
        [NonSerialized] private GitLogEntry rightClickNextRenderEntry;
        [NonSerialized] private int controlId;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; }
        }

        public GitLogEntry SelectedGitLogEntry
        {
            get { return SelectedIndex < 0 ? GitLogEntry.Default : entries[SelectedIndex]; }
        }

        public bool Render(Rect containingRect, Action<GitLogEntry> singleClick = null,
            Action<GitLogEntry> doubleClick = null, Action<GitLogEntry> rightClick = null)
        {
            var requiresRepaint = false;
            scroll = GUILayout.BeginScrollView(scroll);
            {
                controlId = GUIUtility.GetControlID(FocusType.Keyboard);

                if (Event.current.type != EventType.Repaint)
                {
                    if (rightClickNextRender != null)
                    {
                        rightClickNextRender.Invoke(rightClickNextRenderEntry);
                        rightClickNextRender = null;
                        rightClickNextRenderEntry = GitLogEntry.Default;
                    }
                }

                var startDisplay = scroll.y;
                var endDisplay = scroll.y + containingRect.height;

                var rect = new Rect(containingRect.x, containingRect.y, containingRect.width, 0);

                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];

                    var entryRect = new Rect(rect.x, rect.y, rect.width, Styles.HistoryEntryHeight);

                    var shouldRenderEntry = !(entryRect.y > endDisplay || entryRect.yMax < startDisplay);
                    if (shouldRenderEntry && Event.current.type == EventType.Repaint)
                    {
                        RenderEntry(entryRect, entry, index);
                    }

                    var entryRequiresRepaint =
                        HandleInput(entryRect, entry, index, singleClick, doubleClick, rightClick);
                    requiresRepaint = requiresRepaint || entryRequiresRepaint;

                    rect.y += Styles.HistoryEntryHeight;
                }

                GUILayout.Space(rect.y - containingRect.y);
            }
            GUILayout.EndScrollView();

            return requiresRepaint;
        }

        private void RenderEntry(Rect entryRect, GitLogEntry entry, int index)
        {
            var isLocalCommit = index < statusAhead;
            var isSelected = index == SelectedIndex;
            var summaryRect = new Rect(entryRect.x, entryRect.y + Styles.BaseSpacing / 2, entryRect.width, Styles.HistorySummaryHeight + Styles.BaseSpacing);
            var timestampRect = new Rect(entryRect.x, entryRect.yMax - Styles.HistoryDetailsHeight - Styles.BaseSpacing / 2, entryRect.width, Styles.HistoryDetailsHeight);

            var hasKeyboardFocus = GUIUtility.keyboardControl == controlId; 

            Styles.Label.Draw(entryRect, GUIContent.none, false, false, isSelected, hasKeyboardFocus);
            Styles.HistoryEntrySummaryStyle.Draw(summaryRect, entry.Summary, false, false, isSelected, hasKeyboardFocus);

            var historyEntryDetail = string.Format(HistoryEntryDetailFormat, entry.PrettyTimeString, entry.AuthorName);
            Styles.HistoryEntryDetailsStyle.Draw(timestampRect, historyEntryDetail, false, false, isSelected, hasKeyboardFocus);

            if (!string.IsNullOrEmpty(entry.MergeA))
            {
                const float MergeIndicatorWidth = 10.28f;
                const float MergeIndicatorHeight = 12f;
                var mergeIndicatorRect = new Rect(entryRect.x + 7, summaryRect.y, MergeIndicatorWidth, MergeIndicatorHeight);

                GUI.DrawTexture(mergeIndicatorRect, Styles.MergeIcon);

                DrawTimelineRectAroundIconRect(entryRect, mergeIndicatorRect);

                summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorWidth,
                    summaryRect.height);
            }
            else
            {
                if (isLocalCommit)
                {
                    const float LocalIndicatorSize = 6f;
                    var localIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2), summaryRect.y + 5, LocalIndicatorSize,
                        LocalIndicatorSize);

                    DrawTimelineRectAroundIconRect(entryRect, localIndicatorRect);

                    GUI.DrawTexture(localIndicatorRect, Styles.LocalCommitIcon);

                    summaryRect.Set(localIndicatorRect.xMax, summaryRect.y, summaryRect.width - LocalIndicatorSize,
                        summaryRect.height);
                }
                else
                {
                    const float NormalIndicatorWidth = 6f;
                    const float NormalIndicatorHeight = 6f;

                    var normalIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2), summaryRect.y + 5,
                        NormalIndicatorWidth, NormalIndicatorHeight);

                    DrawTimelineRectAroundIconRect(entryRect, normalIndicatorRect);

                    GUI.DrawTexture(normalIndicatorRect, Styles.DotIcon);
                }
            }
        }

        private bool HandleInput(Rect rect, GitLogEntry entry, int index, Action<GitLogEntry> singleClick = null,
            Action<GitLogEntry> doubleClick = null, Action<GitLogEntry> rightClick = null)
        {
            var requiresRepaint = false;
            var clickRect = new Rect(0f, rect.y, rect.width, rect.height);
            if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                GUIUtility.keyboardControl = controlId;

                SelectedIndex = index;
                requiresRepaint = true;
                var clickCount = Event.current.clickCount;
                var mouseButton = Event.current.button;

                if (mouseButton == 0 && clickCount == 1 && singleClick != null)
                {
                    singleClick(entry);
                }
                if (mouseButton == 0 && clickCount > 1 && doubleClick != null)
                {
                    doubleClick(entry);
                }
                if (mouseButton == 1 && clickCount == 1 && rightClick != null)
                {
                    rightClickNextRender = rightClick;
                    rightClickNextRenderEntry = entry;
                }
            }

            // Keyboard navigation if this child is the current selection
            if (GUIUtility.keyboardControl == controlId && index == SelectedIndex && Event.current.type == EventType.KeyDown)
            {
                var directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                if (directionY != 0)
                {
                    Event.current.Use();

                    if (directionY > 0)
                    {
                        requiresRepaint = SelectNext(index) != index;
                    }
                    else
                    {
                        requiresRepaint = SelectPrevious(index) != index;
                    }
                }
            }

            return requiresRepaint;
        }

        private void DrawTimelineRectAroundIconRect(Rect parentRect, Rect iconRect)
        {
            Color timelineBarColor = new Color(0.51F, 0.51F, 0.51F, 0.2F);

            // Draw them lines
            //
            // First I need to figure out how large to make the top one:
            // I'll subtract the entryRect.y from the mergeIndicatorRect.y to
            // get the difference in length. then subtract a little more for padding
            float topTimelineRectHeight = iconRect.y - parentRect.y - 2;
            // Now let's create the rect
            Rect topTimelineRect = new Rect(
                parentRect.x + Styles.BaseSpacing,
                parentRect.y,
                2,
                topTimelineRectHeight);

            // And draw it
            EditorGUI.DrawRect(topTimelineRect, timelineBarColor);

            // Let's do the same for the bottom
            float bottomTimelineRectHeight = parentRect.yMax - iconRect.yMax - 2;
            Rect bottomTimelineRect = new Rect(
                parentRect.x + Styles.BaseSpacing,
                parentRect.yMax - bottomTimelineRectHeight,
                2,
                bottomTimelineRectHeight);
            EditorGUI.DrawRect(bottomTimelineRect, timelineBarColor);
        }

        public void Load(int loadAhead, List<GitLogEntry> loadEntries)
        {
            var selectedCommitId = SelectedGitLogEntry.CommitID;
            var scrollValue = scroll.y;

            var previousCount = entries.Count;

            var scrollIndex = (int)(scrollValue / Styles.HistoryEntryHeight);

            statusAhead = loadAhead;
            entries = loadEntries;

            var selectionPresent = false;
            for (var index = 0; index < entries.Count; index++)
            {
                var gitLogEntry = entries[index];
                if (gitLogEntry.CommitID.Equals(selectedCommitId))
                {
                    selectedIndex = index;
                    selectionPresent = true;
                    break;
                }
            }

            if (!selectionPresent)
            {
                selectedIndex = -1;
            }

            if (scrollIndex > entries.Count)
            {
                ScrollTo(0);
            }
            else
            {
                var scrollOffset = scrollValue % Styles.HistoryEntryHeight;

                var scrollIndexFromBottom = previousCount - scrollIndex;
                var newScrollIndex = entries.Count - scrollIndexFromBottom;

                ScrollTo(newScrollIndex, scrollOffset);
            }
        }

        private int SelectNext(int index)
        {
            index++;

            if (index < entries.Count)
            {
                SelectedIndex = index;
            }
            else
            {
                index = -1;
            }

            return index;
        }

        private int SelectPrevious(int index)
        {
            index--;

            if (index >= 0)
            {
                SelectedIndex = index;
            }
            else
            {
                SelectedIndex = -1;
            }

            return index;
        }

        public void ScrollTo(int index, float offset = 0f)
        {
            scroll.Set(scroll.x, Styles.HistoryEntryHeight * index + offset);
        }
    }

    [Serializable]
    class HistoryView : Subview
    {
        private const string PullButton = "Pull";
        private const string PullButtonCount = "Pull (<b>{0}</b>)";
        private const string PushButton = "Push";
        private const string PushButtonCount = "Push (<b>{0}</b>)";
        private const string PullConfirmTitle = "Pull Changes?";
        private const string PullConfirmDescription = "Would you like to pull changes from remote '{0}'?";
        private const string PullConfirmYes = "Pull";
        private const string PullConfirmCancel = "Cancel";
        private const string PushConfirmTitle = "Push Changes?";
        private const string PushConfirmDescription = "Would you like to push changes to remote '{0}'?";
        private const string PushConfirmYes = "Push";
        private const string PushConfirmCancel = "Cancel";
        private const string CommitDetailsTitle = "Commit details";
        private const string ClearSelectionButton = "×";
        private const string PublishButton = "Publish";
        private const string FetchActionTitle = "Fetch Changes";
        private const string FetchButtonText = "Fetch";
        private const string FetchFailureDescription = "Could not fetch changes";

        [NonSerialized] private bool currentLogHasUpdate;
        [NonSerialized] private bool currentRemoteHasUpdate;
        [NonSerialized] private bool currentTrackingStatusHasUpdate;

        [SerializeField] private bool hasItemsToCommit;
        [SerializeField] private bool hasRemote;
        [SerializeField] private string currentRemoteName;

        [SerializeField] private HistoryControl historyControl;
        [SerializeField] private GitLogEntry selectedEntry = GitLogEntry.Default;

        [SerializeField] private Vector2 detailsScroll;

        [SerializeField] private List<GitLogEntry> logEntries = new List<GitLogEntry>();

        [SerializeField] private int statusAhead;
        [SerializeField] private int statusBehind;

        [SerializeField] private ChangesTree treeChanges = new ChangesTree { IsSelectable = false, DisplayRootNode = false };
        
        [SerializeField] private CacheUpdateEvent lastCurrentRemoteChangedEvent;
        [SerializeField] private CacheUpdateEvent lastLogChangedEvent;
        [SerializeField] private CacheUpdateEvent lastAheadBehindChangedEvent;

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

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
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

        public override void OnGUI()
        {
            // History toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();

                if (hasRemote)
                {
                    EditorGUI.BeginDisabledGroup(currentRemoteName == null);
                    {
                        // Fetch button
                        var fetchClicked = GUILayout.Button(FetchButtonText, Styles.HistoryToolbarButtonStyle);
                        if (fetchClicked)
                        {
                            Fetch();
                        }

                        // Pull button
                        var pullButtonText = statusBehind > 0 ? String.Format(PullButtonCount, statusBehind) : PullButton;
                        var pullClicked = GUILayout.Button(pullButtonText, Styles.HistoryToolbarButtonStyle);

                        if (pullClicked &&
                            EditorUtility.DisplayDialog(PullConfirmTitle,
                                String.Format(PullConfirmDescription, currentRemoteName),
                                PullConfirmYes,
                                PullConfirmCancel)
                        )
                        {
                            Pull();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    // Push button
                    EditorGUI.BeginDisabledGroup(currentRemoteName == null || statusBehind != 0);
                    {
                        var pushButtonText = statusAhead > 0 ? String.Format(PushButtonCount, statusAhead) : PushButton;
                        var pushClicked = GUILayout.Button(pushButtonText, Styles.HistoryToolbarButtonStyle);

                        if (pushClicked &&
                            EditorUtility.DisplayDialog(PushConfirmTitle,
                                String.Format(PushConfirmDescription, currentRemoteName),
                                PushConfirmYes,
                                PushConfirmCancel)
                        )
                        {
                            Push();
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    // Publishing a repo
                    var publishedClicked = GUILayout.Button(PublishButton, Styles.HistoryToolbarButtonStyle);
                    if (publishedClicked)
                    {
                        PopupWindow.OpenWindow(PopupWindow.PopupViewType.PublishView);
                    }
                }
            }
            GUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            if (historyControl != null)
            {
                var historyControlRect = new Rect(0f, 0f, Position.width, Position.height - rect.height);

                var requiresRepaint = historyControl.Render(historyControlRect,  
                    entry => {
                        selectedEntry = entry;
                        BuildTree();
                    },
                    entry => { }, entry => {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Revert"), false, RevertCommit);
                        menu.ShowAsContext();
                    });

                if (requiresRepaint)
                    Redraw();
            }

            if (!selectedEntry.Equals(GitLogEntry.Default))
            {
                // Top bar for scrolling to selection or clearing it
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button(CommitDetailsTitle, Styles.HistoryToolbarButtonStyle))
                    {
                        historyControl.ScrollTo(historyControl.SelectedIndex);
                    }
                    if (GUILayout.Button(ClearSelectionButton, Styles.HistoryToolbarButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        selectedEntry = GitLogEntry.Default;
                        historyControl.SelectedIndex = -1;
                    }
                }
                GUILayout.EndHorizontal();

                // Log entry details - including changeset tree (if any changes are found)
                detailsScroll = GUILayout.BeginScrollView(detailsScroll, GUILayout.Height(250));
                {
                    HistoryDetailsEntry(selectedEntry);

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    GUILayout.Label("Files changed", EditorStyles.boldLabel);
                    GUILayout.Space(-5);

                    rect = GUILayoutUtility.GetLastRect();
                    GUILayout.BeginHorizontal(Styles.HistoryFileTreeBoxStyle);
                    GUILayout.BeginVertical();
                    {
                        var borderLeft = Styles.Label.margin.left;
                        var treeControlRect = new Rect(rect.x + borderLeft, rect.y, Position.width - borderLeft * 2, Position.height - rect.height + Styles.CommitAreaPadding);
                        var treeRect = new Rect(0f, 0f, 0f, 0f);
                        if (treeChanges != null)
                        {
                            treeChanges.FolderStyle = Styles.Foldout;
                            treeChanges.TreeNodeStyle = Styles.TreeNode;
                            treeChanges.ActiveTreeNodeStyle = Styles.ActiveTreeNode;
                            treeChanges.FocusedTreeNodeStyle = Styles.FocusedTreeNode;
                            treeChanges.FocusedActiveTreeNodeStyle = Styles.FocusedActiveTreeNode;

                            treeRect = treeChanges.Render(treeControlRect, detailsScroll,
                                node => { },
                                node => {
                                },
                                node => {
                                });

                            if (treeChanges.RequiresRepaint)
                                Redraw();
                        }

                        GUILayout.Space(treeRect.y - treeControlRect.y);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                }
                GUILayout.EndScrollView();
            }
        }

        private void HistoryDetailsEntry(GitLogEntry entry)
        {
            GUILayout.BeginVertical(Styles.HeaderBoxStyle);
            GUILayout.Label(entry.Summary, Styles.HistoryDetailsTitleStyle);

            GUILayout.Space(-5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.PrettyTimeString, Styles.HistoryDetailsMetaInfoStyle);
            GUILayout.Label(entry.AuthorName, Styles.HistoryDetailsMetaInfoStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(3);
            GUILayout.EndVertical();
        }

        private void RevertCommit()
        {
            var dialogTitle = "Revert commit";
            var dialogBody = string.Format(@"Are you sure you want to revert the following commit:""{0}""", selectedEntry.Summary);

            if (EditorUtility.DisplayDialog(dialogTitle, dialogBody, "Revert", "Cancel"))
            {
                Repository
                    .Revert(selectedEntry.CommitID)
                    .FinallyInUI((success, e) => {
                        if (!success)
                        {
                            EditorUtility.DisplayDialog(dialogTitle,
                                "Error reverting commit: " + e.Message, Localization.Cancel);
                        }
                    })
                    .Start();
            }
        }

        private void RepositoryTrackingOnStatusChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastAheadBehindChangedEvent.Equals(cacheUpdateEvent))
            {
                lastAheadBehindChangedEvent = cacheUpdateEvent;
                currentTrackingStatusHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnLogChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLogChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLogChangedEvent = cacheUpdateEvent;
                currentLogHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnCurrentRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentRemoteChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCurrentRemoteChangedEvent = cacheUpdateEvent;
                currentRemoteHasUpdate = true;
                Redraw();
            }
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.TrackingStatusChanged += RepositoryTrackingOnStatusChanged;
            repository.LogChanged += RepositoryOnLogChanged;
            repository.CurrentRemoteChanged += RepositoryOnCurrentRemoteChanged;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.TrackingStatusChanged -= RepositoryTrackingOnStatusChanged;
            repository.LogChanged -= RepositoryOnLogChanged;
            repository.CurrentRemoteChanged -= RepositoryOnCurrentRemoteChanged;
        }

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLog, lastLogChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitAheadBehind, lastAheadBehindChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.RepositoryInfo, lastCurrentRemoteChangedEvent);
        }

        private void MaybeUpdateData()
        {
            if (Repository == null)
            {
                return;
            }

            if (currentRemoteHasUpdate)
            {
                currentRemoteHasUpdate = false;

                var currentRemote = Repository.CurrentRemote;
                hasRemote = currentRemote.HasValue;
                currentRemoteName = hasRemote ? currentRemote.Value.Name : "placeholder";
            }

            if (currentTrackingStatusHasUpdate)
            {
                currentTrackingStatusHasUpdate = false;

                statusAhead = Repository.CurrentAhead;
                statusBehind = Repository.CurrentBehind;

                var currentChanges = Repository.CurrentChanges;
                hasItemsToCommit = currentChanges != null
                    && currentChanges.Any(entry => entry.Status != GitFileStatus.Ignored && !entry.Staged);
            }

            if (currentLogHasUpdate)
            {
                currentLogHasUpdate = false;

                logEntries = Repository.CurrentLog;

                BuildHistoryControl();
            }
        }

        private void BuildHistoryControl()
        {
            if (historyControl == null)
            {
                historyControl = new HistoryControl();
            }

            historyControl.Load(statusAhead, logEntries);
            if (!selectedEntry.Equals(GitLogEntry.Default) 
                && selectedEntry.CommitID != historyControl.SelectedGitLogEntry.CommitID)
            {
                selectedEntry = GitLogEntry.Default;
            }
        }

        private void Pull()
        {
            if (hasItemsToCommit)
            {
                EditorUtility.DisplayDialog("Pull", "You need to commit your changes before pulling.", "Cancel");
            }
            else
            {
                Repository
                    .Pull()
                    // we need the error propagated from the original git command to handle things appropriately
                    .Then(success => {
                        if (!success)
                        {
                            // if Pull fails we need to parse the output of the command, figure out
                            // whether pull triggered a merge or a rebase, and abort the operation accordingly
                            // (either git rebase --abort or git merge --abort)
                        }
                    }, runOptions: TaskRunOptions.OnAlways)
                    .FinallyInUI((success, e) => {
                        if (success)
                        {
                            TaskManager.Run(UsageTracker.IncrementHistoryViewToolbarButtonPull);

                            EditorUtility.DisplayDialog(Localization.PullActionTitle,
                                String.Format(Localization.PullSuccessDescription, currentRemoteName),
                            Localization.Ok);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(Localization.PullActionTitle,
                                Localization.PullFailureDescription,
                            Localization.Ok);
                        }
                    })
                    .Start();
            }
        }

        private void Push()
        {
            Repository
                .Push()
                .FinallyInUI((success, e) => {
                    if (success)
                    {
                        TaskManager.Run(UsageTracker.IncrementHistoryViewToolbarButtonPush);

                        EditorUtility.DisplayDialog(Localization.PushActionTitle,
                            String.Format(Localization.PushSuccessDescription, currentRemoteName),
                        Localization.Ok);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(Localization.PushActionTitle,
                            Localization.PushFailureDescription,
                        Localization.Ok);
                    }
                })
                .Start();
        }

        private void Fetch()
        {
            Repository
                .Fetch()
                .FinallyInUI((success, e) => {
                    if (!success)
                    {
                        TaskManager.Run(UsageTracker.IncrementHistoryViewToolbarButtonFetch);

                        EditorUtility.DisplayDialog(FetchActionTitle, FetchFailureDescription,
                            Localization.Ok);
                    }
                })
                .Start();
        }

        private void BuildTree()
        {
            treeChanges.PathSeparator = Environment.FileSystem.DirectorySeparatorChar.ToString();
            treeChanges.Load(selectedEntry.changes.Select(entry => new GitStatusEntryTreeData(entry)));
            Redraw();
        }
    }
}
