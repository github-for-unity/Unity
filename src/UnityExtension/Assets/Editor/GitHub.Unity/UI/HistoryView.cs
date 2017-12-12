using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    struct HistoryControlRenderResult
    {
        public Rect Rect;
        public bool RequiresRepaint;
    }

    [Serializable]
    class HistoryControl
    {
        private const string HistoryEntryDetailFormat = "{0}     {1}";

        [SerializeField] private List<GitLogEntry> entries = new List<GitLogEntry>();
        [SerializeField] private int statusAhead;
        [SerializeField] private int selectedIndex;

        [NonSerialized] private Action<GitLogEntry> rightClickNextRender;
        [NonSerialized] private GitLogEntry rightClickNextRenderEntry;
        [NonSerialized] private int controlId;

        public HistoryControlRenderResult Render(Rect containingRect, Rect rect, Vector2 scroll, Action<GitLogEntry> singleClick = null,
            Action<GitLogEntry> doubleClick = null, Action<GitLogEntry> rightClick = null)
        {
            var requiresRepaint = false;

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

            rect = new Rect(rect.x, rect.y, rect.width, 0);

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];

                var entryRect = new Rect(rect.x, rect.y, rect.width, Styles.HistoryEntryHeight);

                var shouldRenderEntry = !(entryRect.y > endDisplay || entryRect.yMax < startDisplay);
                if (shouldRenderEntry && Event.current.type == EventType.Repaint)
                {
                    RenderEntry(entryRect, entry, index);
                }

                var entryRequiresRepaint = HandleInput(entryRect, entry, index, singleClick, doubleClick, rightClick);
                requiresRepaint = requiresRepaint || entryRequiresRepaint;

                rect.y += Styles.HistoryEntryHeight;
            }

            return new HistoryControlRenderResult {
                Rect = rect,
                RequiresRepaint = requiresRepaint
            };
        }

        private void RenderEntry(Rect entryRect, GitLogEntry entry, int index)
        {
            var isLocalCommit = index < statusAhead;
            var isSelected = index == selectedIndex;
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

                selectedIndex = index;
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
            if (GUIUtility.keyboardControl == controlId && index == selectedIndex && Event.current.type == EventType.KeyDown)
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
            statusAhead = loadAhead;
            entries = loadEntries;
        }

        private int SelectNext(int index)
        {
            index++;

            if (index < entries.Count)
            {
                selectedIndex = index;
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
                selectedIndex = index;
            }
            else
            {
                selectedIndex = -1;
            }

            return index;
        }
    }

    enum LogEntryState
    {
        Normal,
        Local
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

        [SerializeField] private Vector2 historyScroll;
        [SerializeField] private HistoryControl historyControl;

        [SerializeField] private List<GitLogEntry> logEntries = new List<GitLogEntry>();

        [SerializeField] private int statusAhead;
        [SerializeField] private int statusBehind;
        
        [SerializeField] private CacheUpdateEvent lastCurrentRemoteChangedEvent;
        [SerializeField] private CacheUpdateEvent lastLogChangedEvent;
        [SerializeField] private CacheUpdateEvent lastTrackingStatusChangedEvent;

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                Repository.CheckLogChangedEvent(lastLogChangedEvent);
                Repository.CheckStatusChangedEvent(lastTrackingStatusChangedEvent);
                Repository.CheckCurrentRemoteChangedEvent(lastCurrentRemoteChangedEvent);
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
            OnEmbeddedGUI();
        }

        public void OnEmbeddedGUI()
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
            historyScroll = GUILayout.BeginScrollView(historyScroll);
            {
                OnHistoryGUI(new Rect(0f, 0f, Position.width, Position.height - rect.height));
            }
            GUILayout.EndScrollView();
        }

        private void OnHistoryGUI(Rect rect)
        {
            var initialRect = rect;
            if (historyControl != null)
            {
                var renderResult = historyControl.Render(initialRect, rect, historyScroll,
                    entry => { },
                    entry => { },
                    entry => { });

                rect = renderResult.Rect;

                if (renderResult.RequiresRepaint)
                    Redraw();
            }

            GUILayout.Space(rect.y - initialRect.y);
        }

        private void RepositoryTrackingOnStatusChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastTrackingStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                lastTrackingStatusChangedEvent = cacheUpdateEvent;
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
                    }, true)
                    .FinallyInUI((success, e) => {
                        if (success)
                        {
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
                        EditorUtility.DisplayDialog(FetchActionTitle, FetchFailureDescription,
                            Localization.Ok);
                    }
                })
                .Start();
        }

        public override bool IsBusy
        {
            get { return false; }
        }
    }
}
