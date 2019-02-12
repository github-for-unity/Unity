using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    public class FileHistoryWindow : BaseWindow
    {
        [MenuItem("Assets/Git/History", false)]
        private static void GitFileHistory()
        {
            if (Selection.assetGUIDs != null)
            {
                var assetPath =
                    AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs.First())
                                 .ToNPath();

                var windowType = typeof(Window);
                var fileHistoryWindow = GetWindow<FileHistoryWindow>(windowType);
                fileHistoryWindow.InitializeWindow(EntryPoint.ApplicationManager);
                fileHistoryWindow.SetSelectedPath(assetPath);
                fileHistoryWindow.Show();
            }
        }

        [MenuItem("Assets/Git/History", true)]
        private static bool GitFileHistoryValidation()
        {
            return Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;
        }

        private const string Title = "File History";

        [NonSerialized] private bool firstOnGUI = true;
        [NonSerialized] private Texture selectedIcon;

        [SerializeField] private bool locked;
        [SerializeField] private FileHistoryView fileHistoryView = new FileHistoryView();
        [SerializeField] private UnityEngine.Object selectedObject;
        [SerializeField] private NPath selectedObjectAssetPath;

        public void SetSelectedPath(NPath assetPath)
        {
            NPath repositoryPath = NPath.Default;

            selectedObjectAssetPath = assetPath;
            selectedObject = null;

            if (selectedObjectAssetPath != NPath.Default)
            {
                selectedObject = AssetDatabase.LoadMainAssetAtPath(selectedObjectAssetPath.ToString());

                repositoryPath = Environment.GetRepositoryPath(assetPath);
            }

            LoadSelectedIcon();

            Repository.UpdateFileLog(repositoryPath)
                      .Start();
        }

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);

            fileHistoryView.InitializeView(this);
        }

        public override bool IsBusy
        {
            get { return false; }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            LoadSelectedIcon();

            if (fileHistoryView != null)
                fileHistoryView.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (fileHistoryView != null)
                fileHistoryView.OnDisable();
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();

            if (fileHistoryView != null)
                fileHistoryView.OnDataUpdate();
        }

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);

            DetachHandlers(oldRepository);
            AttachHandlers(Repository);

            if (HasRepository)
            {
                
            }
            else
            {
               
            }
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            if (fileHistoryView != null)
                fileHistoryView.OnSelectionChange();

            if (!locked)
            {
                selectedObject = Selection.activeObject;
                selectedObjectAssetPath = NPath.Default;
                if (selectedObject != null)
                {
                    selectedObjectAssetPath = AssetDatabase.GetAssetPath(selectedObject)
                                             .ToNPath();

                    SetSelectedPath(selectedObjectAssetPath);
                }
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            if (fileHistoryView != null)
                fileHistoryView.Refresh();
            Refresh(CacheType.GitFileLog);
            Redraw();
        }

        public override void OnUI()
        {
            base.OnUI();

            if (selectedObject != null)
            {
                GUILayout.BeginVertical(Styles.HeaderStyle);
                {
                    DoHeaderGUI();

                    fileHistoryView.OnGUI();
                }
                GUILayout.EndVertical();
            }
        }

        private void MaybeUpdateData()
        {
            if (firstOnGUI)
            {
                titleContent = new GUIContent(Title, Styles.SmallLogo);
            }
            firstOnGUI = false;
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
        }

        private void LoadSelectedIcon()
        {
            Texture nodeIcon = null;

            if (selectedObjectAssetPath != NPath.Default)
            {
                if (selectedObjectAssetPath.DirectoryExists())
                {
                    nodeIcon = Styles.FolderIcon;
                }
                else
                {
                    nodeIcon = UnityEditorInternal.InternalEditorUtility.GetIconForFile(selectedObjectAssetPath.ToString());
                }

                nodeIcon.hideFlags = HideFlags.HideAndDontSave;
            }

            selectedIcon = nodeIcon;
        }

        private void ShowButton(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            
            locked = GUI.Toggle(rect, locked, GUIContent.none, Styles.LockButtonStyle);

            if (!EditorGUI.EndChangeCheck())
                return;

            this.OnSelectionChange();
        }

        private void DoHeaderGUI()
        {
            GUILayout.BeginHorizontal(Styles.HeaderBoxStyle);
            {
                var iconWidth = 32;
                var iconHeight = 32;

                GUILayout.Label(selectedIcon, GUILayout.Height(iconWidth), GUILayout.Width(iconHeight));

                GUILayout.Label(selectedObjectAssetPath, Styles.FileHistoryLogTitleStyle);

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(16);

                    if (GUILayout.Button("Show in Project"))
                    {
                        EditorGUIUtility.PingObject(selectedObject);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }
}
