using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class PublishWindow : BaseWindow
    {
        private const string Title = "Publish this repository to GitHub";
        private string repoName = "";
        private string repoDescription = "";
        private int selectedOrg = 0;
        private bool togglePrivate = false;

        // TODO: Replace me since this is just to test rendering errors
        private bool error = true;

        private string[] orgs = { "donokuda", "github", "donokudallc", "another-org" };
        private IApiClient client;

        public static IView Open(Action<bool> onClose = null)
        {
            var publishWindow = GetWindow<PublishWindow>();

            if (onClose != null)
                publishWindow.OnClose += onClose;

            publishWindow.minSize = new Vector2(300, 200);
            publishWindow.Show();

            return publishWindow;
        }

        public override void OnEnable()
        {
            // Set window title
            titleContent = new GUIContent(Title, Styles.SmallLogo);

            Utility.UnregisterReadyCallback(PopulateView);
            Utility.RegisterReadyCallback(PopulateView);
        }

        private void PopulateView()
        {
            try
            {
                Initialize(EntryPoint.ApplicationManager);

                Logger.Trace("Create Client");

                var repository = Environment.Repository;
                UriString host;
                if (repository != null && !string.IsNullOrEmpty(repository.CloneUrl))
                {
                    host = repository.CloneUrl.ToRepositoryUrl();
                }
                else
                {
                    host = UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri);
                }

                Logger.Trace("GetOrganizations");

                Guard.NotNull(this, Platform, "Platform");
                Guard.NotNull(Platform, Platform.Keychain, "Platform.Keychain");

                client = ApiClient.Create(host, Platform.Keychain, new AppConfiguration());
                client.GetOrganizations(organizations => {
                    if (organizations == null)
                    {
                        Logger.Warning("Organizations is null");
                        return;
                    }

                    foreach (var organization in organizations)
                    {
                        Logger.Trace("Organization: {0}", organization.Name);
                    }
                });

            }
            catch (Exception e)
            {
                Logger.Error(e, "Error PopulateView & GetOrganizations");
                throw;
            }
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal(Styles.AuthHeaderBoxStyle);
            {
                GUILayout.BeginVertical(GUILayout.Width(16));
                {
                    GUILayout.Space(9);
                    GUILayout.Label(Styles.BigLogo, GUILayout.Height(20), GUILayout.Width(20));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(11);
                    GUILayout.Label(Title, EditorStyles.boldLabel); 
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            repoName = EditorGUILayout.TextField("Name", repoName);
            repoDescription = EditorGUILayout.TextField("Description", repoDescription);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                togglePrivate = GUILayout.Toggle(togglePrivate, "Keep my code private");
            }
            GUILayout.EndHorizontal();
            selectedOrg = EditorGUILayout.Popup("Owner", 0, orgs);

            GUILayout.Space(5);

            if (error)
                GUILayout.Label("There was an error", Styles.ErrorLabel);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create"))
                {
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
