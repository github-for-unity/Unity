using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class PopupWindow : BaseWindow
    {
        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            Open(new AuthenticationView(), "Authentication");
        }

        [SerializeField]
        private Subview subview;
        private string titleValue;

        public static IView Open(Subview popupSubview, string popupTitle, Action<bool> onClose = null)
        {
            PopupWindow popupWindow = GetWindow<PopupWindow>(true, popupTitle);
            if (onClose != null)
                popupWindow.OnClose += onClose;

            popupWindow.titleValue = popupTitle;
            popupWindow.subview = popupSubview;
            popupWindow.minSize = popupWindow.maxSize = new Vector2(290, 290);

            popupWindow.Show();
            return popupWindow;
        }

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);

            if (subview != null)
            {
                subview.InitializeView(this);
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (titleValue != null)
            {
                titleContent = new GUIContent(titleValue, Styles.SmallLogo);
            }

            if (subview != null)
            {
                subview.OnEnable();
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (subview != null)
            {
                subview.OnDisable();
            }
        }

        public override void OnUI()
        {
            base.OnUI();
            if (subview != null)
            {
                subview.OnGUI();
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            subview.Refresh();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            subview.OnSelectionChange();
        }

        public override void Finish(bool result)
        {
            Close();
            base.Finish(result);
        }
    }
}