using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    public class PublishWindow : EditorWindow
    {
        private const string PublishTitle = "Publish this repository to GitHub";
        private string repoName = "";
        private string repoDescription = "";
        private bool togglePrivate = false;

        static void Init()
        {
            PublishWindow window = (PublishWindow)EditorWindow.GetWindow(typeof(PublishWindow));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label(PublishTitle, EditorStyles.boldLabel); 

            GUILayout.Space(5);

            repoName = EditorGUILayout.TextField("Name", repoName);
            repoDescription = EditorGUILayout.TextField("Description", repoDescription);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                togglePrivate = GUILayout.Toggle(togglePrivate, "Keep my code private");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Button("Create");
            }
            GUILayout.EndHorizontal();
        }
    }
}
