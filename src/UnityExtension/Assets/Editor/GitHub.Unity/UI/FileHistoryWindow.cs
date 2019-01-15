using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    public class FileHistoryWindow : BaseWindow
    {
        private const string Title = "File History";

        [NonSerialized] private bool firstOnGUI = true;

        [SerializeField] private bool locked;
        [SerializeField] private string assetPath;
        [SerializeField] private FileHistoryView fileHistoryView = new FileHistoryView();

        public static FileHistoryWindow OpenWindow(IApplicationManager applicationManager, string assetPath)
        {
            var fileHistoryWindow = CreateInstance<FileHistoryWindow>();
            fileHistoryWindow.InitializeWindow(applicationManager);

            fileHistoryWindow.Open(assetPath);
            fileHistoryWindow.Show();

            return fileHistoryWindow;
        }

        public void Open(string path)
        {
            assetPath = path;
            fileHistoryView.SetPath(path);
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

        public override void OnFocusChanged()
        {
            if (fileHistoryView != null)
                fileHistoryView.OnFocusChanged();
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
        }

        public override void Refresh()
        {
            base.Refresh();
            if (fileHistoryView != null)
                fileHistoryView.Refresh();
            Refresh(CacheType.GitLocks);
            Redraw();
        }

        public override void OnUI()
        {
            base.OnUI();

            fileHistoryView.OnGUI();
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

        private void ShowButton(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            
            locked = GUI.Toggle(rect, locked, GUIContent.none, Styles.LockButtonStyle);

            if (!EditorGUI.EndChangeCheck())
                return;

            this.OnSelectionChange();
        }
    }
}
