#pragma warning disable 649

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GitHub.Unity
{
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
        private const int HistoryExtraItemCount = 10;
        private const float MaxChangelistHeightRatio = .2f;

        [NonSerialized] private int historyStartIndex;
        [NonSerialized] private int historyStopIndex;
        [NonSerialized] private float lastWidth;
        [NonSerialized] private int listID;
        [NonSerialized] private int newSelectionIndex;
        [NonSerialized] private float scrollOffset;
        [NonSerialized] private DateTimeOffset scrollTime = DateTimeOffset.Now;
        [NonSerialized] private int selectionIndex;
        [NonSerialized] private bool updated = true;
        [NonSerialized] private bool useScrollTime;
        [NonSerialized] private bool isBusy;

        [SerializeField] private Vector2 detailsScroll;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private string selectionID;
        [SerializeField] private int statusAhead;
        [SerializeField] private int statusBehind;

        [SerializeField] private ChangesetTreeView changesetTree = new ChangesetTreeView();
        [SerializeField] private List<GitLogEntry> history = new List<GitLogEntry>();
        [SerializeField] private string currentRemote;
        [SerializeField] private bool isPublished;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);

            lastWidth = Position.width;
            selectionIndex = newSelectionIndex = -1;

            changesetTree.InitializeView(this);
            changesetTree.Readonly = true;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);
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

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);

            DetachHandlers(oldRepository);
            AttachHandlers(Repository);
            Refresh();
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshLog();
        }

        public override void OnSelectionChange()
        {

        }

        public override void OnGUI()
        {
            OnEmbeddedGUI();
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.OnHeadChanged += Refresh;
            repository.OnStatusUpdated += UpdateStatusOnMainThread;
            repository.OnActiveBranchChanged += s => Refresh();
            repository.OnActiveRemoteChanged += s => Refresh();
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.OnHeadChanged -= Refresh;
            repository.OnStatusUpdated -= UpdateStatusOnMainThread;
            repository.OnActiveBranchChanged -= s => Refresh();
            repository.OnActiveRemoteChanged -= s => Refresh();
        }

        private void UpdateStatusOnMainThread(GitStatus status)
        {
            new ActionTask(TaskManager.Token, _ => UpdateStatus(status))
                .ScheduleUI(TaskManager);
        }

        private void UpdateStatus(GitStatus status)
        {
            statusAhead = status.Ahead;
            statusBehind = status.Behind;
        }

        private void RefreshLog()
        {
            if (Repository != null)
            {
                Repository.Log().ThenInUI((success, log) => {
                    if (success) OnLogUpdate(log);
                }).Start();
            }
        }

        private void OnLogUpdate(List<GitLogEntry> entries)
        {
            Logger.Trace("OnLogUpdate");
            GitLogCache.Instance.Log = entries;
            updated = true;
            Redraw();
        }

        private void MaybeUpdateData()
        {
            isPublished = Repository != null && Repository.CurrentRemote.HasValue;
            currentRemote = isPublished ? Repository.CurrentRemote.Value.Name : "placeholder";

            if (!updated)
                return;
            updated = false;

            history = GitLogCache.Instance.Log;

            if (history.Any())
            {
                // Make sure that scroll as much as possible focuses the same time period in the new entry list
                if (useScrollTime)
                {
                    var closestIndex = -1;
                    double closestDifference = Mathf.Infinity;
                    for (var index = 0; index < history.Count; ++index)
                    {
                        var diff = Math.Abs((history[index].Time - scrollTime).TotalSeconds);
                        if (diff < closestDifference)
                        {
                            closestDifference = diff;
                            closestIndex = index;
                        }
                    }

                    ScrollTo(closestIndex, scrollOffset);
                }

                CullHistory();
            }

            // Restore selection index or clear it
            newSelectionIndex = -1;
            if (!string.IsNullOrEmpty(selectionID))
            {
                selectionIndex =
                    Enumerable.Range(1, history.Count + 1).FirstOrDefault(index => history[index - 1].CommitID.Equals(selectionID)) - 1;

                if (selectionIndex < 0)
                {
                    selectionID = string.Empty;
                }
            }
        }

        public void OnEmbeddedGUI()
        {
            // History toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();

                if (isPublished)
                {
                    EditorGUI.BeginDisabledGroup(currentRemote == null);
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
                                String.Format(PullConfirmDescription, currentRemote),
                                PullConfirmYes,
                                PullConfirmCancel)
                        )
                        {
                            Pull();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    // Push button
                    EditorGUI.BeginDisabledGroup(currentRemote == null || statusBehind != 0);
                    {
                        var pushButtonText = statusAhead > 0 ? String.Format(PushButtonCount, statusAhead) : PushButton;
                        var pushClicked = GUILayout.Button(pushButtonText, Styles.HistoryToolbarButtonStyle);

                        if (pushClicked &&
                            EditorUtility.DisplayDialog(PushConfirmTitle,
                                String.Format(PushConfirmDescription, currentRemote),
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
                    EditorGUI.BeginDisabledGroup(!Platform.Keychain.Connections.Any());
                    {
                        var publishedClicked = GUILayout.Button(PublishButton, Styles.HistoryToolbarButtonStyle);
                        if (publishedClicked)
                        {
                            PopupWindow.Open(PopupWindow.PopupViewType.PublishView);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            GUILayout.EndHorizontal();

            // When history scroll actually changes, store time value of topmost visible entry. This is the value we use to reposition scroll on log update - not the pixel value.
            if (history.Any())
            {
                listID = GUIUtility.GetControlID(FocusType.Keyboard);

                // Only update time scroll
                var lastScroll = scroll;
                scroll = GUILayout.BeginScrollView(scroll);
                if (lastScroll != scroll && !updated)
                {
                    scrollTime = history[historyStartIndex].Time;
                    scrollOffset = scroll.y - historyStartIndex * EntryHeight;
                    useScrollTime = true;
                }
                // Handle only the selected range of history items - adding spacing for the rest
                var start = Mathf.Max(0, historyStartIndex - HistoryExtraItemCount);
                var stop = Mathf.Min(historyStopIndex + HistoryExtraItemCount, history.Count);
                GUILayout.Space(start * EntryHeight);
                for (var index = start; index < stop; ++index)
                {
                    if (HistoryEntry(history[index], GetEntryState(index), selectionIndex == index))
                    {
                        newSelectionIndex = index;
                        GUIUtility.keyboardControl = listID;
                    }
                }

                GUILayout.Space((history.Count - stop) * EntryHeight);

                // Keyboard control
                if (GUIUtility.keyboardControl == listID && Event.current.type == EventType.KeyDown)
                {
                    var change = 0;

                    if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        change = 1;
                    }
                    else if (Event.current.keyCode == KeyCode.UpArrow)
                    {
                        change = -1;
                    }

                    if (change != 0)
                    {
                        newSelectionIndex = (selectionIndex + change) % history.Count;
                        if (newSelectionIndex < historyStartIndex || newSelectionIndex > historyStopIndex)
                        {
                            ScrollTo(newSelectionIndex,
                                (Position.height - Position.height * MaxChangelistHeightRatio - 30f - EntryHeight) * -.5f);
                        }
                        Event.current.Use();
                    }
                }
            }
            else
            {
                GUILayout.BeginScrollView(scroll);
            }

            GUILayout.EndScrollView();

            // Selection info
            if (selectionIndex >= 0 && history.Count > selectionIndex)
            {
                var selection = history[selectionIndex];

                // Top bar for scrolling to selection or clearing it
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button(CommitDetailsTitle, Styles.HistoryToolbarButtonStyle))
                    {
                        ScrollTo(selectionIndex);
                    }
                    if (GUILayout.Button(ClearSelectionButton, Styles.HistoryToolbarButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        newSelectionIndex = -2;
                    }
                }
                GUILayout.EndHorizontal();

                // Log entry details - including changeset tree (if any changes are found)
                if (changesetTree.Entries.Any())
                {
                    detailsScroll = GUILayout.BeginScrollView(detailsScroll, GUILayout.Height(250));
                    {
                        HistoryDetailsEntry(selection);

                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                        GUILayout.Label("Files changed", EditorStyles.boldLabel);
                        GUILayout.Space(-5);

                        GUILayout.BeginHorizontal(Styles.HistoryFileTreeBoxStyle);
                        {
                            changesetTree.OnGUI();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    detailsScroll = GUILayout.BeginScrollView(detailsScroll, GUILayout.Height(246));
                    HistoryDetailsEntry(selection);
                    GUILayout.EndScrollView();
                }
            }

            // Handle culling and selection changes at the end of the last GUI frame
            if (Event.current.type == EventType.Repaint)
            {
                CullHistory();
                updated = false;

                if (newSelectionIndex >= 0 || newSelectionIndex == -2)
                {
                    selectionIndex = newSelectionIndex == -2 ? -1 : newSelectionIndex;
                    newSelectionIndex = -1;
                    detailsScroll = Vector2.zero;

                    if (selectionIndex >= 0)
                    {
                        changesetTree.UpdateEntries(history[selectionIndex].Changes);
                    }

                    Redraw();
                }
            }
        }

        private void ScrollTo(int index, float offset = 0f)
        {
            scroll.Set(scroll.x, EntryHeight * index + offset);
        }

        private LogEntryState GetEntryState(int index)
        {
            return index < statusAhead ? LogEntryState.Local : LogEntryState.Normal;
        }

        /// <summary>
        /// Recalculate the range of history items to handle - based on what is visible, plus a bit of padding for fast scrolling
        /// </summary>
        private void CullHistory()
        {
            historyStartIndex = (int)Mathf.Clamp(scroll.y / EntryHeight, 0, history.Count);
            historyStopIndex =
                (int)
                    Mathf.Clamp(
                        historyStartIndex +
                            (Position.height - 2f * Mathf.Min(changesetTree.Height, Position.height * MaxChangelistHeightRatio)) /
                                EntryHeight + 1, 0, history.Count);
        }

        private void RevertCommit()
        {
            var selection = history[selectionIndex];

            var dialogTitle = "Revert commit";
            var dialogBody = string.Format(@"Are you sure you want to revert the following commit:""{0}""", selection.Summary);

            if (EditorUtility.DisplayDialog(dialogTitle, dialogBody, "Revert", "Cancel"))
            {
                Repository
                    .Revert(selection.CommitID)
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

        private bool HistoryEntry(GitLogEntry entry, LogEntryState state, bool selected)
        {
            var entryRect = GUILayoutUtility.GetRect(Styles.HistoryEntryHeight, Styles.HistoryEntryHeight);
            var timelineBarRect = new Rect(entryRect.x + Styles.BaseSpacing, 0, 2, Styles.HistoryDetailsHeight);

            if (Event.current.type == EventType.Repaint)
            {
                var keyboardFocus = GUIUtility.keyboardControl == listID;

                var summaryRect = new Rect(entryRect.x, entryRect.y + (Styles.BaseSpacing / 2), entryRect.width, Styles.HistorySummaryHeight + Styles.BaseSpacing);
                var timestampRect = new Rect(entryRect.x, entryRect.yMax - Styles.HistoryDetailsHeight - (Styles.BaseSpacing / 2), entryRect.width, Styles.HistoryDetailsHeight);
                var authorRect = new Rect(timestampRect.xMax, timestampRect.y, timestampRect.width, timestampRect.height);

                var contentOffset = new Vector2(Styles.BaseSpacing * 2, 0);

                Styles.Label.Draw(entryRect, "", false, false, selected, keyboardFocus);

                Styles.Label.contentOffset = contentOffset;
                Styles.HistoryEntryDetailsStyle.contentOffset = contentOffset;

                Styles.Label.Draw(summaryRect, entry.Summary, false, false, selected, keyboardFocus);
                Styles.HistoryEntryDetailsStyle.Draw(timestampRect, entry.PrettyTimeString + "     " + entry.AuthorName, false, false, selected, keyboardFocus);

                if (!string.IsNullOrEmpty(entry.MergeA))
                {
                    const float MergeIndicatorWidth = 10.28f;
                    const float MergeIndicatorHeight = 12f;
                    var mergeIndicatorRect = new Rect(entryRect.x + 7, summaryRect.y, MergeIndicatorWidth, MergeIndicatorHeight);

                    GUI.DrawTexture(mergeIndicatorRect, Styles.MergeIcon);

                    DrawTimelineRectAroundIconRect(entryRect, mergeIndicatorRect);

                    summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorWidth, summaryRect.height);
                }

                if (state == LogEntryState.Local && string.IsNullOrEmpty(entry.MergeA))
                {
                    const float LocalIndicatorSize = 6f;
                    var localIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2), summaryRect.y + 5, LocalIndicatorSize, LocalIndicatorSize);

                    DrawTimelineRectAroundIconRect(entryRect, localIndicatorRect);

                    GUI.DrawTexture(localIndicatorRect, Styles.LocalCommitIcon);

                    summaryRect.Set(localIndicatorRect.xMax, summaryRect.y, summaryRect.width - LocalIndicatorSize, summaryRect.height);
                }

                if (state == LogEntryState.Normal && string.IsNullOrEmpty(entry.MergeA))
                {
                    const float NormalIndicatorWidth = 6f;
                    const float NormalIndicatorHeight = 6f;

                    Rect normalIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2),
                        summaryRect.y + 5,
                        NormalIndicatorWidth,
                        NormalIndicatorHeight);

                    DrawTimelineRectAroundIconRect(entryRect, normalIndicatorRect);

                    GUI.DrawTexture(normalIndicatorRect, Styles.DotIcon);
                }
            }
            else if (Event.current.type == EventType.ContextClick && entryRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Revert"), false, RevertCommit);
                menu.ShowAsContext();

                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        private void HistoryDetailsEntry(GitLogEntry entry)
        {
            GUILayout.BeginVertical(Styles.HeaderBoxStyle);
            GUILayout.Label(entry.Summary, Styles.HistoryDetailsTitleStyle, GUILayout.Width(Position.width));

            GUILayout.Space(-5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.PrettyTimeString, Styles.HistoryDetailsMetaInfoStyle);
            GUILayout.Label(entry.AuthorName, Styles.HistoryDetailsMetaInfoStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(3);
            GUILayout.EndVertical();
        }

        private void Pull()
        {
            var status = Repository.CurrentStatus;
            if (status.Entries != null && status.GetEntriesExcludingIgnoredAndUntracked().Any())
            {
                EditorUtility.DisplayDialog("Pull", "You need to commit your changes before pulling.", "Cancel");
            }
            else
            {
                var remote = Repository.CurrentRemote.HasValue ? Repository.CurrentRemote.Value.Name : String.Empty;
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
                    }, true)
                    .FinallyInUI((success, e) => {
                        if (success)
                        {
                            EditorUtility.DisplayDialog(Localization.PullActionTitle,
                                String.Format(Localization.PullSuccessDescription, remote),
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
            var remote = Repository.CurrentRemote.HasValue ? Repository.CurrentRemote.Value.Name : String.Empty;
            Repository
                .Push()
                .FinallyInUI((success, e) => {
                    if (success)
                    {
                        EditorUtility.DisplayDialog(Localization.PushActionTitle,
                            String.Format(Localization.PushSuccessDescription, remote),
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
            var remote = Repository.CurrentRemote.HasValue ? Repository.CurrentRemote.Value.Name : String.Empty;
            Repository
                .Fetch()
                .FinallyInUI((success, e) => {
                    if (!success)
                    {
                        EditorUtility.DisplayDialog(FetchActionTitle, FetchFailureDescription,
                            Localization.Ok);
                    }
                })
                .Start();
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

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        private float EntryHeight
        {
            get { return Styles.HistoryEntryHeight + Styles.HistoryEntryPadding; }
        }

        private enum LogEntryState
        {
            Normal,
            Local,
            Remote
        }
    }
}
