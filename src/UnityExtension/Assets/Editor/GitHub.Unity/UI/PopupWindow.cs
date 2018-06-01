using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class PopupWindow : BaseWindow
    {
        [NonSerialized] private IApiClient client;

        [SerializeField] private PopupViewType activeViewType;
        [SerializeField] private AuthenticationView authenticationView;
        [SerializeField] private LoadingView loadingView;
        [SerializeField] private PublishView publishView;
        [SerializeField] private bool shouldCloseOnFinish;

        public event Action<bool, object> OnClose;

        public static PopupWindow OpenWindow(PopupViewType popupViewType, object data, Action<bool, object> onClose)
        {
            var popupWindow = GetWindow<PopupWindow>(true);

            popupWindow.Open(popupViewType, data, onClose);

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

            titleContent = new GUIContent(ActiveView.Title, Styles.SmallLogo);
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

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            if (titleContent.image == null)
                titleContent = new GUIContent(ActiveView.Title, Styles.SmallLogo);
            ActiveView.OnDataUpdate();
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

        public override void Finish(bool result, object output)
        {
            OnClose.SafeInvoke(result, output);
            OnClose = null;

            if (shouldCloseOnFinish)
            {
                shouldCloseOnFinish = false;
                Close();
            }

            base.Finish(result, output);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            OnClose.SafeInvoke(false, null);
            OnClose = null;
        }

        private void Open(PopupViewType popupViewType, object data, Action<bool, object> onClose)
        {
            OnClose.SafeInvoke(false, null);
            OnClose = null;

            var viewNeedsAuthentication = popupViewType == PopupViewType.PublishView;
            if (viewNeedsAuthentication)
            {
                Client.GetCurrentUser(user =>
                {
                    OpenInternal(popupViewType, data, onClose);
                    shouldCloseOnFinish = true;

                },
                exception =>
                {
                    authenticationView.Initialize(exception);
                    OpenInternal(PopupViewType.AuthenticationView, data, (success, output) =>
                    {
                        if (success)
                        {
                            Open(popupViewType, data, onClose);
                        }
                    });
                    shouldCloseOnFinish = false;
                });
            }
            else
            {
                OpenInternal(popupViewType, data, onClose);
                shouldCloseOnFinish = true;
            }
        }

        private void OpenInternal(PopupViewType popupViewType, object data, Action<bool, object> onClose)
        {
            if (onClose != null)
            {
                OnClose += onClose;
            }

            Data = data;
            var fromView = ActiveView;
            ActiveViewType = popupViewType;
            SwitchView(fromView, ActiveView);
            Show();
        }

        private void SwitchView(Subview fromView, Subview toView)
        {
            GUI.FocusControl(null);

            if (fromView != null)
                fromView.OnDisable();
            toView.OnEnable();
            titleContent = new GUIContent(ActiveView.Title, Styles.SmallLogo);

            // this triggers a repaint
            Repaint();
        }

        public IApiClient Client
        {
            get
            {
                if (client == null)
                {
                    var repository = Environment.Repository;
                    UriString host;
                    if (repository != null && !string.IsNullOrEmpty(repository.CloneUrl))
                    {
                        host = repository.CloneUrl.ToRepositoryUrl();
                    }
                    else
                    {
                        host = UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri);
                    }

                    client = new ApiClient(host, Platform.Keychain, Manager.ProcessManager, TaskManager, Environment.NodeJsExecutablePath, Environment.OctorunScriptPath);
                }

                return client;
            }
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
            set
            {
                if (activeViewType != value)
                {
                    ActiveView.OnDisable();
                    activeViewType = value;
                }
            }
        }

        public override bool IsBusy
        {
            get { return ActiveView.IsBusy; }
        }
    }
}
