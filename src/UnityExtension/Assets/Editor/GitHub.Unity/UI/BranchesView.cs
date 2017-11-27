using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitHub.Unity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class BranchesView : Subview
    {
        private const string ConfirmSwitchTitle = "Confirm branch switch";
        private const string ConfirmSwitchMessage = "Switch branch to {0}?";
        private const string ConfirmSwitchOK = "Switch";
        private const string ConfirmSwitchCancel = "Cancel";
        private const string ConfirmCheckoutBranchTitle = "Confirm branch checkout";
        private const string ConfirmCheckoutBranchMessage = "Checkout branch {0} from {1}?";
        private const string ConfirmCheckoutBranchOK = "Checkout";
        private const string ConfirmCheckoutBranchCancel = "Cancel";
        private const string WarningCheckoutBranchExistsTitle = "Branch already exists";
        private const string WarningCheckoutBranchExistsMessage = "Branch {0} already exists";
        private const string WarningCheckoutBranchExistsOK = "Ok";
        private const string NewBranchCancelButton = "x";
        private const string NewBranchConfirmButton = "Create";
        private const string CreateBranchTitle = "Create Branch";
        private const string LocalTitle = "Local branches";
        private const string RemoteTitle = "Remote branches";
        private const string CreateBranchButton = "New Branch";
        private const string DeleteBranchMessageFormatString = "Are you sure you want to delete the branch: {0}?";
        private const string DeleteBranchTitle = "Delete Branch?";
        private const string DeleteBranchButton = "Delete";
        private const string CancelButtonLabel = "Cancel";

        [NonSerialized] private int listID = -1;
        [NonSerialized] private BranchesMode targetMode;

        [SerializeField] private Tree treeLocals = new Tree();
        [SerializeField] private Tree treeRemotes = new Tree();
        [SerializeField] private BranchesMode mode = BranchesMode.Default;
        [SerializeField] private string newBranchName;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private bool disableDelete;

        [SerializeField] private CacheUpdateEvent lastLocalAndRemoteBranchListChangedEvent;
        [NonSerialized] private bool localAndRemoteBranchListHasUpdate;

        [SerializeField] private List<GitBranch> localBranches;
        [SerializeField] private List<GitBranch> remoteBranches;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            targetMode = mode;
        }


        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);
            Repository.CheckLocalAndRemoteBranchListChangedEvent(lastLocalAndRemoteBranchListChangedEvent);
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

        private void RepositoryOnLocalAndRemoteBranchListChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocalAndRemoteBranchListChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocalAndRemoteBranchListChangedEvent = cacheUpdateEvent;
                localAndRemoteBranchListHasUpdate = true;
                Redraw();
            }
        }

        private void MaybeUpdateData()
        {
            if (localAndRemoteBranchListHasUpdate)
            {
                localAndRemoteBranchListHasUpdate = false;

                localBranches = Repository.LocalBranches.ToList();
                remoteBranches = Repository.RemoteBranches.ToList();

                BuildTree();
            }

            disableDelete = treeLocals.SelectedNode == null || treeLocals.SelectedNode.IsFolder || treeLocals.SelectedNode.IsActive;
        }

        public override void OnGUI()
        {
            Render();
        }

        private void AttachHandlers(IRepository repository)
        {
            repository.LocalAndRemoteBranchListChanged += RepositoryOnLocalAndRemoteBranchListChanged;
        }

        private void DetachHandlers(IRepository repository)
        {
            repository.LocalAndRemoteBranchListChanged -= RepositoryOnLocalAndRemoteBranchListChanged;
        }

        private void Render()
        {
            listID = GUIUtility.GetControlID(FocusType.Keyboard);
            GUILayout.BeginHorizontal();
            {
                OnButtonBarGUI();
            }
            GUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            scroll = GUILayout.BeginScrollView(scroll);
            {
                OnTreeGUI(new Rect(0f, 0f, Position.width, Position.height - rect.height + Styles.CommitAreaPadding)); 
            }
            GUILayout.EndScrollView();
        }

        private void BuildTree()
        {
            localBranches.Sort(CompareBranches);
            remoteBranches.Sort(CompareBranches);
            treeLocals = new Tree();
            treeLocals.ActiveNodeIcon = Styles.ActiveBranchIcon;
            treeLocals.NodeIcon = Styles.BranchIcon;
            treeLocals.RootFolderIcon = Styles.RootFolderIcon;
            treeLocals.FolderIcon = Styles.FolderIcon;

            treeRemotes = new Tree();
            treeRemotes.ActiveNodeIcon = Styles.ActiveBranchIcon;
            treeRemotes.NodeIcon = Styles.BranchIcon;
            treeRemotes.RootFolderIcon = Styles.RootFolderIcon;
            treeRemotes.FolderIcon = Styles.FolderIcon;

            treeLocals.Load(localBranches.Cast<ITreeData>(), LocalTitle);
            treeRemotes.Load(remoteBranches.Cast<ITreeData>(), RemoteTitle);
            Redraw();
        }

        private void OnButtonBarGUI()
        {
            if (mode == BranchesMode.Default)
            {
                // Delete button
                // If the current branch is selected, then do not enable the Delete button
                EditorGUI.BeginDisabledGroup(disableDelete);
                {
                    if (GUILayout.Button(DeleteBranchButton, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        var selectedBranchName = treeLocals.SelectedNode.Name;
                        var dialogMessage = string.Format(DeleteBranchMessageFormatString, selectedBranchName);
                        if (EditorUtility.DisplayDialog(DeleteBranchTitle, dialogMessage, DeleteBranchButton, CancelButtonLabel))
                        {
                            GitClient.DeleteBranch(selectedBranchName, true).Start();
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                // Create button
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(CreateBranchButton, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    targetMode = BranchesMode.Create;
                }
            }
            // Branch name + cancel + create
            else if (mode == BranchesMode.Create)
            {
                GUILayout.BeginHorizontal();
                {
                    var createBranch = false;
                    var cancelCreate = false;
                    var cannotCreate = treeLocals.SelectedNode == null ||
                                       treeLocals.SelectedNode.IsFolder ||
                                       !Validation.IsBranchNameValid(newBranchName);

                    // Create on return/enter or cancel on escape
                    var offsetID = GUIUtility.GetControlID(FocusType.Passive);
                    if (Event.current.isKey && GUIUtility.keyboardControl == offsetID + 1)
                    {
                        if (Event.current.keyCode == KeyCode.Escape)
                        {
                            cancelCreate = true;
                            Event.current.Use();
                        }
                        else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                        {
                            if (cannotCreate)
                            {
                                EditorApplication.Beep();
                            }
                            else
                            {
                                createBranch = true;
                            }
                            Event.current.Use();
                        }
                    }
                    newBranchName = EditorGUILayout.TextField(newBranchName);

                    // Create
                    EditorGUI.BeginDisabledGroup(cannotCreate);
                    {
                        if (GUILayout.Button(NewBranchConfirmButton, EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false)))
                        {
                            createBranch = true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    // Cancel create
                    if (GUILayout.Button(NewBranchCancelButton, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                    {
                        cancelCreate = true;
                    }

                    // Effectuate create
                    if (createBranch)
                    {
                        GitClient.CreateBranch(newBranchName, treeLocals.SelectedNode.Name)
                            .FinallyInUI((success, e) =>
                            {
                                if (success)
                                {
                                    Redraw();
                                }
                                else
                                {
                                    var errorHeader = "fatal: ";
                                    var errorMessage = e.Message.StartsWith(errorHeader) ? e.Message.Remove(0, errorHeader.Length) : e.Message;

                                    EditorUtility.DisplayDialog(CreateBranchTitle,
                                        errorMessage,
                                        Localization.Ok);
                                }
                            })
                            .Start();
                    }

                    // Cleanup
                    if (createBranch || cancelCreate)
                    {
                        newBranchName = "";
                        GUIUtility.keyboardControl = -1;
                        targetMode = BranchesMode.Default;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void OnTreeGUI(Rect rect)
        {
             var initialRect = rect;

            if (treeLocals.FolderStyle == null)
            {
                treeLocals.FolderStyle = Styles.Foldout;
                treeLocals.TreeNodeStyle = Styles.TreeNode;
                treeLocals.ActiveTreeNodeStyle = Styles.TreeNodeActive;
                treeRemotes.FolderStyle = Styles.Foldout;
                treeRemotes.TreeNodeStyle = Styles.TreeNode;
                treeRemotes.ActiveTreeNodeStyle = Styles.TreeNodeActive;
            }

            var treeHadFocus = treeLocals.SelectedNode != null;

            rect = treeLocals.Render(rect, scroll,
                node =>{ },
                node =>
                {
                    if (EditorUtility.DisplayDialog(ConfirmSwitchTitle, String.Format(ConfirmSwitchMessage, node.Name), ConfirmSwitchOK,
                            ConfirmSwitchCancel))
                    {
                        GitClient.SwitchBranch(node.Name)
                            .FinallyInUI((success, e) =>
                            {
                                if (success)
                                {
                                    Redraw();
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                        String.Format(Localization.SwitchBranchFailedDescription, node.Name),
                                    Localization.Ok);
                                }
                            }).Start();
                    }
                },
                node =>
                {
                    Debug.Log("Right Click");
                });

            if (treeHadFocus && treeLocals.SelectedNode == null)
                treeRemotes.Focus();
            else if (!treeHadFocus && treeLocals.SelectedNode != null)
                treeRemotes.Blur();

            if (treeLocals.RequiresRepaint)
                Redraw();

            treeHadFocus = treeRemotes.SelectedNode != null;

            rect.y += Styles.TreePadding;

            rect = treeRemotes.Render(rect, scroll,
                node => { },
                selectedNode =>
                {
                    var indexOfFirstSlash = selectedNode.Name.IndexOf('/');
                    var originName = selectedNode.Name.Substring(0, indexOfFirstSlash);
                    var branchName = selectedNode.Name.Substring(indexOfFirstSlash + 1);

                    if (Repository.LocalBranches.Any(localBranch => localBranch.Name == branchName))
                    {
                        EditorUtility.DisplayDialog(WarningCheckoutBranchExistsTitle,
                            String.Format(WarningCheckoutBranchExistsMessage, branchName),
                            WarningCheckoutBranchExistsOK);
                    }
                    else
                    {
                        var confirmCheckout = EditorUtility.DisplayDialog(ConfirmCheckoutBranchTitle,
                            String.Format(ConfirmCheckoutBranchMessage, selectedNode.Name, originName),
                            ConfirmCheckoutBranchOK,
                            ConfirmCheckoutBranchCancel);

                        if (confirmCheckout)
                        {
                            GitClient
                                .CreateBranch(branchName, selectedNode.Name)
                                .FinallyInUI((success, e) =>
                                {
                                    if (success)
                                    {
                                        Redraw();
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                            String.Format(Localization.SwitchBranchFailedDescription, selectedNode.Name),
                                            Localization.Ok);
                                    }
                                })
                                .Start();
                        }
                    }
                },
                node =>
                {
                    Debug.Log("Right Click");
                });

            if (treeHadFocus && treeRemotes.SelectedNode == null)
            {
                treeLocals.Focus();
            }
            else if (!treeHadFocus && treeRemotes.SelectedNode != null)
            {
                treeLocals.Blur();
            }

            if (treeRemotes.RequiresRepaint)
                Redraw();

            //Debug.LogFormat("reserving: {0} {1} {2}", rect.y - initialRect.y, rect.y, initialRect.y);
            GUILayout.Space(rect.y - initialRect.y);
        }

        private int CompareBranches(GitBranch a, GitBranch b)
        {
            //if (IsFavorite(a.Name))
            //{
            //    return -1;
            //}

            //if (IsFavorite(b.Name))
            //{
            //    return 1;
            //}

            if (a.Name.Equals("master"))
            {
                return -1;
            }

            if (b.Name.Equals("master"))
            {
                return 1;
            }

            return a.Name.CompareTo(b.Name);
        }

        //private bool IsFavorite(string branchName)
        //{
        //    return !String.IsNullOrEmpty(branchName) && favoritesList.Contains(branchName);
        //}

        //private void SetFavorite(TreeNode branch, bool favorite)
        //{
        //    if (string.IsNullOrEmpty(branch.Name))
        //    {
        //        return;
        //    }

        //    if (!favorite)
        //    {
        //        favorites.Remove(branch);
        //        Manager.LocalSettings.Set(FavoritesSetting, favorites.Select(x => x.Name).ToList());
        //    }
        //    else
        //    {
        //        favorites.Remove(branch);
        //        favorites.Add(branch);
        //        Manager.LocalSettings.Set(FavoritesSetting, favorites.Select(x => x.Name).ToList());
        //    }
        //}

        public override bool IsBusy
        {
            get { return false; }
        }

        private enum NodeType
        {
            Folder,
            LocalBranch,
            RemoteBranch
        }

        private enum BranchesMode
        {
            Default,
            Create
        }
    }
}
