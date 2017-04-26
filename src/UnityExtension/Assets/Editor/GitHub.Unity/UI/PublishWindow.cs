using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    public class PublishWindow : EditorWindow
    {
        private const string PublishTitle = "Publish this repository to GitHub";
        private string repoName = "";
        private string repoDescription = "";
        private int selectedOrg = 0;
        private bool togglePrivate = false;

        // TODO: Replace me since this is just to test rendering errors
        private bool error = true;

        private string[] orgs = { "donokuda", "github", "donokudallc", "another-org" };

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
            selectedOrg = EditorGUILayout.Popup("Organization", 0, orgs);

            GUILayout.Space(5);

            if (error)
                GUILayout.Label("There was an error", Styles.ErrorLabel);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create"))
                {
                    Debug.Log("CREATING A REPO HAPPENS HERE!");
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
