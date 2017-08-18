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
            PublishView,
            AuthenticationView
        }

        [NonSerialized] private Subview activeView;

        [SerializeField] private PopupViewType activeViewType;
        [SerializeField] private AuthenticationView authenticationView;
        [SerializeField] private PublishView publishView;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            var popupWindow = Open(PopupViewType.AuthenticationView);
            popupWindow.InitializeWindow(EntryPoint.ApplicationManager);
        }

        public static PopupWindow Open(PopupViewType popupViewType, Action<bool> onClose = null)
        {
            var popupWindow = GetWindow<PopupWindow>(true);
            if (onClose != null)
            {
                popupWindow.OnClose += onClose;
            }

            popupWindow.ActiveViewType = popupViewType;
            popupWindow.titleContent = new GUIContent(popupWindow.ActiveView.Title, Styles.SmallLogo);

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
            Close();
            base.Finish(result);
        }

        private Subview ActiveView
        {
            get { return activeView; }
        }

        private PopupViewType ActiveViewType
        {
            get { return activeViewType; }
            set
            {
                if (activeViewType != value)
                {
                    activeViewType = value;

                    switch (activeViewType)
                    {
                        case PopupViewType.PublishView:
                            activeView = publishView;
                            break;

                        case PopupViewType.AuthenticationView:
                            activeView = authenticationView;
                            break;

                        default: throw new ArgumentOutOfRangeException("value", value, null);
                    }
                }
            }
        }
    }
}
