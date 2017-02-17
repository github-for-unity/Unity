#pragma warning disable 649

using GitHub.Unity;
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
        private const string HistoryFocusAll = "(All)";
        private const string HistoryFocusSingle = "Focus: <b>{0}</b>";
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
        private const string ClearSelectionButton = "x";
        private const int HistoryExtraItemCount = 10;
        private const float MaxChangelistHeightRatio = .2f;

        [NonSerialized] private string currentRemote = "placeholder";
        [NonSerialized] private int historyStartIndex;
        [NonSerialized] private int historyStopIndex;
        [NonSerialized] private float lastWidth;
        [NonSerialized] private int listID;
        [NonSerialized] private int newSelectionIndex;
        [NonSerialized] private float scrollOffset;
        [NonSerialized] private DateTimeOffset scrollTime = DateTimeOffset.Now;
        [NonSerialized] private int selectionIndex;
        [NonSerialized] private int statusAhead;
        [NonSerialized] private int statusBehind;
        [NonSerialized] private bool updated = true;
        [NonSerialized] private bool useScrollTime;

#if ENABLE_BROADMODE
        [SerializeField] private bool broadMode;
#endif
        [SerializeField] private Vector2 detailsScroll;
        [SerializeField] private bool historyLocked = true;
        [SerializeField] private Object historyTarget;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private string selectionID;

        [SerializeField] private ChangesetTreeView changesetTree = new ChangesetTreeView();
        [SerializeField] private List<GitLogEntry> history = new List<GitLogEntry>();

        public override void Initialize(IView parent)
        {
            base.Initialize(parent);

            lastWidth = Position.width;
            selectionIndex = newSelectionIndex = -1;

            changesetTree.Initialize(this);
            changesetTree.Readonly = true;
        }

        public override void OnShow()
        {
            base.OnShow();
            StatusService.Instance.RegisterCallback(OnStatusUpdate);
            Refresh();
        }

        public override void OnHide()
        {
            base.OnHide();
            StatusService.Instance.UnregisterCallback(OnStatusUpdate);
        }


        public override void Refresh()
        {
            if (historyTarget != null)
            {
                //TODO: Create a task that can get the log of one target
                //GitLogTask.Schedule(Utility.AssetPathToRepository(AssetDatabase.GetAssetPath(historyTarget)),);
            }
            else
            {
                GitLogTask.Schedule(OnLogUpdate);
            }

            StatusService.Instance.Run();

#if ENABLE_BROADMODE
            if (broadMode)
            {
                ((Window)Parent).BranchesTab.RefreshEmbedded();
            }
#endif
        }

        public override void OnSelectionChange()
        {
            if (!historyLocked && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject)))
            {
                historyTarget = Selection.activeObject;
                Refresh();
            }
        }

#if ENABLE_BROADMODE
        public bool EvaluateBroadMode()
        {
            var past = broadMode;

            // Flip when the limits are breached
            if (Position.width > Styles.BroadModeLimit)
            {
                broadMode = true;
            }
            else if (Position.width < Styles.NarrowModeLimit)
            {
                broadMode = false;
            }

            // Show the layout notification while scaling
            var window = (Window)Parent;
            var scaled = Position.width != lastWidth;
            lastWidth = Position.width;

            if (scaled)
            {
                window.ShowNotification(new GUIContent(Styles.FolderIcon), Styles.ModeNotificationDelay);
            }

            // Return whether we flipped
            return broadMode != past;
        }
#endif

        public override void OnGUI()
        {
#if ENABLE_BROADMODE
            if (broadMode)
                OnBroadGUI();
            else
#endif
                OnEmbeddedGUI();

#if ENABLE_BROADMODE
            if (Event.current.type == EventType.Repaint && EvaluateBroadMode())
            {
                Refresh();
            }
#endif
        }

