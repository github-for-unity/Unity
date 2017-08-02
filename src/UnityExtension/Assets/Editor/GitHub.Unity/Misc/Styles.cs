using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class Styles
    {
        public const float BaseSpacing = 10f,
                           BroadModeLimit = 500f,
                           NarrowModeLimit = 300f,
                           ModeNotificationDelay = .5f,
                           BroadModeBranchesMinWidth = 200f,
                           BroadModeBranchesRatio = .4f,
                           InitialStateAreaWidth = 200f,
                           HistoryEntryHeight = 40f,
                           HistorySummaryHeight = 16f,
                           HistoryDetailsHeight = 16f,
                           HistoryEntryPadding = 16f,
                           HistoryChangesIndentation = 17f,
                           CommitAreaMinHeight = 16f,
                           CommitAreaDefaultRatio = .4f,
                           CommitAreaMaxHeight = 12 * 15f,
                           CommitAreaPadding = 5f,
                           MinCommitTreePadding = 20f,
                           FoldoutWidth = 11f,
                           FoldoutIndentation = -2f,
                           TreeIndentation = 12f,
                           TreeRootIndentation = -5f,
                           TreeVerticalSpacing = 3f,
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

        public const int HalfSpacing = (int)(BaseSpacing / 2);

        private const string BrowseButton = "...";
        private const string WarningLabel = "<b>Warning:</b> {0}";

        private static Color headerGreyColor = new Color(0.878f, 0.878f, 0.878f, 1.0f);

        private static GUIStyle label,
                                boldLabel,
                                errorLabel,
                                deletedFileLabel,
                                longMessageStyle,
                                headerBoxStyle,
                                headerBranchLabelStyle,
                                headerRepoLabelStyle,
                                headerTitleStyle,
                                headerDescriptionStyle,
                                historyToolbarButtonStyle,
                                historyLockStyle,
                                historyEntryDetailsStyle,
                                historyEntryDetailsRightStyle,
                                historyFileTreeBoxStyle,
                                commitFileAreaStyle,
                                commitButtonStyle,
                                textFieldStyle,
                                centeredLabel,
                                commitDescriptionFieldStyle,
                                toggleMixedStyle,
                                authHeaderBoxStyle,
                                lockedFileRowSelectedStyle,
                                lockedFileRowStyle,
                                genericTableBoxStyle,
                                historyDetailsTitleStyle,
                                historyDetailsMetaInfoStyle,
                                genericBoxStyle;

        private static Texture2D branchIcon,
                                 activeBranchIcon,
                                 trackingBranchIcon,
                                 favouriteIconOn,
                                 favouriteIconOff,
                                 smallLogoIcon,
                                 bigLogoIcon,
                                 defaultAssetIcon,
                                 folderIcon,
                                 mergeIcon,
                                 dotIcon,
                                 localCommitIcon,
                                 repoIcon,
                                 lockIcon,
                                 dropdownListIcon;

        private static Color timelineBarColor;

        public static Texture2D GetFileStatusIcon(GitFileStatus status, bool isLocked)
        {
            if (isLocked)
            {
                switch (status)
                {
                    case GitFileStatus.Modified:
                        return Utility.GetIcon("locked.png", "locked@2x.png");

                    default:
                        return Utility.GetIcon("locked-by-person.png", "locked-by-person@2x.png");
                }
            }

            switch (status)
            {
                case GitFileStatus.Modified:
                    return Utility.GetIcon("modified.png", "modified@2x.png");
                case GitFileStatus.Added:
                    return Utility.GetIcon("added.png", "added@2x.png");
                case GitFileStatus.Deleted:
                    return Utility.GetIcon("removed.png", "removed@2x.png");
                case GitFileStatus.Renamed:
                    return Utility.GetIcon("renamed.png", "renamed@2x.png");
                case GitFileStatus.Untracked:
                    return Utility.GetIcon("untracked.png", "untracked@2x.png");
            }

            return null;
        }

        public static void BeginInitialStateArea(string title, string message)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.MaxWidth(InitialStateAreaWidth));
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(message, LongMessageStyle);
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
            GUILayout.Label(string.Format(WarningLabel, message), LongMessageStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static void PathField(ref string path, Func<string> browseFunction, Func<string, bool> validationFunction)
        {
            GUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField("Path to Git", path);
            if (GUILayout.Button(BrowseButton, EditorStyles.miniButton, GUILayout.Width(25)))
            {
                var newValue = browseFunction();
                if (!string.IsNullOrEmpty(newValue) && validationFunction(newValue))
                {
                    path = newValue;
                    GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
                    GUI.changed = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        public static GUIStyle HistoryFileTreeBoxStyle
        {
            get
            {
                if (historyFileTreeBoxStyle == null)
                {
                    var padding = new RectOffset((int)HistoryChangesIndentation, 0, 0, 0);

                    historyFileTreeBoxStyle = new GUIStyle();
                    historyFileTreeBoxStyle.padding = padding;
                }

                return historyFileTreeBoxStyle;
            }
        }

        public static GUIStyle Label
        {
            get
            {
                if (label == null)
                {
                    label = new GUIStyle(GUI.skin.label);
                    label.name = "CustomLabel";

                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    label.onNormal.background = hierarchyStyle.onNormal.background;
                    label.onNormal.textColor = hierarchyStyle.onNormal.textColor;
                    label.onFocused.background = hierarchyStyle.onFocused.background;
                    label.onFocused.textColor = hierarchyStyle.onFocused.textColor;
                }
                return label;
            }
        }

        public static GUIStyle HeaderBranchLabelStyle
        {
            get
            {
                if (headerBranchLabelStyle == null)
                {
                    headerBranchLabelStyle = new GUIStyle(EditorStyles.label);
                    headerBranchLabelStyle.name = "HeaderBranchLabelStyle";
                    headerBranchLabelStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return headerBranchLabelStyle;
            }
        }

        public static GUIStyle HeaderRepoLabelStyle
        {
            get
            {
                if (headerRepoLabelStyle == null)
                {
                    headerRepoLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    headerRepoLabelStyle.name = "HeaderRepoLabelStyle";
                    headerRepoLabelStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return headerRepoLabelStyle;
            }
        }

        public static GUIStyle HeaderTitleStyle
        {
            get
            {
                if (headerTitleStyle == null)
                {
                    headerTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                    headerTitleStyle.name = "HeaderTitleStyle";
                    headerTitleStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerTitleStyle.wordWrap = true;
                }
                return headerTitleStyle;
            }
        }

        public static GUIStyle HeaderDescriptionStyle
        {
            get
            {
                if (headerDescriptionStyle == null)
                {
                    headerDescriptionStyle = new GUIStyle(EditorStyles.label);
                    headerDescriptionStyle.name = "HeaderDescriptionStyle";
                    headerDescriptionStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerDescriptionStyle.wordWrap = true;
                }
                return headerDescriptionStyle;
            }
        }

        public static GUIStyle HeaderBoxStyle
        {
            get
            {
                if (headerBoxStyle == null)
                {
                    headerBoxStyle = new GUIStyle("IN BigTitle");
                    headerBoxStyle.name = "HeaderBoxStyle";
                    headerBoxStyle.padding = new RectOffset(5, 5, 5, 5);
                    headerBoxStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return headerBoxStyle;
            }
        }

        public static GUIStyle BoldLabel
        {
            get
            {
                if (boldLabel == null)
                {
                    boldLabel = new GUIStyle(Label);
                    boldLabel.name = "CustomBoldLabel";

                    boldLabel.fontStyle = FontStyle.Bold;
                }
                return boldLabel;
            }
        }

        public static GUIStyle DeletedFileLabel
        {
            get
            {
                if (deletedFileLabel == null)
                {
                    deletedFileLabel = new GUIStyle(EditorStyles.label);
                    deletedFileLabel.name = "DeletedFileLabel";
                    deletedFileLabel.normal.textColor = Color.gray;
                }
                return deletedFileLabel;
            }
        }

        public static GUIStyle ErrorLabel
        {
            get
            {
                if (errorLabel == null)
                {
                    errorLabel = new GUIStyle(EditorStyles.label);
                    errorLabel.name = "ErrorLabel";
                    errorLabel.wordWrap = true;
                    errorLabel.normal.textColor = Color.red;
                }
                return errorLabel;
            }
        }

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
                    var c = EditorStyles.miniLabel.normal.textColor;
                    historyEntryDetailsStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.7f);

                    historyEntryDetailsStyle.onNormal.background = Label.onNormal.background;
                    historyEntryDetailsStyle.onNormal.textColor = Label.onNormal.textColor;
                    historyEntryDetailsStyle.onFocused.background = Label.onFocused.background;
                    historyEntryDetailsStyle.onFocused.textColor = Label.onFocused.textColor;
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

        public static GUIStyle LockedFileRowStyle
        {
            get
            {
                if (lockedFileRowStyle == null)
                {
                    lockedFileRowStyle = new GUIStyle();
                    lockedFileRowStyle.name = "LockedFileRowStyle";
                    lockedFileRowStyle.padding = new RectOffset(2, 2, 1, 1);
                }
                return lockedFileRowStyle;
            }
        }

        public static GUIStyle LockedFileRowSelectedStyle
        {
            get
            {
                if (lockedFileRowSelectedStyle == null)
                {
                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    lockedFileRowSelectedStyle = new GUIStyle(LockedFileRowStyle);
                    lockedFileRowSelectedStyle.name = "LockedFileRowSelectedStyle";
                    lockedFileRowSelectedStyle.normal.background = hierarchyStyle.onFocused.background;
                    lockedFileRowSelectedStyle.normal.textColor = hierarchyStyle.onFocused.textColor;
                }
                return lockedFileRowSelectedStyle;
            }
        }

        public static GUIStyle GenericTableBoxStyle
        {
            get
            {
                if (genericTableBoxStyle == null)
                {
                    genericTableBoxStyle = new GUIStyle(GUI.skin.box);
                    genericTableBoxStyle.name = "GenericTableBoxStyle";
                }
                return genericTableBoxStyle;
            }
        }

        public static GUIStyle HistoryDetailsTitleStyle
        {
            get
            {
                if (historyDetailsTitleStyle == null)
                {
                    historyDetailsTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                    historyDetailsTitleStyle.name = "HistoryDetailsTitleStyle";
                    historyDetailsTitleStyle.wordWrap = true;
                }
                return historyDetailsTitleStyle;
            }
        }

        public static GUIStyle HistoryDetailsMetaInfoStyle
        {
            get
            {
                if (historyDetailsMetaInfoStyle == null)
                {
                    historyDetailsMetaInfoStyle = new GUIStyle(EditorStyles.miniLabel);
                    historyDetailsMetaInfoStyle.name = "HistoryDetailsMetaInfoStyle";
                    historyDetailsMetaInfoStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
                }
                return historyDetailsMetaInfoStyle;
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

        public static GUIStyle TextFieldStyle
        {
            get
            {
                if (textFieldStyle == null)
                {
                    textFieldStyle = new GUIStyle(GUI.skin.textField);
                    textFieldStyle.name = "TextFieldStyle";
                    textFieldStyle.fixedHeight = 21;
                    textFieldStyle.padding = new RectOffset(HalfSpacing, HalfSpacing, 4, 0);
                }
                return textFieldStyle;
            }
        }

        public static GUIStyle CenteredLabel
        {
            get
            {
                if (centeredLabel == null)
                {
                    centeredLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
                    centeredLabel.alignment = TextAnchor.MiddleCenter;
                }
                return centeredLabel;
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
                    commitDescriptionFieldStyle.padding = new RectOffset(HalfSpacing, HalfSpacing, HalfSpacing, HalfSpacing);
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

        public static GUIStyle AuthHeaderBoxStyle
        {
            get
            {
                if (authHeaderBoxStyle == null)
                {
                    authHeaderBoxStyle = new GUIStyle(HeaderBoxStyle);
                    authHeaderBoxStyle.name = "AuthHeaderBoxStyle";
                    authHeaderBoxStyle.padding = new RectOffset(10, 10, 0, 5);
                }
                return authHeaderBoxStyle;
            }
        }

        public static GUIStyle GenericBoxStyle
        {
            get
            {
                if (genericBoxStyle == null)
                {
                    genericBoxStyle = new GUIStyle();
                    genericBoxStyle.padding = new RectOffset(5, 5, 5, 5);
                }
                return genericBoxStyle;
            }
        }

        public static Color TimelineBarColor
        {
            get
            {
                if (timelineBarColor == null)
                {
                    timelineBarColor = new Color(0.51F, 0.51F, 0.51F, 0.2F);
                }
                return timelineBarColor;
            }
        }

        public static Texture2D ActiveBranchIcon
        {
            get
            {
                if (activeBranchIcon == null)
                {
                    activeBranchIcon = Utility.GetIcon("current-branch-indicator.png", "current-branch-indicator@2x.png");
                }
                return activeBranchIcon;
            }
        }

        public static Texture2D BranchIcon
        {
            get
            {
                if (branchIcon == null)
                {
                    branchIcon = Utility.GetIcon("branch.png", "branch@2x.png");
                }
                return branchIcon;
            }
        }

        public static Texture2D TrackingBranchIcon
        {
            get
            {
                if (trackingBranchIcon == null)
                {
                    trackingBranchIcon = Utility.GetIcon("tracked-branch-indicator.png");
                }

                return trackingBranchIcon;
            }
        }

        public static Texture2D FavouriteIconOn
        {
            get
            {
                if (favouriteIconOn == null)
                {
                    favouriteIconOn = Utility.GetIcon("favorite-branch-indicator.png");
                }

                return favouriteIconOn;
            }
        }

        public static Texture2D FavouriteIconOff
        {
            get
            {
                if (favouriteIconOff == null)
                {
                    favouriteIconOff = FolderIcon;
                }

                return favouriteIconOff;
            }
        }

        public static Texture2D SmallLogo
        {
            get
            {
                if (smallLogoIcon == null)
                {
                    smallLogoIcon = Utility.GetIcon("small-logo.png");
                }

                return smallLogoIcon;
            }
        }

        public static Texture2D BigLogo
        {
            get
            {
                if (bigLogoIcon == null)
                {
                    bigLogoIcon = Utility.GetIcon("big-logo.png");
                }

                return bigLogoIcon;
            }
        }

        public static Texture2D MergeIcon
        {
            get
            {
                if (mergeIcon == null)
                {
                    mergeIcon = Utility.GetIcon("git-merge.png", "git-merge@2x.png");
                }

                return mergeIcon;
            }
        }

        public static Texture2D DotIcon
        {
            get
            {
                if (dotIcon == null)
                {
                    dotIcon = Utility.GetIcon("dot.png", "dot@2x.png");
                }

                return dotIcon;
            }
        }

        public static Texture2D LocalCommitIcon
        {
            get
            {
                if (localCommitIcon == null)
                {
                    localCommitIcon = Utility.GetIcon("local-commit-icon.png", "local-commit-icon@2x.png");
                }

                return localCommitIcon;
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

        public static Texture2D RepoIcon
        {
            get
            {
                if (repoIcon == null)
                {
                    repoIcon = Utility.GetIcon("repo.png", "repo@2x.png");
                }
                return repoIcon;
            }
        }

        public static Texture2D LockIcon
        {
            get
            {
                if (lockIcon == null)
                {
                    lockIcon = Utility.GetIcon("lock.png", "lock@2x.png");
                }
                return lockIcon;
            }
        }

        public static Texture2D DropdownListIcon
        {
            get
            {
                if (dropdownListIcon == null)
                {
                    dropdownListIcon = Utility.GetIcon("dropdown-list-icon.png", "dropdown-list-icon@2x.png");
                }
                return dropdownListIcon;
            }
        }
    }
}
