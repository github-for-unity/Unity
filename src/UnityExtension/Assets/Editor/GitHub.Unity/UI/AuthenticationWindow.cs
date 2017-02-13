using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationWindow : EditorWindow, IView
    {
        private static Action callbackOnClose;

        [SerializeField] private AuthenticationView authView;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            Open();
        }

        public static void Open(Action onClose = null)
        {
            callbackOnClose = onClose;
            AuthenticationWindow authWindow = GetWindow<AuthenticationWindow>();
            authWindow.minSize = new Vector2(290, 290);
            authWindow.Show();
        }

        public void OnGUI()
        {
            authView.OnGUI();
        }

        public void Redraw()
        {
            Repaint();
        }

        public void Refresh()
        {
            authView.Refresh();
        }

        private void OnEnable()
        {
            Utility.UnregisterReadyCallback(CreateViews);
            Utility.RegisterReadyCallback(CreateViews);

            Utility.UnregisterReadyCallback(Refresh);
            Utility.RegisterReadyCallback(Refresh);
        }

        private void OnDestroy()
        {
            callbackOnClose.SafeInvoke();
        }

        private void CreateViews()
        {
            if (authView == null)
                authView = new AuthenticationView();
            authView.Show(this);
        }

        public Rect Position { get { return position; } }
    }
}
