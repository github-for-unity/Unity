using UnityEngine;
using UnityEditor;
using System;


namespace GitHub.Unity
{
	class Styles
	{
		public const float
			BroadModeLimit = 500f,
			NarrowModeLimit = 300f,
			ModeNotificationDelay = .5f,
			BroadModeBranchesMinWidth = 200f,
			BroadModeBranchesRatio = .4f,
			InitialStateAreaWidth = 200f,
			BrowseFolderButtonHorizontalPadding = -4f,
			HistoryEntryHeight = 30f,
			HistorySummaryHeight = 16f,
			HistoryDetailsHeight = 16f,
			HistoryEntryPadding = 16f,
			HistoryChangesIndentation = 8f,
			CommitAreaMinHeight = 16f,
			CommitAreaDefaultRatio = .4f,
			CommitAreaMaxHeight = 10 * 15f,
			MinCommitTreePadding = 20f,
			FoldoutWidth = 11f,
			FoldoutIndentation = -2f,
			TreeIndentation = 17f,
			TreeRootIndentation = -5f,
			CommitIconSize = 16f,
			CommitIconHorizontalPadding = -5f,
			BranchListIndentation = 20f,
			BranchListSeperation = 15f,
			RemotesTotalHorizontalMargin = 37f,
			RemotesNameRatio = .2f,
			RemotesUserRatio = .2f,
			RemotesHostRation = .5f,
			RemotesAccessRatio = .1f,
			GitIgnoreRulesTotalHorizontalMargin = 33f,
			GitIgnoreRulesSelectorWidth = 14f,
			GitIgnoreRulesEffectRatio = .2f,
			GitIgnoreRulesFileRatio = .3f,
			GitIgnoreRulesLineRatio = .5f;


		const string
			BrowseButton = "...",
			WarningLabel = "<b>Warning:</b> {0}";


		static GUIStyle
			longMessageStyle,
			historyToolbarButtonStyle,
			historyLockStyle,
			historyEntryDetailsStyle,
			historyEntryDetailsRightStyle,
			commitFileAreaStyle,
			commitButtonStyle,
			commitDescriptionFieldStyle,
			toggleMixedStyle;
		static Texture2D
			titleIcon,
			defaultAssetIcon,
			folderIcon;


		public static GUIStyle LongMessageStyle
		{
			get
			{
				if (longMessageStyle == null)
				{
					longMessageStyle = new GUIStyle(EditorStyles.miniLabel);
					longMessageStyle.name = "LongMessageStyle";
					longMessageStyle.richText = true;
					longMessageStyle.wordWrap = true;
				}

				return longMessageStyle;
			}
		}


		public static GUIStyle HistoryToolbarButtonStyle
		{
			get
			{
				if (historyToolbarButtonStyle == null)
				{
					historyToolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
					historyToolbarButtonStyle.name = "HistoryToolbarButtonStyle";
					historyToolbarButtonStyle.richText = true;
					historyToolbarButtonStyle.wordWrap = true;
				}

				return historyToolbarButtonStyle;
			}
		}


		public static GUIStyle HistoryLockStyle
		{
			get
			{
				if (historyLockStyle == null)
				{
					historyLockStyle = new GUIStyle(GUI.skin.FindStyle("IN LockButton"));
					historyLockStyle.name = "HistoryLockStyle";
				}

				historyLockStyle.margin = new RectOffset(3, 3, 2, 2);

				return historyLockStyle;
			}
		}


		public static GUIStyle HistoryEntryDetailsStyle
		{
			get
			{
				if (historyEntryDetailsStyle == null)
				{
					historyEntryDetailsStyle = new GUIStyle(EditorStyles.miniLabel);
					historyEntryDetailsStyle.name = "HistoryEntryDetailsStyle";
					Color c = EditorStyles.miniLabel.normal.textColor;
					historyEntryDetailsStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.7f);
				}

				return historyEntryDetailsStyle;
			}
		}


