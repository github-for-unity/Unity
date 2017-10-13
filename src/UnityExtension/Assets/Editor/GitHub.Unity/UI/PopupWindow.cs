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

        [SerializeField] private bool shouldCloseOnFinish;
        [SerializeField] private PopupViewType activeViewType;
        [SerializeField] private AuthenticationView authenticationView;
        [SerializeField] private PublishView publishView;
        [SerializeField] private LoadingView loadingView;

        [NonSerialized] private IApiClient client;

        public event Action<bool> OnClose;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            OpenWindow(PopupViewType.AuthenticationView);
        }

        public static PopupWindow OpenWindow(PopupViewType popupViewType, Action<bool> onClose = null)
        {
            var popupWindow = GetWindow<PopupWindow>(true);

            popupWindow.Open(popupViewType, onClose);

            return popupWindow;
        }

        private void Open(PopupViewType popupViewType, Action<bool> onClose)
        {
            OnClose.SafeInvoke(false);
            OnClose = null;

            Logger.Trace("OpenView: {0}", popupViewType.ToString());

            var viewNeedsAuthentication = popupViewType == PopupViewType.PublishView;
            if (viewNeedsAuthentication)
            {
                Logger.Trace("Validating to open view");

                Client.ValidateCurrentUser(() => {

                    Logger.Trace("User validated opening view");

                    OpenInternal(popupViewType, onClose);
                    shouldCloseOnFinish = true;

                }, exception => {
                    Logger.Trace("User required validation opening AuthenticationView");

                    string message = null;
                    string username = null;

                    var usernameMismatchException = exception as TokenUsernameMismatchException;
                    if (usernameMismatchException != null)
                    {
                        message = "Your credentials need to be refreshed";
                        username = usernameMismatchException.CachedUsername;
                    }

                    var keychainEmptyException = exception as KeychainEmptyException;
                    if (keychainEmptyException != null)
                    {
                        message = "We need you to authenticate first";
                    }

                    if (usernameMismatchException == null && keychainEmptyException == null)
                    {
                        message = "There was an error validating your account";
                    }

                    OpenInternal(PopupViewType.AuthenticationView, completedAuthentication => {
                        authenticationView.ClearMessage();
                        authenticationView.ClearUsername();

                        if (completedAuthentication)
                        {
                            Logger.Trace("User completed validation opening view: {0}", popupViewType.ToString());

                            Open(popupViewType, onClose);
                        }
                    });

                    shouldCloseOnFinish = false;
                    authenticationView.SetMessage(message);
                    if (username != null)
                    {
                        authenticationView.SetUsername(username);
                    }
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

            ActiveViewType = popupViewType;
            titleContent = new GUIContent(ActiveView.Title, Styles.SmallLogo);
            OnEnable();
            Show();
            Refresh();
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

                    client = ApiClient.Create(host, Platform.Keychain);
                }

                return client;
            }
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
