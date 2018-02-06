using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class LoadingView : Subview
    {
        private static readonly Vector2 MinViewSize = new Vector2(300, 250);

        private const string WindowTitle = "Loading...";

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = MinViewSize;
        }

        public override void OnGUI()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(WindowTitle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
        }

        public override bool IsBusy
        {
            get { return false; }
        }
    }
}
