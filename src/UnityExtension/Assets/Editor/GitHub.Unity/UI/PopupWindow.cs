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
            AuthenticationView,
        }

        [NonSerialized] private IApiClient client;

        [SerializeField] private PopupViewType activeViewType;
        [SerializeField] private AuthenticationView authenticationView;
        [SerializeField] private LoadingView loadingView;
        [SerializeField] private PublishView publishView;
        [SerializeField] private bool shouldCloseOnFinish;

        public event Action<bool> OnClose;

        public static PopupWindow OpenWindow(PopupViewType popupViewType, Action<bool> onClose = null)
        {
            var popupWindow = GetWindow<PopupWindow>(true);

            popupWindow.Open(popupViewType, onClose);

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

        public override void Finish(bool result)
        {
            OnClose.SafeInvoke(result);
            OnClose = null;

            if (shouldCloseOnFinish)
            {
                shouldCloseOnFinish = false;
                Close();
            }

            base.Finish(result);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            OnClose.SafeInvoke(false);
            OnClose = null;
        }

        private void Open(PopupViewType popupViewType, Action<bool> onClose)
        {
            OnClose.SafeInvoke(false);
            OnClose = null;

            //Logger.Trace("OpenView: {0}", popupViewType.ToString());

            var viewNeedsAuthentication = popupViewType == PopupViewType.PublishView;
            if (viewNeedsAuthentication)
            {
                //Logger.Trace("Validating to open view");

                Client.GetCurrentUser(user => {

                    //Logger.Trace("User validated opening view");

                    OpenInternal(popupViewType, onClose);
                    shouldCloseOnFinish = true;

                }, exception => {
                    //Logger.Trace("User required validation opening AuthenticationView");
                    authenticationView.Initialize(exception);
                    OpenInternal(PopupViewType.AuthenticationView, completedAuthentication => {
                        if (completedAuthentication)
                        {
                            //Logger.Trace("User completed validation opening view: {0}", popupViewType.ToString());

                            Open(popupViewType, onClose);
                        }
                    });

                    shouldCloseOnFinish = false;
                });
            }
            else
            {
                OpenInternal(popupViewType, onClose);
                shouldCloseOnFinish = true;
            }
        }

        private void OpenInternal(PopupViewType popupViewType, Action<bool> onClose)
        {
            if (onClose != null)
            {
                OnClose += onClose;
            }

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
