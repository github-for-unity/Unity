using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class PopupWindow : BaseWindow
    {
        public enum PopupViewType
        {
            None,
            PublishView,
            AuthenticationView
        }

        [SerializeField] private PopupViewType activeViewType;

        [SerializeField] private AuthenticationView authenticationView;
        [SerializeField] private PublishView publishView;
        [SerializeField] private LoadingView loadingView;

        public event Action<bool> OnClose;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            Open(PopupViewType.AuthenticationView);
        }

        public static PopupWindow Open(PopupViewType popupViewType, Action<bool> onClose = null)
        {
            var popupWindow = GetWindow<PopupWindow>(true);

            popupWindow.OnClose.SafeInvoke(false);

            if (onClose != null)
            {
                popupWindow.OnClose += onClose;
            }

            popupWindow.ActiveViewType = popupViewType;
            popupWindow.titleContent = new GUIContent(popupWindow.ActiveView.Title, Styles.SmallLogo);

            popupWindow.InitializeWindow(EntryPoint.ApplicationManager);
            popupWindow.Show();

            return popupWindow;
        }

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);

            publishView = publishView ?? new PublishView();
            authenticationView = authenticationView ?? new AuthenticationView();
            loadingView = loadingView ?? new LoadingView();

            publishView.InitializeView(this);
            authenticationView.InitializeView(this);
            loadingView.InitializeView(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            minSize = maxSize = ActiveView.Size;
            ActiveView.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ActiveView.OnDisable();
        }

        public override void OnUI()
        {
            base.OnUI();
            ActiveView.OnGUI();
        }

        public override void Refresh()
        {
            base.Refresh();
            ActiveView.Refresh();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            ActiveView.OnSelectionChange();
        }

        public override void Finish(bool result)
        {
            OnClose.SafeInvoke(result);
            OnClose = null;
            Close();
            base.Finish(result);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            OnClose.SafeInvoke(false);
            OnClose = null;
        }

        public override bool IsBusy
        {
            get { return ActiveView.IsBusy; }
        }

        private Subview ActiveView
        {
            get
            {
                switch (activeViewType)
                {
                    case PopupViewType.PublishView:
                        return publishView;
                    case PopupViewType.AuthenticationView:
                        return authenticationView;
                    default:
                        return loadingView;
                }
            }
        }

        private PopupViewType ActiveViewType
        {
            get { return activeViewType; }
            set { activeViewType = value; }
        }
    }
}
