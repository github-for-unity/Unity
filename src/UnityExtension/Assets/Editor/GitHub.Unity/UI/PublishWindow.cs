using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    public class PublishWindow : EditorWindow
    {
        private const string PublishTitle = "Publish to GitHub";

        static void Init()
        {
            PublishWindow window = (PublishWindow)EditorWindow.GetWindow(typeof(PublishWindow));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label(PublishTitle, EditorStyles.boldLabel);

            GUILayout.Label("hey");
        }
    }
}
