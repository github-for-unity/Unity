using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationWindow : BaseWindow
    {
        private const string Title = "Authentication";

        [SerializeField] private AuthenticationView authView;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            Open();
        }

        public static IView Open(Action<bool> onClose = null)
        {
            AuthenticationWindow authWindow = GetWindow<AuthenticationWindow>(true);
            if (onClose != null)
                authWindow.OnClose += onClose;
            authWindow.minSize = authWindow.maxSize = new Vector2(290, 290);
            authWindow.Show();
            return authWindow;
       }

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);
            if (authView == null)
                authView = new AuthenticationView();
            authView.InitializeView(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            // Set window title
            titleContent = new GUIContent(Title, Styles.SmallLogo);
            authView.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            authView.OnDisable();
        }

        public override void OnUI()
        {
            base.OnUI();
            authView.OnGUI();
        }

        public override void Refresh()
        {
            base.Refresh();
            authView.Refresh();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            authView.OnSelectionChange();
        }

        public override void Finish(bool result)
        {
            Close();
            base.Finish(result);
        }

        public override bool IsBusy
        {
            get { return authView.IsBusy; }
        }
    }
}
