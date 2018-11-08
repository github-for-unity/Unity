using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    public class FileHistoryWindow : BaseWindow
    {
        [SerializeField] private string assetPath;
        [SerializeField] private List<GitLogEntry> history;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private Vector2 detailsScroll;
        [NonSerialized] private bool busy;
        [SerializeField] private HistoryControl historyControl;
        [SerializeField] private GitLogEntry selectedEntry = GitLogEntry.Default;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { IsSelectable = false, DisplayRootNode = false };

        public static FileHistoryWindow OpenWindow(string assetPath)
        {
            var popupWindow = CreateInstance<FileHistoryWindow>();

            popupWindow.titleContent = new GUIContent(assetPath + " History");
            popupWindow.Open(assetPath);

            popupWindow.Show();

            return popupWindow;
        }

        public override bool IsBusy { get { return this.busy; } }

        public void Open(string assetPath)
        {
            this.assetPath = assetPath;

            this.RefreshLog();
        }

        public void RefreshLog()
        {
            var path =  Application.dataPath.ToNPath().Parent.Combine(assetPath.ToNPath());
            this.busy = true;
            this.GitClient.LogFile(path).ThenInUI((success, logEntries) => {
                this.history = logEntries;
                this.BuildHistoryControl();
                this.Repaint();
                this.busy = false;
            }).Start();
        }

        private void CheckoutVersion(string commitID)
        {
            this.busy = true;
            this.GitClient.CheckoutVersion(commitID, new string[]{assetPath}).ThenInUI((success, result) => {
                AssetDatabase.Refresh();
                this.busy = false;
            }).Start();
        }

        private void Checkout()
        {
            // TODO:  This is a destructive, irreversible operation; we should prompt user if
            // there are any changes to the file
            this.CheckoutVersion(this.selectedEntry.CommitID);
        }

        public override void OnUI()
        {
            // TODO:
            //   - should handle case where the file is outside of the repository (handle exceptional cases)
            //   - should display a spinner while history is still loading...
            base.OnUI();
            GUILayout.BeginHorizontal(Styles.HeaderStyle);
            {
                GUILayout.Label("GIT File History for: ", Styles.BoldLabel);
                if (HyperlinkLabel(this.assetPath))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(this.assetPath);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            if (historyControl != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                var historyControlRect = new Rect(0f, 0f, Position.width, Position.height - rect.height);

                var requiresRepaint = historyControl.Render(historyControlRect,  
                    entry => {
                        selectedEntry = entry;
                        BuildTree();
                    },
                    entry => { }, entry => {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Checkout version " + entry.ShortID), false, Checkout);
                        menu.ShowAsContext();
                    });

                if (requiresRepaint)
                    Redraw();
            }

            // DrawDetails is maybe irrelevant?  Would be a nice place to put the short id perhaps?
            DrawDetails();
        }

        private bool HyperlinkLabel(string label)
        {
            bool returnValue = false;
            if (GUILayout.Button(label, HyperlinkStyle))
            {
                returnValue = true;
            }
            var rect = GUILayoutUtility.GetLastRect();
            var size = HyperlinkStyle.CalcSize(new GUIContent(label));
            rect.width = size.x;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            return returnValue;
        }

        private void BuildHistoryControl()
        {
            if (historyControl == null)
            {
                historyControl = new HistoryControl();
            }

            historyControl.Load(0, this.history);
        }

        private const string CommitDetailsTitle = "Commit details";
        private const string ClearSelectionButton = "×";

        private void DrawDetails()
        {
            if (!selectedEntry.Equals(GitLogEntry.Default))
            {
                // Top bar for scrolling to selection or clearing it
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button(CommitDetailsTitle, Styles.ToolbarButtonStyle))
                    {
                        historyControl.ScrollTo(historyControl.SelectedIndex);
                    }
                    if (GUILayout.Button(ClearSelectionButton, Styles.ToolbarButtonStyle, GUILayout.ExpandWidth(false)))
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

                    var rect = GUILayoutUtility.GetLastRect();
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
                                node => {
                                },
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

        private void BuildTree()
        {
            treeChanges.PathSeparator = Environment.FileSystem.DirectorySeparatorChar.ToString();
            treeChanges.Load(selectedEntry.changes.Select(entry => new GitStatusEntryTreeData(entry)));
            Redraw();
        }

        protected static GUIStyle hyperlinkStyle = null;

        public static GUIStyle HyperlinkStyle
        {
            get
            {
                if (hyperlinkStyle == null)
                {
                    hyperlinkStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    hyperlinkStyle.normal.textColor = new Color(95.0f/255.0f, 170.0f/255.0f, 247.0f/255.0f);
                }
                return hyperlinkStyle;
            }
        }        
    }
}