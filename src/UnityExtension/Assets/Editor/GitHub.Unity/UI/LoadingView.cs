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
            
        }

        public override bool IsBusy
        {
            get { return false; }
        }
    }
}
