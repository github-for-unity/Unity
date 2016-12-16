using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

using Object = UnityEngine.Object;


namespace GitHub.Unity
{
	[System.Serializable]
	class HistoryView : Subview
	{
		enum LogEntryState
		{
			Normal,
			Local,
			Remote
		}


		const string
			HistoryFocusAll = "(All)",
			HistoryFocusSingle = "Focus: <b>{0}</b>",
			PullButton = "Pull",
			PullButtonCount = "Pull ({<b>0</b>})",
			PushButton = "Push",
			PushButtonCount = "Push (<b>{0}</b>)",
			PullConfirmTitle = "Pull Changes?",
			PullConfirmDescription = "Would you like to pull changes from remote '{0}'?",
			PullConfirmYes = "Pull",
			PullConfirmCancel = "Cancel",
			PushConfirmTitle = "Push Changes?",
			PushConfirmDescription = "Would you like to push changes to remote '{0}'?",
			PushConfirmYes = "Push",
			PushConfirmCancel = "Cancel",
			ClearSelectionButton = "x";
		const int
			HistoryExtraItemCount = 10;
		const float
			MaxChangelistHeightRatio = .2f;


		[SerializeField] List<GitLogEntry> history = new List<GitLogEntry>();
		[SerializeField] bool historyLocked = true;
		[SerializeField] Object historyTarget = null;
		[SerializeField] Vector2 scroll;
		[SerializeField] string selectionID;
		[SerializeField] ChangesetTreeView changesetTree = new ChangesetTreeView();
		[SerializeField] Vector2 detailsScroll;
		[SerializeField] bool broadMode = false;


		string currentRemote = "placeholder";
		int
			historyStartIndex,
			historyStopIndex,
			statusAhead,
			statusBehind;
		bool
			updated = true,
			useScrollTime = false;
		DateTimeOffset scrollTime = DateTimeOffset.Now;
		float
			lastWidth,
			scrollOffset;
		int
			selectionIndex,
			newSelectionIndex;


		public bool BroadMode { get { return broadMode; } }


