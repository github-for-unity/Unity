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
        private const string DeleteBranchContextMenuLabel = "Delete";
        private const string SwitchBranchContextMenuLabel = "Switch";
        private const string CheckoutBranchContextMenuLabel = "Checkout";

        [NonSerialized] private int listID = -1;
        [NonSerialized] private BranchesMode targetMode;

        [SerializeField] private BranchesTree treeLocals = new BranchesTree { Title = LocalTitle };
        [SerializeField] private BranchesTree treeRemotes = new BranchesTree { Title = RemoteTitle, IsRemote = true };
        [SerializeField] private BranchesMode mode = BranchesMode.Default;
        [SerializeField] private string newBranchName;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private bool disableDelete;
        [SerializeField] private bool disableCreate;

        [SerializeField] private CacheUpdateEvent lastLocalAndRemoteBranchListChangedEvent;
        [NonSerialized] private bool localAndRemoteBranchListHasUpdate;

        [SerializeField] private CacheUpdateEvent lastCurrentBranchAndRemoteChange;
        [NonSerialized] private bool currentBranchAndRemoteChangeHasUpdate;

        [SerializeField] private GitBranch currentBranch = GitBranch.Default;
        [SerializeField] private GitRemote currentRemote = GitRemote.Default;

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

            var hasFocus = HasFocus;
            if (treeLocals != null)
            {
                treeLocals.ViewHasFocus = hasFocus;
                treeLocals.UpdateIcons(Styles.ActiveBranchIcon, Styles.BranchIcon, Styles.FolderIcon, Styles.GlobeIcon);
            }

            if (treeRemotes != null)
            {
                treeRemotes.ViewHasFocus = hasFocus;
                treeRemotes.UpdateIcons(Styles.ActiveBranchIcon, Styles.BranchIcon, Styles.FolderIcon, Styles.GlobeIcon);
            }

            AttachHandlers(Repository);
            ValidateCachedData(Repository);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void Refresh()
        {
            base.Refresh();
            Repository.Refresh(CacheType.Branches);
            Repository.Refresh(CacheType.RepositoryInfo);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            Redraw();
        }

        public override void OnFocusChanged()
        {
            base.OnFocusChanged();
            if(treeLocals.ViewHasFocus != HasFocus || treeRemotes.ViewHasFocus != HasFocus)
            { 
                treeLocals.ViewHasFocus = HasFocus;
                treeRemotes.ViewHasFocus = HasFocus;
                Redraw();
            }
        }

        private void RepositoryOnCurrentBranchAndRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentBranchAndRemoteChange.Equals(cacheUpdateEvent))
            {
                lastCurrentBranchAndRemoteChange = cacheUpdateEvent;
                currentBranchAndRemoteChangeHasUpdate = true;
                Redraw();
            }
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
            if (currentBranchAndRemoteChangeHasUpdate)
            {
                currentBranch = Repository.CurrentBranch ?? GitBranch.Default;
                currentRemote = Repository.CurrentRemote ?? GitRemote.Default;
            }

            if (localAndRemoteBranchListHasUpdate)
            {

                localBranches = Repository.LocalBranches.ToList();
                remoteBranches = Repository.RemoteBranches.ToList();

            }

            if (currentBranchAndRemoteChangeHasUpdate || localAndRemoteBranchListHasUpdate)
            {
                currentBranchAndRemoteChangeHasUpdate = false;
                localAndRemoteBranchListHasUpdate = false;

                BuildTree();
            }

            disableDelete = treeLocals.SelectedNode == null || treeLocals.SelectedNode.IsFolder || treeLocals.SelectedNode.IsActive;
            disableCreate = treeLocals.SelectedNode == null || treeLocals.SelectedNode.IsFolder || treeLocals.SelectedNode.Level == 0;
        }

        public override void OnGUI()
        {
            Render();
        }

        private void AttachHandlers(IRepository repository)
        {
            repository.LocalAndRemoteBranchListChanged += RepositoryOnLocalAndRemoteBranchListChanged;
            repository.CurrentBranchAndRemoteChanged += RepositoryOnCurrentBranchAndRemoteChanged;
        }

        private void DetachHandlers(IRepository repository)
        {
            repository.LocalAndRemoteBranchListChanged -= RepositoryOnLocalAndRemoteBranchListChanged;
            repository.CurrentBranchAndRemoteChanged -= RepositoryOnCurrentBranchAndRemoteChanged;
        }

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.Branches, lastLocalAndRemoteBranchListChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.RepositoryInfo, lastCurrentBranchAndRemoteChange);
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

            if (Event.current.type == EventType.Repaint)
            {
                // Effectuating mode switch
                if (mode != targetMode)
                {
                    mode = targetMode;
                    Redraw();
                }
            }
        }

        private void BuildTree()
        {
            localBranches.Sort(CompareBranches);
            remoteBranches.Sort(CompareBranches);

            treeLocals.Load(localBranches.Select(branch => new GitBranchTreeData(branch, currentBranch != GitBranch.Default && currentBranch.Name == branch.Name)));
            treeRemotes.Load(remoteBranches.Select(branch => new GitBranchTreeData(branch, false)));
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
                        DeleteLocalBranch(treeLocals.SelectedNode.Path);
                    }
                }
                EditorGUI.EndDisabledGroup();

                // Create button
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(disableCreate);
                {
                    if (GUILayout.Button(CreateBranchButton, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        targetMode = BranchesMode.Create;
                    }
                }
                EditorGUI.EndDisabledGroup();
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
                        Repository.CreateBranch(newBranchName, treeLocals.SelectedNode.Path)
                            .FinallyInUI((success, e) =>
                            {
                                if (success)
                                {
                                    TaskManager.Run(UsageTracker.IncrementBranchesViewButtonCreateBranch);
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
            var treeRenderRect = new Rect(0f, 0f, 0f, 0f);
            if (treeLocals != null && treeRemotes != null)
            {
                treeLocals.FolderStyle = Styles.Foldout;
                treeLocals.TreeNodeStyle = Styles.TreeNode;
                treeLocals.ActiveTreeNodeStyle = Styles.ActiveTreeNode;
                treeLocals.FocusedTreeNodeStyle = Styles.FocusedTreeNode;
                treeLocals.FocusedActiveTreeNodeStyle = Styles.FocusedActiveTreeNode;

                treeRemotes.FolderStyle = Styles.Foldout;
                treeRemotes.TreeNodeStyle = Styles.TreeNode;
                treeRemotes.ActiveTreeNodeStyle = Styles.ActiveTreeNode;
                treeRemotes.FocusedTreeNodeStyle = Styles.FocusedTreeNode;
                treeRemotes.FocusedActiveTreeNodeStyle = Styles.FocusedActiveTreeNode;

                var treeHadFocus = treeLocals.SelectedNode != null;

                treeRenderRect = treeLocals.Render(rect, scroll,
                    node => { },
                    node => {
                        if (node.IsFolder)
                            return;

                        if (node.IsActive)
                            return;

                        SwitchBranch(node.Path);
                    },
                    node => {
                        if (node.IsFolder)
                            return;

                        var menu = CreateContextMenuForLocalBranchNode(node);
                        menu.ShowAsContext();
                    });

                if (treeHadFocus && treeLocals.SelectedNode == null)
                    treeRemotes.Focus();
                else if (!treeHadFocus && treeLocals.SelectedNode != null)
                    treeRemotes.Blur();

                if (treeLocals.RequiresRepaint)
                    Redraw();

                treeHadFocus = treeRemotes.SelectedNode != null;

                treeRenderRect.y += Styles.TreePadding;

                var treeRemoteDisplayRect = new Rect(rect.x, treeRenderRect.y, rect.width, rect.height);
                treeRenderRect = treeRemotes.Render(treeRemoteDisplayRect, scroll, 
                    node => { }, 
                    node => {
                        if (node.IsFolder)
                            return;

                        CheckoutRemoteBranch(node.Path);
                    },
                    node => {
                        if (node.IsFolder)
                            return;

                        var menu = CreateContextMenuForRemoteBranchNode(node);
                        menu.ShowAsContext();
                    });

                if (treeHadFocus && treeRemotes.SelectedNode == null)
                    treeLocals.Focus();
                else if (!treeHadFocus && treeRemotes.SelectedNode != null)
                    treeLocals.Blur();

                if (treeRemotes.RequiresRepaint)
                    Redraw();
            }

            GUILayout.Space(treeRenderRect.y - rect.y);
        }

        private GenericMenu CreateContextMenuForLocalBranchNode(TreeNode node)
        {
            var genericMenu = new GenericMenu();

            var deleteGuiContent = new GUIContent(DeleteBranchContextMenuLabel);
            var switchGuiContent = new GUIContent(SwitchBranchContextMenuLabel);

            if (node.IsActive)
            {
                genericMenu.AddDisabledItem(deleteGuiContent);
                genericMenu.AddDisabledItem(switchGuiContent);
            }
            else
            {
                genericMenu.AddItem(deleteGuiContent, false, () => {
                    DeleteLocalBranch(node.Path);
                });

                genericMenu.AddItem(switchGuiContent, false, () => {
                    SwitchBranch(node.Path);
                });
            }

            return genericMenu;
        }

        private GenericMenu CreateContextMenuForRemoteBranchNode(TreeNode node)
        {
            var genericMenu = new GenericMenu();

            var checkoutGuiContent = new GUIContent(CheckoutBranchContextMenuLabel);
            
            genericMenu.AddItem(checkoutGuiContent, false, () => {
                CheckoutRemoteBranch(node.Path);
            });
            
            return genericMenu;
        }

        private void CheckoutRemoteBranch(string branch)
        {
            var indexOfFirstSlash = branch.IndexOf('/');
            var originName = branch.Substring(0, indexOfFirstSlash);
            var branchName = branch.Substring(indexOfFirstSlash + 1);

            if (Repository.LocalBranches.Any(localBranch => localBranch.Name == branchName))
            {
                EditorUtility.DisplayDialog(WarningCheckoutBranchExistsTitle,
                    String.Format(WarningCheckoutBranchExistsMessage, branchName), WarningCheckoutBranchExistsOK);
            }
            else
            {
                var confirmCheckout = EditorUtility.DisplayDialog(ConfirmCheckoutBranchTitle,
                    String.Format(ConfirmCheckoutBranchMessage, branch, originName), ConfirmCheckoutBranchOK,
                    ConfirmCheckoutBranchCancel);

                if (confirmCheckout)
                {
                    Repository.CreateBranch(branchName, branch)
                        .FinallyInUI((success, e) =>
                        {
                            if (success)
                            {
                                TaskManager.Run(UsageTracker.IncrementBranchesViewButtonCheckoutRemoteBranch);
                                Redraw();
                            }
                            else
                            {
                                EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                    String.Format(Localization.SwitchBranchFailedDescription, branch), Localization.Ok);
                            }
                        }).Start();
                }
            }
        }

        private void SwitchBranch(string branch)
        {
            if (EditorUtility.DisplayDialog(ConfirmSwitchTitle, String.Format(ConfirmSwitchMessage, branch), ConfirmSwitchOK,
                ConfirmSwitchCancel))
            {
                Repository.SwitchBranch(branch)
                    .FinallyInUI((success, e) =>
                    {
                        if (success)
                        {
                            TaskManager.Run(UsageTracker.IncrementBranchesViewButtonCheckoutLocalBranch);
                            Redraw();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(Localization.SwitchBranchTitle,
                                String.Format(Localization.SwitchBranchFailedDescription, branch), Localization.Ok);
                        }
                    }).Start();
            }
        }

        private void DeleteLocalBranch(string branch)
        {
            var dialogMessage = string.Format(DeleteBranchMessageFormatString, branch);
            if (EditorUtility.DisplayDialog(DeleteBranchTitle, dialogMessage, DeleteBranchButton, CancelButtonLabel))
            {
                Repository.DeleteBranch(branch, true)
                    .Then(UsageTracker.IncrementBranchesViewButtonDeleteBranch)
                    .Start();
            }
        }

        private int CompareBranches(GitBranch a, GitBranch b)
        {
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
