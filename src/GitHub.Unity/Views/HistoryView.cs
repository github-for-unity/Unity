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

        [SerializeField] private bool broadMode;
        [SerializeField] private ChangesetTreeView changesetTree = new ChangesetTreeView();
        [SerializeField] private Vector2 detailsScroll;
        [SerializeField] private List<GitLogEntry> history = new List<GitLogEntry>();
        [SerializeField] private bool historyLocked = true;
        [SerializeField] private Object historyTarget;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private string selectionID;

        public override void Refresh()
        {
            if (historyTarget != null)
            {
                GitLogTask.Schedule(Utility.AssetPathToRepository(AssetDatabase.GetAssetPath(historyTarget)));
            }
            else
            {
                GitLogTask.Schedule();
            }

            GitStatusTask.Schedule();

            if (broadMode)
            {
                ((Window)parent).BranchesTab.RefreshEmbedded();
            }
        }

        public override void OnSelectionChange()
        {
            if (!historyLocked && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject)))
            {
                historyTarget = Selection.activeObject;
                Refresh();
            }
        }

        public bool EvaluateBroadMode()
        {
            var past = broadMode;

            // Flip when the limits are breached
            if (position.width > Styles.BroadModeLimit)
            {
                broadMode = true;
            }
            else if (position.width < Styles.NarrowModeLimit)
            {
                broadMode = false;
            }

            // Show the layout notification while scaling
            var window = (Window)parent;
            var scaled = position.width != lastWidth;
            lastWidth = position.width;

            if (scaled)
            {
                window.ShowNotification(new GUIContent(Styles.FolderIcon), Styles.ModeNotificationDelay);
            }

            // Return whether we flipped
            return broadMode != past;
        }

        public override void OnGUI()
        {
            if (broadMode)
            {
                OnBroadGUI();
            }
            else
            {
                OnEmbeddedGUI();
            }

            if (Event.current.type == EventType.Repaint && EvaluateBroadMode())
            {
                Refresh();
            }
        }

        public void OnBroadGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(
                    GUILayout.MinWidth(Styles.BroadModeBranchesMinWidth),
                    GUILayout.MaxWidth(Mathf.Max(Styles.BroadModeBranchesMinWidth, position.width * Styles.BroadModeBranchesRatio))
                );
                {
                    ((Window)parent).BranchesTab.OnEmbeddedGUI();
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

                    GUILayout.Space(Styles.HistoryEntryPadding);
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
                                (position.height - position.height * MaxChangelistHeightRatio - 30f - EntryHeight) * -.5f);
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
            if (selectionIndex >= 0)
            {
                var selection = history[selectionIndex];

                // Top bar for scrolling to selection or clearing it
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button(selection.ShortID, Styles.HistoryToolbarButtonStyle))
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
                        GUILayout.MinHeight(Mathf.Min(changesetTree.Height, position.height * MaxChangelistHeightRatio)));
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
                    HistoryEntry(selection, GetEntryState(selectionIndex), false);
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
                        changesetTree.Update(history[selectionIndex].Changes);
                    }

                    Repaint();
                }
            }
        }

        protected override void OnShow()
        {
            lastWidth = position.width;
            selectionIndex = newSelectionIndex = -1;

            GitLogTask.RegisterCallback(OnLogUpdate);
            GitStatusTask.RegisterCallback(OnStatusUpdate);

            changesetTree.Show(this);
            changesetTree.Readonly = true;
        }

        protected override void OnHide()
        {
            GitStatusTask.UnregisterCallback(OnStatusUpdate);
            GitLogTask.UnregisterCallback(OnLogUpdate);
        }

        private void OnStatusUpdate(GitStatus update)
        {
            // Set branch state
            // TODO: Update currentRemote
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

            Repaint();
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
                            (position.height - 2f * Mathf.Min(changesetTree.Height, position.height * MaxChangelistHeightRatio)) /
                                EntryHeight + 1, 0, history.Count);
        }

        private bool HistoryEntry(GitLogEntry entry, LogEntryState state, bool selected)
        {
            var entryRect = GUILayoutUtility.GetRect(Styles.HistoryEntryHeight, Styles.HistoryEntryHeight);

            if (Event.current.type == EventType.Repaint)
            {
                var keyboardFocus = GUIUtility.keyboardControl == listID;

                var summaryRect = new Rect(entryRect.x, entryRect.y, entryRect.width, Styles.HistorySummaryHeight);
                var timestampRect = new Rect(entryRect.x, entryRect.yMax - Styles.HistoryDetailsHeight, entryRect.width * .5f,
                    Styles.HistoryDetailsHeight);
                var authorRect = new Rect(timestampRect.xMax, timestampRect.y, timestampRect.width, timestampRect.height);

                if (!string.IsNullOrEmpty(entry.MergeA))
                {
                    const float MergeIndicatorSize = 40f;
                    var mergeIndicatorRect = new Rect(summaryRect.x, summaryRect.y, MergeIndicatorSize, summaryRect.height);

                    // TODO: Get an icon or something here
                    Styles.HistoryEntryDetailsStyle.Draw(mergeIndicatorRect, "Merge:", false, false, selected, keyboardFocus);

                    summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorSize, summaryRect.height);
                }

                if (state == LogEntryState.Local)
                {
                    const float LocalIndicatorSize = 40f;
                    var localIndicatorRect = new Rect(summaryRect.x, summaryRect.y, LocalIndicatorSize, summaryRect.height);

                    // TODO: Get an icon or something here
                    Styles.HistoryEntryDetailsStyle.Draw(localIndicatorRect, "Local:", false, false, selected, keyboardFocus);

                    summaryRect.Set(localIndicatorRect.xMax, summaryRect.y, summaryRect.width - LocalIndicatorSize, summaryRect.height);
                }

                Styles.Label.Draw(summaryRect, entry.Summary, false, false, selected, keyboardFocus);
                Styles.HistoryEntryDetailsStyle.Draw(timestampRect, entry.PrettyTimeString, false, false, selected, keyboardFocus);
                Styles.HistoryEntryDetailsStyle.Draw(authorRect, entry.AuthorName, false, false, selected, keyboardFocus);
            }
            else if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        private void Pull()
        {
            Debug.Log("TODO: Pull");
        }

        private void Push()
        {
            Debug.Log("TODO: Push");
        }

        public bool BroadMode
        {
            get { return broadMode; }
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