		float EntryHeight
		{
			get
			{
				return Styles.HistoryEntryHeight + Styles.HistoryEntryPadding;
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


		void OnStatusUpdate(GitStatus update)
		{
			// Set branch state
			// TODO: Update currentRemote
			statusAhead = update.Ahead;
			statusBehind = update.Behind;
		}


		void OnLogUpdate(IList<GitLogEntry> entries)
		{
			updated = true;

			history.Clear();
			history.AddRange(entries);

			if (history.Any())
			{
				// Make sure that scroll as much as possible focuses the same time period in the new entry list
				if (useScrollTime)
				{
					int closestIndex = -1;
					double closestDifference = Mathf.Infinity;
					for (int index = 0; index < history.Count; ++index)
					{
						double diff = Math.Abs((history[index].Time - scrollTime).TotalSeconds);
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
				selectionIndex = Enumerable.Range(1, history.Count + 1).FirstOrDefault(index => history[index - 1].CommitID.Equals(selectionID)) - 1;

				if (selectionIndex < 0)
				{
					selectionID = string.Empty;
				}
			}

			Repaint();
		}


		void ScrollTo(int index, float offset = 0f)
		{
			scroll.Set(scroll.x, EntryHeight * index + offset);
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
			bool past = broadMode;

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

			Window window = (Window)parent;
			bool scaled = position.width != lastWidth;
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
				GUILayout.BeginVertical(
					GUILayout.MinWidth(Styles.BroadModeBranchesMinWidth),
					GUILayout.MaxWidth(Mathf.Max(Styles.BroadModeBranchesMinWidth, position.width * Styles.BroadModeBranchesRatio))
				);
					((Window)parent).BranchesTab.OnEmbeddedGUI();
				GUILayout.EndVertical();
				GUILayout.BeginVertical();
					OnEmbeddedGUI();
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}


		public void OnEmbeddedGUI()
		{
			// History toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				// Target indicator / clear button
				EditorGUI.BeginDisabledGroup(historyTarget == null);
					if (GUILayout.Button(
						historyTarget == null ? HistoryFocusAll : string.Format(HistoryFocusSingle, historyTarget.name),
						Styles.HistoryToolbarButtonStyle
					))
					{
						historyTarget = null;
						Refresh();
					}
				EditorGUI.EndDisabledGroup();

				GUILayout.FlexibleSpace();

				// Pull / Push buttons
				if (
					GUILayout.Button(statusBehind > 0 ? string.Format(PullButtonCount, statusBehind) : PullButton, Styles.HistoryToolbarButtonStyle) &&
					EditorUtility.DisplayDialog(
						PullConfirmTitle,
						string.Format(PullConfirmDescription, currentRemote),
						PullConfirmYes,
						PullConfirmCancel
					)
				)
				{
					Pull();
				}
				if (
					GUILayout.Button(statusAhead > 0 ? string.Format(PushButtonCount, statusAhead) : PushButton, Styles.HistoryToolbarButtonStyle) &&
					EditorUtility.DisplayDialog(
						PushConfirmTitle,
						string.Format(PushConfirmDescription, currentRemote),
						PushConfirmYes,
						PushConfirmCancel
					)
				)
				{
					Push();
				}

				// Target lock button
				EditorGUI.BeginChangeCheck();
					historyLocked = GUILayout.Toggle(historyLocked, GUIContent.none, Styles.HistoryLockStyle);
				if (EditorGUI.EndChangeCheck())
				{
					OnSelectionChange();
				}
			GUILayout.EndHorizontal();

			// When history scroll actually changes, store time value of topmost visible entry. This is the value we use to reposition scroll on log update - not the pixel value.
			if (history.Any())
			{
				int listID = GUIUtility.GetControlID(FocusType.Keyboard);

				// Only update time scroll
				Vector2 lastScroll = scroll;
				scroll = GUILayout.BeginScrollView(scroll);
					if (lastScroll != scroll && !updated)
					{
						scrollTime = history[historyStartIndex].Time;
						scrollOffset = scroll.y - historyStartIndex * EntryHeight;
						useScrollTime = true;
					}
					// Handle only the selected range of history items - adding spacing for the rest
					int
						start = Mathf.Max(0, historyStartIndex - HistoryExtraItemCount),
						stop = Mathf.Min(historyStopIndex + HistoryExtraItemCount, history.Count);
					GUILayout.Space(start * EntryHeight);
					for (int index = start; index < stop; ++index)
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
					if (Event.current.GetTypeForControl(listID) == EventType.KeyDown)
					{
						int change = 0;

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
							newSelectionIndex = (selectionIndex + change) %  history.Count;
							if (newSelectionIndex < historyStartIndex || newSelectionIndex > historyStopIndex)
							{
								ScrollTo(newSelectionIndex, (position.height - position.height * MaxChangelistHeightRatio - 30f - EntryHeight) * -.5f);
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
				GitLogEntry selection = history[selectionIndex];

				// Top bar for scrolling to selection or clearing it
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
					if (GUILayout.Button(selection.ShortID, Styles.HistoryToolbarButtonStyle))
					{
						ScrollTo(selectionIndex);
					}
					if (GUILayout.Button(ClearSelectionButton, Styles.HistoryToolbarButtonStyle, GUILayout.ExpandWidth(false)))
					{
						newSelectionIndex = -2;
					}
				GUILayout.EndHorizontal();

				// Log entry details - including changeset tree (if any changes are found)
				if (changesetTree.Entries.Any())
				{
					detailsScroll = GUILayout.BeginScrollView(
						detailsScroll,
						GUILayout.MinHeight(Mathf.Min(changesetTree.Height, position.height * MaxChangelistHeightRatio))
					);
						HistoryEntry(selection, GetEntryState(selectionIndex), false);

						GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

						GUILayout.BeginHorizontal();
							GUILayout.Space(Styles.HistoryChangesIndentation);
							changesetTree.OnGUI();
						GUILayout.EndHorizontal();

						GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
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


		LogEntryState GetEntryState(int index)
		{
			return historyTarget == null ? (index < statusAhead ? LogEntryState.Local : LogEntryState.Normal) : LogEntryState.Normal;
		}


		void CullHistory()
		// Recalculate the range of history items to handle - based on what is visible, plus a bit of padding for fast scrolling
		{
			historyStartIndex = (int)Mathf.Clamp(scroll.y / EntryHeight, 0, history.Count);
			historyStopIndex = (int)Mathf.Clamp(
				historyStartIndex + (position.height - 2f * Mathf.Min(changesetTree.Height, position.height * MaxChangelistHeightRatio)) / EntryHeight +
				1,
				0,
				history.Count
			);
		}


		bool HistoryEntry(GitLogEntry entry, LogEntryState state, bool selected)
		{
			Rect entryRect = GUILayoutUtility.GetRect(Styles.HistoryEntryHeight, Styles.HistoryEntryHeight);
			Rect
				summaryRect = new Rect(entryRect.x, entryRect.y, entryRect.width, Styles.HistorySummaryHeight),
				timestampRect = new Rect(entryRect.x, entryRect.yMax - Styles.HistoryDetailsHeight, entryRect.width * .5f, Styles.HistoryDetailsHeight);
			Rect authorRect = new Rect(timestampRect.xMax, timestampRect.y, timestampRect.width, timestampRect.height);

			if (selected && Event.current.type == EventType.Repaint)
			{
				EditorStyles.helpBox.Draw(entryRect, GUIContent.none, false, false, false, false);
			}

			if (!string.IsNullOrEmpty(entry.MergeA))
			{
				const float MergeIndicatorSize = 40f;
				Rect mergeIndicatorRect = new Rect(summaryRect.x, summaryRect.y, MergeIndicatorSize, summaryRect.height);
				GUI.Label(mergeIndicatorRect, "Merge:", Styles.HistoryEntryDetailsStyle);
				summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorSize, summaryRect.height);
			}

			if (state == LogEntryState.Local)
			{
				const float LocalIndicatorSize = 40f;
				Rect localIndicatorRect = new Rect(summaryRect.x, summaryRect.y, LocalIndicatorSize, summaryRect.height);
				GUI.Label(localIndicatorRect, "Local:", Styles.HistoryEntryDetailsStyle);
				summaryRect.Set(localIndicatorRect.xMax, summaryRect.y, summaryRect.width - LocalIndicatorSize, summaryRect.height);
			}

			GUI.Label(summaryRect, entry.Summary);
			GUI.Label(timestampRect, entry.PrettyTimeString, Styles.HistoryEntryDetailsStyle);
			GUI.Label(authorRect, entry.AuthorName, Styles.HistoryEntryDetailsRightStyle);

			if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
			{
				Event.current.Use();
				return true;
			}

			return false;
		}


		void Pull()
		{
			Debug.Log("TODO: Pull");
		}


		void Push()
		{
			Debug.Log("TODO: Push");
		}
	}
}
