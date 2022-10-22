﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VersionControl.Git;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    public class HistoryControl
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

    abstract class HistoryBase : Subview
    {
        protected const string CommitDetailsTitle = "Commit details";
        protected const string ClearSelectionButton = "×";

        protected abstract HistoryControl HistoryControl { get; set; }
        protected abstract GitLogEntry SelectedEntry { get; set; }
        protected abstract ChangesTree TreeChanges { get; set; }
        protected abstract Vector2 DetailsScroll { get; set; }

        protected void BuildHistoryControl(int loadAhead, List<GitLogEntry> gitLogEntries)
        {
            if (HistoryControl == null)
            {
                HistoryControl = new HistoryControl();
            }

            HistoryControl.Load(loadAhead, gitLogEntries);
            if (!SelectedEntry.Equals(GitLogEntry.Default)
                && SelectedEntry.CommitID != HistoryControl.SelectedGitLogEntry.CommitID)
            {
                SelectedEntry = GitLogEntry.Default;
            }
        }

        protected void BuildTreeChanges()
        {
            TreeChanges.PathSeparator = Environment.FileSystem.DirectorySeparatorChar.ToString();
            TreeChanges.Load(SelectedEntry.changes.Select(entry => new GitStatusEntryTreeData(entry)));
            Redraw();
        }

        protected void RevertCommit()
        {
            var dialogTitle = "Revert commit";
            var dialogBody = string.Format(@"Are you sure you want to revert the following commit:""{0}""", SelectedEntry.Summary);

            if (EditorUtility.DisplayDialog(dialogTitle, dialogBody, "Revert", "Cancel"))
            {
                Repository
                    .Revert(SelectedEntry.CommitID)
                    .FinallyInUI((success, e) => {
                        if (!success)
                        {
                            EditorUtility.DisplayDialog(dialogTitle,
                                "Error reverting commit: " + e.Message, Localization.Cancel);
                        }
                        AssetDatabase.Refresh();
                    })
                    .Start();
            }
        }

        protected void HistoryDetailsEntry(GitLogEntry entry)
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

        protected void DoHistoryGui(Rect rect, Action<GitLogEntry> historyControlRightClick = null,
            Action<ChangesTreeNode> changesTreeRightClick = null)
        {
            if (HistoryControl != null)
            {
                var historyControlRect = new Rect(0f, 0f, Position.width, Position.height - rect.height);

                var requiresRepaint = HistoryControl.Render(historyControlRect,
                    singleClick: entry => {
                        SelectedEntry = entry;
                        BuildTreeChanges();
                    },
                    doubleClick: entry => {

                    },
                    rightClick: historyControlRightClick);

                if (requiresRepaint)
                    Redraw();
            }

            DoProgressGUI();

            if (!SelectedEntry.Equals(GitLogEntry.Default))
            {
                // Top bar for scrolling to selection or clearing it
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button(CommitDetailsTitle, Styles.ToolbarButtonStyle))
                    {
                        HistoryControl.ScrollTo(HistoryControl.SelectedIndex);
                    }

                    if (GUILayout.Button(ClearSelectionButton, Styles.ToolbarButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        SelectedEntry = GitLogEntry.Default;
                        HistoryControl.SelectedIndex = -1;
                    }
                }
                GUILayout.EndHorizontal();

                // Log entry details - including changeset tree (if any changes are found)
                DetailsScroll = GUILayout.BeginScrollView(DetailsScroll, GUILayout.Height(250));
                {
                    HistoryDetailsEntry(SelectedEntry);

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    GUILayout.Label("Files changed", EditorStyles.boldLabel);
                    GUILayout.Space(-5);

                    rect = GUILayoutUtility.GetLastRect();
                    GUILayout.BeginHorizontal(Styles.HistoryFileTreeBoxStyle);
                    GUILayout.BeginVertical();
                    {
                        var borderLeft = Styles.Label.margin.left;
                        var treeControlRect = new Rect(rect.x + borderLeft, rect.y, Position.width - borderLeft * 2,
                            Position.height - rect.height + Styles.CommitAreaPadding);
                        var treeRect = new Rect(0f, 0f, 0f, 0f);
                        if (TreeChanges != null)
                        {
                            TreeChanges.FolderStyle = Styles.Foldout;
                            TreeChanges.TreeNodeStyle = Styles.TreeNode;
                            TreeChanges.ActiveTreeNodeStyle = Styles.ActiveTreeNode;
                            TreeChanges.FocusedTreeNodeStyle = Styles.FocusedTreeNode;
                            TreeChanges.FocusedActiveTreeNodeStyle = Styles.FocusedActiveTreeNode;

                            treeRect = TreeChanges.Render(treeControlRect, DetailsScroll,
                                singleClick: node => { },
                                doubleClick: node => { },
                                rightClick: changesTreeRightClick);

                            if (TreeChanges.RequiresRepaint)
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

        public override void OnEnable()
        {
            base.OnEnable();

            if (TreeChanges != null)
            {
                TreeChanges.ViewHasFocus = HasFocus;
                TreeChanges.UpdateIcons(Styles.FolderIcon);
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
            if (TreeChanges.ViewHasFocus != hasFocus)
            {
                TreeChanges.ViewHasFocus = hasFocus;
                Redraw();
            }
        }

        protected abstract void AttachHandlers(IRepository repository);
        protected abstract void DetachHandlers(IRepository repository);
        protected abstract void ValidateCachedData(IRepository repository);
        protected abstract void MaybeUpdateData();
    }

    [Serializable]
    class HistoryView : HistoryBase
    {
        [SerializeField] private bool currentLogHasUpdate;
        [SerializeField] private bool currentTrackingStatusHasUpdate;

        [SerializeField] private List<GitLogEntry> logEntries = new List<GitLogEntry>();

        [SerializeField] private int statusAhead;

        [SerializeField] private CacheUpdateEvent lastLogChangedEvent;
        [SerializeField] private CacheUpdateEvent lastTrackingStatusChangedEvent;

        [SerializeField] private HistoryControl historyControl;
        [SerializeField] private GitLogEntry selectedEntry = GitLogEntry.Default;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { DisplayRootNode = false };
        [SerializeField] private Vector2 detailsScroll;

        public override void Refresh()
        {
            base.Refresh();
            Refresh(CacheType.GitLog);
            Refresh(CacheType.GitAheadBehind);
        }

        private void RepositoryOnTrackingStatusChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastTrackingStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastTrackingStatusChangedEvent = cacheUpdateEvent;
                currentTrackingStatusHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnLogChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLogChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastLogChangedEvent = cacheUpdateEvent;
                currentLogHasUpdate = true;
                Redraw();
            }
        }

        protected override void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.TrackingStatusChanged += RepositoryOnTrackingStatusChanged;
            repository.LogChanged += RepositoryOnLogChanged;
        }

        protected override void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.TrackingStatusChanged -= RepositoryOnTrackingStatusChanged;
            repository.LogChanged -= RepositoryOnLogChanged;
        }

        protected override void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLog, lastLogChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitAheadBehind, lastTrackingStatusChangedEvent);
        }

        protected override void MaybeUpdateData()
        {
            if (Repository == null)
            {
                return;
            }

            if (currentTrackingStatusHasUpdate)
            {
                currentTrackingStatusHasUpdate = false;

                statusAhead = Repository.CurrentAhead;
            }

            if (currentLogHasUpdate)
            {
                currentLogHasUpdate = false;

                logEntries = Repository.CurrentLog;

                BuildHistoryControl(statusAhead, logEntries);
            }
        }

        public override void OnUI()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            DoHistoryGui(lastRect, entry => {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Revert"), false, RevertCommit);
                menu.ShowAsContext();
            }, node => {
                var menu = CreateChangesTreeContextMenu(node);
                menu.ShowAsContext();
            });
        }

        protected override HistoryControl HistoryControl
        {
            get { return historyControl; }
            set { historyControl = value; }
        }

        protected override GitLogEntry SelectedEntry
        {
            get { return selectedEntry; }
            set { selectedEntry = value; }
        }

        protected override ChangesTree TreeChanges
        {
            get { return treeChanges; }
            set { treeChanges = value; }
        }

        protected override Vector2 DetailsScroll
        {
            get { return detailsScroll; }
            set { detailsScroll = value; }
        }

        private GenericMenu CreateChangesTreeContextMenu(ChangesTreeNode node)
        {
            var genericMenu = new GenericMenu();

            genericMenu.AddItem(new GUIContent("Show History"), false, () => { });

            return genericMenu;
        }
    }

    [Serializable]
    class FileHistoryView : HistoryBase
    {
        [SerializeField] private bool currentFileLogHasUpdate;
        [SerializeField] private bool currentStatusEntriesHasUpdate;

        [SerializeField] private GitFileLog gitFileLog;

        [SerializeField] private HistoryControl historyControl;
        [SerializeField] private GitLogEntry selectedEntry = GitLogEntry.Default;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { DisplayRootNode = false };
        [SerializeField] private Vector2 detailsScroll;
        [SerializeField] private List<GitStatusEntry> gitStatusEntries = new List<GitStatusEntry>();

        [SerializeField] private CacheUpdateEvent lastStatusEntriesChangedEvent;
        [SerializeField] private CacheUpdateEvent lastFileLogChangedEvent;

        public override void Refresh()
        {
            base.Refresh();
            Refresh(CacheType.GitLog);
            Refresh(CacheType.GitAheadBehind);
        }

        private void RepositoryOnFileLogChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastFileLogChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastFileLogChangedEvent = cacheUpdateEvent;
                currentFileLogHasUpdate = true;
                Redraw();
            }
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

        protected override void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.FileLogChanged += RepositoryOnFileLogChanged;
            repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
        }

        protected override void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.FileLogChanged -= RepositoryOnFileLogChanged;
            repository.FileLogChanged -= RepositoryOnStatusEntriesChanged;
        }

        protected override void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitFileLog, lastFileLogChangedEvent);
        }

        protected override void MaybeUpdateData()
        {
            if (Repository == null)
            {
                return;
            }

            if (currentFileLogHasUpdate)
            {
                currentFileLogHasUpdate = false;

                gitFileLog = Repository.CurrentFileLog;

                BuildHistoryControl(0, gitFileLog.LogEntries);
            }

            if (currentStatusEntriesHasUpdate)
            {
                currentStatusEntriesHasUpdate = false;

                gitStatusEntries = Repository.CurrentChanges;
            }
        }

        public override void OnUI()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            DoHistoryGui(lastRect, entry => {
                GenericMenu menu = new GenericMenu();
                string checkoutPrompt = string.Format("Checkout revision {0}", entry.ShortID);
                menu.AddItem(new GUIContent(checkoutPrompt), false, () => Checkout(entry.commitID));
                menu.ShowAsContext();
            }, node => {
            });
        }

        protected override HistoryControl HistoryControl
        {
            get { return historyControl; }
            set { historyControl = value; }
        }

        protected override GitLogEntry SelectedEntry
        {
            get { return selectedEntry; }
            set { selectedEntry = value; }
        }

        protected override ChangesTree TreeChanges
        {
            get { return treeChanges; }
            set { treeChanges = value; }
        }

        protected override Vector2 DetailsScroll
        {
            get { return detailsScroll; }
            set { detailsScroll = value; }
        }

        private const string ConfirmCheckoutTitle = "Discard Changes?";
        private const string ConfirmCheckoutMessage = "You've made changes to file '{0}'.  Overwrite these changes with the historical version?";
        private const string ConfirmCheckoutOK = "Overwrite";
        private const string ConfirmCheckoutCancel = "Cancel";

        protected void Checkout(string commitId)
        {
            var promptUser = gitStatusEntries.Count > 0 && gitStatusEntries.Any(statusEntry => gitFileLog.Path.Equals(statusEntry.Path.ToNPath()));

            if (!promptUser || EditorUtility.DisplayDialog(ConfirmCheckoutTitle, string.Format(ConfirmCheckoutMessage, gitFileLog.Path), ConfirmCheckoutOK, ConfirmCheckoutCancel))
            {
                Repository.CheckoutVersion(commitId, new string[] { gitFileLog.Path })
                          .ThenInUI(AssetDatabase.Refresh)
                          .Start();
            }
        }
    }
}