#if ENABLE_BROADMODE
        public void OnBroadGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(
                    GUILayout.MinWidth(Styles.BroadModeBranchesMinWidth),
                    GUILayout.MaxWidth(Mathf.Max(Styles.BroadModeBranchesMinWidth, Position.width * Styles.BroadModeBranchesRatio))
                );
                {
                    ((Window)Parent).BranchesTab.OnEmbeddedGUI();
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                {
                    OnEmbeddedGUI();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
#endif

        public void OnEmbeddedGUI()
        {
            // History toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // Target indicator / clear button
                EditorGUI.BeginDisabledGroup(historyTarget == null);
                {
                    if (GUILayout.Button(
                            historyTarget == null ? HistoryFocusAll : String.Format(HistoryFocusSingle, historyTarget.name),
                            Styles.HistoryToolbarButtonStyle)
                    )
                    {
                        historyTarget = null;
                        Refresh();
                    }
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();


                // Pull / Push buttons
                var pullButtonText = statusBehind > 0 ? String.Format(PullButtonCount, statusBehind) : PullButton;
                GUI.enabled = currentRemote != null;
                var pullClicked = GUILayout.Button(pullButtonText, Styles.HistoryToolbarButtonStyle);
                GUI.enabled = true;
                if (pullClicked &&
                    EditorUtility.DisplayDialog(PullConfirmTitle,
                        String.Format(PullConfirmDescription, currentRemote),
                        PullConfirmYes,
                        PullConfirmCancel)
                )
                {
                    Pull();
                }

                var pushButtonText = statusAhead > 0 ? String.Format(PushButtonCount, statusAhead) : PushButton;
                GUI.enabled = statusAhead > 0 && statusBehind == 0;
                var pushClicked = GUILayout.Button(pushButtonText, Styles.HistoryToolbarButtonStyle);
                GUI.enabled = true;
                if (pushClicked &&
                    EditorUtility.DisplayDialog(PushConfirmTitle,
                        String.Format(PushConfirmDescription, currentRemote),
                        PushConfirmYes,
                        PushConfirmCancel)
                )
                {
                    Push();
                }

                // Target lock button
                EditorGUI.BeginChangeCheck();
                {
                    historyLocked = GUILayout.Toggle(historyLocked, GUIContent.none, Styles.HistoryLockStyle);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    OnSelectionChange();
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
                    // if (GUILayout.Button(selection.ShortID, Styles.HistoryToolbarButtonStyle))
                    if (GUILayout.Button("Commit details", Styles.HistoryToolbarButtonStyle))
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
                    detailsScroll = GUILayout.BeginScrollView(detailsScroll,
                        GUILayout.Height(250));
                    {
                        HistoryEntry(selection, GetEntryState(selectionIndex), false);

                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(Styles.HistoryChangesIndentation);
                            changesetTree.OnGUI();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    HistoryDetailsEntry(selection);
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

        private void OnStatusUpdate(GitStatus update)
        {
            if (update.Entries == null)
            {
                Refresh();
                return;
            }

            // Set branch state
            // TODO: Update currentRemote

            var remote = EntryPoint.GitClient.GetActiveRemote(update.RemoteBranch);
            if (remote.HasValue)
                currentRemote = remote.Value.Name;
            else
                currentRemote = null;
            statusAhead = update.Ahead;
            statusBehind = update.Behind;
        }

        private void OnLogUpdate(IList<GitLogEntry> entries)
        {
            updated = true;

            history.Clear();
            history.AddRange(entries);

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

            Redraw();
        }

        private void ScrollTo(int index, float offset = 0f)
        {
            scroll.Set(scroll.x, EntryHeight * index + offset);
        }

        private LogEntryState GetEntryState(int index)
        {
            return historyTarget == null ? (index < statusAhead ? LogEntryState.Local : LogEntryState.Normal) : LogEntryState.Normal;
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

                    drawTimelineRectAroundIconRect(entryRect, mergeIndicatorRect);

                    summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorWidth, summaryRect.height);
                }

                if (state == LogEntryState.Local)
                {
                    const float LocalIndicatorSize = 40f;
                    var localIndicatorRect = new Rect(summaryRect.x, summaryRect.y, LocalIndicatorSize, summaryRect.height);

                    // TODO: Get an icon or something here
                    Styles.HistoryEntryDetailsStyle.Draw(localIndicatorRect, "Local:", false, false, selected, keyboardFocus);

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

                    drawTimelineRectAroundIconRect(entryRect, normalIndicatorRect);

                    GUI.DrawTexture(normalIndicatorRect, Styles.DotIcon);
                }
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
            detailsScroll = GUILayout.BeginScrollView(detailsScroll, GUILayout.Height(246));
              GUILayout.Label(entry.Summary, Styles.HistoryDetailsTitleStyle);

              GUILayout.BeginHorizontal();
                GUILayout.Label(entry.PrettyTimeString, Styles.HistoryDetailsMetaInfoStyle);
                GUILayout.Label(entry.AuthorName, Styles.HistoryDetailsMetaInfoStyle);
              GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }


        private void Pull()
        {
            var task = new GitStatusTask(EntryPoint.Environment, EntryPoint.ProcessManager, null,
                EntryPoint.GitObjectFactory, status =>
                {
                    if (status.Entries.Count > 0 )
                    {
                        EntryPoint.TaskResultDispatcher.ReportFailure(FailureSeverity.Critical, "Pull", "You need to commit your changes before pulling.");
                    }
                    else
                    {
                        GitPullTask.Schedule(EntryPoint.Environment.Repository.CurrentRemote, null, null, null);
                    }
                });
            Tasks.Add(task);
        }

        private void Push()
        {
            //GitPushTask.Schedule(EntryPoint.Environment.Repository.CurrentRemote, null, null, null);
        }


        void drawTimelineRectAroundIconRect(Rect parentRect, Rect iconRect)
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


#if ENABLE_BROADMODE
        public bool BroadMode
        {
            get { return broadMode; }
        }
#endif

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
