using UnityEngine;
using UnityEditor;
using System;
using System.Linq;


namespace GitHub.Unity
{
	[System.Serializable]
	class ChangesView : Subview
	{
		const string
			SummaryLabel = "Commit summary",
			DescriptionLabel = "Commit description",
			CommitButton = "Commit to <b>{0}</b>",
			SelectAllButton = "All",
			SelectNoneButton = "None",
			ChangedFilesLabel = "{0} changed files",
			OneChangedFileLabel = "1 changed file",
			NoChangedFilesLabel = "No changed files";


		[SerializeField] Vector2
			verticalScroll,
			horizontalScroll;
		[SerializeField] string
			commitMessage = "",
			commitBody = "",
			currentBranch = "[unknown]";
		[SerializeField] ChangesetTreeView tree = new ChangesetTreeView();


		bool lockCommit = true;


		protected override void OnShow()
		{
			tree.Show(this);
			GitStatusTask.RegisterCallback(OnStatusUpdate);
		}


		protected override void OnHide()
		{
			GitStatusTask.UnregisterCallback(OnStatusUpdate);
		}


		public override void Refresh()
		{
			GitStatusTask.Schedule();
		}


		void OnStatusUpdate(GitStatus update)
		{
			// Set branch state
			currentBranch = update.LocalBranch;

			// (Re)build tree
			tree.Update(update.Entries);

			lockCommit = false;
		}


		public override void OnGUI()
		{
			if (tree.Height > 0)
			{
				verticalScroll = GUILayout.BeginScrollView(verticalScroll);
			}
			else
			{
				GUILayout.BeginScrollView(verticalScroll);
			}
				GUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup(tree.Entries.Count == 0);
						if (GUILayout.Button(SelectAllButton, EditorStyles.miniButtonLeft))
						{
							SelectAll();
						}

						if (GUILayout.Button(SelectNoneButton, EditorStyles.miniButtonRight))
						{
							SelectNone();
						}
					EditorGUI.EndDisabledGroup();

					GUILayout.FlexibleSpace();

					GUILayout.Label(
						tree.Entries.Count == 0 ? NoChangedFilesLabel :
							tree.Entries.Count == 1 ? OneChangedFileLabel :
								string.Format(ChangedFilesLabel, tree.Entries.Count),
						EditorStyles.miniLabel
					);
				GUILayout.EndHorizontal();

				GUILayout.BeginVertical(Styles.CommitFileAreaStyle);
					if (tree.Height > 0)
					// Specify a minimum height if we can - avoiding vertical scrollbars on both the outer and inner scroll view
					{
						horizontalScroll = GUILayout.BeginScrollView(
							horizontalScroll,
							GUILayout.MinHeight(tree.Height),
							GUILayout.MaxHeight(100000f) // NOTE: This ugliness is necessary as unbounded MaxHeight appears impossible when MinHeight is specified
						);
					}
					// If we have no minimum height to work with, just stretch and hope
					else
					{
						horizontalScroll = GUILayout.BeginScrollView(horizontalScroll);
					}
						tree.OnGUI();
					GUILayout.EndScrollView();
				GUILayout.EndVertical();

				// Do the commit details area
				OnCommitDetailsAreaGUI();
			GUILayout.EndScrollView();
		}


		void OnCommitDetailsAreaGUI()
		{
			GUILayout.BeginVertical(
				GUILayout.Height(Mathf.Clamp(position.height * Styles.CommitAreaDefaultRatio, Styles.CommitAreaMinHeight, Styles.CommitAreaMaxHeight))
			);
				GUILayout.Label(SummaryLabel);
				commitMessage = GUILayout.TextField(commitMessage);

				GUILayout.Label(DescriptionLabel);
				commitBody = EditorGUILayout.TextArea(commitBody, Styles.CommitDescriptionFieldStyle, GUILayout.ExpandHeight(true));

				// Disable committing when already committing or if we don't have all the data needed
				EditorGUI.BeginDisabledGroup(lockCommit || string.IsNullOrEmpty(commitMessage) || !tree.CommitTargets.Any(t => t.Any));
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(string.Format(CommitButton, currentBranch), Styles.CommitButtonStyle))
						{
							Commit();
						}
					GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			GUILayout.EndVertical();
		}


		void SelectAll()
		{
			for (int index = 0; index < tree.CommitTargets.Count; ++index)
			{
				tree.CommitTargets[index].All = true;
			}
		}


		void SelectNone()
		{
			for (int index = 0; index < tree.CommitTargets.Count; ++index)
			{
				tree.CommitTargets[index].All = false;
			}
		}


		void Commit()
		{
			// Do not allow new commits before we have received one successful update
			lockCommit = true;

			// Schedule the commit with the selected files
			GitCommitTask.Schedule(
				Enumerable.Range(0, tree.Entries.Count).Where(i => tree.CommitTargets[i].All).Select(i => tree.Entries[i].Path),
				commitMessage,
				commitBody,
				() =>
				{
					commitMessage = "";
					commitBody = "";
					for (int index = 0; index < tree.Entries.Count; ++index)
					{
						tree.CommitTargets[index].Clear();
					}
				},
				() => lockCommit = false
			);
		}
	}
}
