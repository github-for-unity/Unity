using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class PublishWindow : BaseWindow
    {
        private const string Title = "Publish";

        [SerializeField] private PublishView publishView;

        public static void Launch()
        {
            Open();
        }

        public static IView Open(Action<bool> onClose = null)
        {
            PublishWindow publishWindow = GetWindow<PublishWindow>(true);
            if (onClose != null)
                publishWindow.OnClose += onClose;
            publishWindow.minSize = publishWindow.maxSize = new Vector2(300, 250);
            publishWindow.Show();
            return publishWindow;
       }

        public override void OnUI()
        {
            if (publishView == null)
            {
                CreateViews();
            }
            publishView.OnGUI();
        }

        public override void Refresh()
        {
            publishView.Refresh();
        }

        public override void OnEnable()
        {
            // Set window title
            titleContent = new GUIContent(Title, Styles.SmallLogo);

            Utility.UnregisterReadyCallback(CreateViews);
            Utility.RegisterReadyCallback(CreateViews);

            Utility.UnregisterReadyCallback(ShowActiveView);
            Utility.RegisterReadyCallback(ShowActiveView);
        }

        private void CreateViews()
        {
            if (publishView == null)
                publishView = new PublishView();

            Initialize(EntryPoint.ApplicationManager);
            publishView.InitializeView(this);
        }

        private void ShowActiveView()
        {
            publishView.OnShow();
            Refresh();
        }

        public override void Finish(bool result)
        {
            Close();
            base.Finish(result);
        }
    }
}