		public static GUIStyle HistoryEntryDetailsRightStyle
		{
			get
			{
				if (historyEntryDetailsRightStyle == null)
				{
					historyEntryDetailsRightStyle = new GUIStyle(HistoryEntryDetailsStyle);
					historyEntryDetailsRightStyle.name = "HistoryEntryDetailsRightStyle";
				}

				historyEntryDetailsRightStyle.alignment = TextAnchor.MiddleRight;

				return historyEntryDetailsRightStyle;
			}
		}


		public static GUIStyle CommitFileAreaStyle
		{
			get
			{
				if (commitFileAreaStyle == null)
				{
					commitFileAreaStyle = new GUIStyle(GUI.skin.box);
					commitFileAreaStyle.name = "CommitFileAreaStyle";
					commitFileAreaStyle.margin = new RectOffset(0, 0, 0, 0);
				}

				return commitFileAreaStyle;
			}
		}


		public static GUIStyle CommitButtonStyle
		{
			get
			{
				if (commitButtonStyle == null)
				{
					commitButtonStyle = new GUIStyle(GUI.skin.button);
					commitButtonStyle.name = "CommitButtonStyle";
					commitButtonStyle.richText = true;
					commitButtonStyle.wordWrap = true;
				}

				return commitButtonStyle;
			}
		}


		public static GUIStyle CommitDescriptionFieldStyle
		{
			get
			{
				if (commitDescriptionFieldStyle == null)
				{
					commitDescriptionFieldStyle = new GUIStyle(GUI.skin.textArea);
					commitDescriptionFieldStyle.name = "CommitDescriptionFieldStyle";
					commitDescriptionFieldStyle.wordWrap = true;
				}

				return commitDescriptionFieldStyle;
			}
		}


		public static GUIStyle ToggleMixedStyle
		{
			get
			{
				if (toggleMixedStyle == null)
				{
					toggleMixedStyle = GUI.skin.FindStyle("ToggleMixed");
				}

				return toggleMixedStyle;
			}
		}


		public static Texture2D TitleIcon
		{
			get
			{
				if (titleIcon == null)
				{
					titleIcon = Utility.GetIcon("mark-github.png");
				}

				return titleIcon;
			}
		}


		public static Texture2D DefaultAssetIcon
		{
			get
			{
				if (defaultAssetIcon == null)
				{
					defaultAssetIcon = EditorGUIUtility.FindTexture("DefaultAsset Icon");
				}

				return defaultAssetIcon;
			}
		}


		public static Texture2D FolderIcon
		{
			get
			{
				if (folderIcon == null)
				{
					folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
				}

				return folderIcon;
			}
		}


		public static void BeginInitialStateArea(string title, string message)
		{
			GUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					GUILayout.BeginVertical(GUILayout.MaxWidth(Styles.InitialStateAreaWidth));
						GUILayout.Label(title, EditorStyles.boldLabel);
						GUILayout.Label(message, Styles.LongMessageStyle);
		}


		public static void EndInitialStateArea()
		{
					GUILayout.EndVertical();
					GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
		}


		public static bool InitialStateActionButton(string label)
		{
			bool result;

			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				result = GUILayout.Button(label, GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			return result;
		}


		public static void Warning(string message)
		{
			GUILayout.BeginHorizontal(EditorStyles.helpBox);
				GUILayout.Label(string.Format(WarningLabel, message), Styles.LongMessageStyle);
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}


		public static void PathField(ref string path, Func<string> browseFunction, Func<string, bool> validationFunction)
		{
			GUILayout.BeginHorizontal();
				path = EditorGUILayout.TextField(path);
				GUILayout.Space(Styles.BrowseFolderButtonHorizontalPadding);
				if (GUILayout.Button(BrowseButton, EditorStyles.miniButtonRight))
				{
					string newValue = browseFunction();
					if (!string.IsNullOrEmpty(newValue) && validationFunction(newValue))
					{
						path = newValue;
						GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
						GUI.changed = true;
					}
				}
			GUILayout.EndHorizontal();
		}
	}
}
