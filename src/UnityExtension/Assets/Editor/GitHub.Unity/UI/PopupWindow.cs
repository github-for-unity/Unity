using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class PopupWindow : BaseWindow
    {
        public enum PopupView
        {
            PublishView,
            AuthenticationView
        }

        public static string Title(PopupView popupView)
        {
            switch (popupView)
            {
                case PopupView.PublishView:
                    return "Publish";

                case PopupView.AuthenticationView:
                    return "Authenticate";

                default:
                    throw new ArgumentOutOfRangeException("popupView", popupView, null);
            }
        }

        public static Vector2 PopupSize(PopupView popupView)
        {
            switch (popupView)
            {
                case PopupView.PublishView:
                    return new Vector2(300, 250);

                case PopupView.AuthenticationView:
                    return new Vector2(290, 290);

                default:
                    throw new ArgumentOutOfRangeException("popupView", popupView, null);
            }
        }

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            var popupWindow = (PopupWindow) Open(PopupView.AuthenticationView);
            popupWindow.Initialize(EntryPoint.ApplicationManager);
        }

        [SerializeField] private PopupView activePopupView;
        [SerializeField] private AuthenticationView authenticationView;
        [SerializeField] private PublishView publishView;

        [NonSerialized] private Subview activeSubview;

        public Subview ActiveSubview
        {
            get
            {
                if (activeSubview == null)
                {
                    switch (activePopupView)
                    {
                        case PopupView.PublishView:
                            activeSubview = publishView;
                            break;

                        case PopupView.AuthenticationView:
                            activeSubview = authenticationView;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("selectedPopupView", activePopupView, null);
                    }
                }

                return activeSubview;
            }
        }

        public PopupView ActivePopupView
        {
            get { return activePopupView; }
            set
            {
                if (activePopupView != value)
                {
                    activeSubview = null;
                    activePopupView = value;
                }
            }
        }

        public static IView Open(PopupView popupView, Action<bool> onClose = null)
        {
            var popupWindow = GetWindow<PopupWindow>(true);
            if (onClose != null)
                popupWindow.OnClose += onClose;

            popupWindow.ActivePopupView = popupView;
            popupWindow.titleContent = new GUIContent(Title(popupView), Styles.SmallLogo);

            popupWindow.Show();
            return popupWindow;
        }

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);

            publishView = publishView ?? new PublishView();
            authenticationView = authenticationView ?? new AuthenticationView();

            publishView.InitializeView(this);
            authenticationView.InitializeView(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            minSize = maxSize = PopupSize(activePopupView);

            ActiveSubview.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ActiveSubview.OnDisable();
        }

        public override void OnUI()
        {
            base.OnUI();
            ActiveSubview.OnGUI();
        }

        public override void Refresh()
        {
            base.Refresh();
            ActiveSubview.Refresh();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            ActiveSubview.OnSelectionChange();
        }

        public override void Finish(bool result)
        {
            Close();
            base.Finish(result);
        }
    }
}