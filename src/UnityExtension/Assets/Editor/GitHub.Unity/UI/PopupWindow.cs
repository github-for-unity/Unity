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
                    activePopupView = value;

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
            }
        }

        public static IView Open(PopupView popupView, Action<bool> onClose = null)
        {
            var popupWindow = GetWindow<PopupWindow>(true);
            if (onClose != null)
                popupWindow.OnClose += onClose;

            popupWindow.ActivePopupView = popupView;
            popupWindow.titleContent = new GUIContent(popupWindow.ActiveSubview.Title, Styles.SmallLogo);

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

            minSize = maxSize = ActiveSubview.Size;

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