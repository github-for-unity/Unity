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

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);
            if (publishView == null)
                publishView = new PublishView();
            publishView.InitializeView(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();
       
            // Set window title
            titleContent = new GUIContent(Title, Styles.SmallLogo);
            publishView.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            publishView.OnDisable();
        }

        public override void OnUI()
        {
            publishView.OnGUI();
        }

        public override void Refresh()
        {
            publishView.Refresh();
        }

        public override void Finish(bool result)
        {
            Close();
            base.Finish(result);
        }

        public override bool IsBusy
        {
            get { return publishView.IsBusy; }
        }
    }
}
