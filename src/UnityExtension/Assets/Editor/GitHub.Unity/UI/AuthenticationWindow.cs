using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationWindow : EditorWindow, IView
    {
        [SerializeField] private AuthenticationView authView;

        [MenuItem("GitHub/Authenticate")]
        public static void Launch()
        {
            GetWindow<AuthenticationWindow>().Show();
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

        private void CreateViews()
        {
            if (authView == null)
                authView = new AuthenticationView();
            authView.Show(this);
        }

        public Rect Position { get { return position; } }
    }
}
