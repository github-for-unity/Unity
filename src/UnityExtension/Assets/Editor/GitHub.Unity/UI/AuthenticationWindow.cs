using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationWindow : BaseWindow
    {
        [SerializeField] private AuthenticationView authView;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            Open();
        }

        public static IView Open(Action<bool> onClose = null)
        {
            AuthenticationWindow authWindow = GetWindow<AuthenticationWindow>();
            if (onClose != null)
                authWindow.OnClose += onClose;
            authWindow.minSize = new Vector2(290, 290);
            authWindow.Show();
            return authWindow;
       }

        public override void OnGUI()
        {
            authView.OnGUI();
        }

        public override void Refresh()
        {
            authView.Refresh();
        }

        public override void OnEnable()
        {
            Utility.UnregisterReadyCallback(CreateViews);
            Utility.RegisterReadyCallback(CreateViews);

            Utility.UnregisterReadyCallback(ShowActiveView);
            Utility.RegisterReadyCallback(ShowActiveView);
        }

        private void CreateViews()
        {
            if (authView == null)
                authView = new AuthenticationView();
            authView.Initialize(this);
        }

        private void ShowActiveView()
        {
            authView.OnShow();
            Refresh();
        }

        public override void Finish(bool result)
        {
            Close();
            base.Finish(result);
        }
    }
}
