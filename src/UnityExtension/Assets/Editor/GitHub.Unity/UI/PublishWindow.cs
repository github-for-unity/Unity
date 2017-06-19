using System;
using System.Linq;
using Octokit;
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
        private string error;

        private string username;
        private string[] owners = { };
        private IApiClient client;

        public IApiClient Client
        {
            get
            {
                if (client == null)
                {
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

                    client = ApiClient.Create(host, Platform.Keychain, new AppConfiguration());
                }

                return client;
            }
        }

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

                var keychainConnections = Platform.Keychain.Connections;
                if (keychainConnections.Any())
                {
                    username = keychainConnections[0].Username;
                    owners = new[] { username };
                }
                else
                {
                    Logger.Warning("No Keychain connections to use");
                }

                Logger.Trace("Create Client");

                Logger.Trace("GetOrganizations");

                Guard.NotNull(this, Platform, "Platform");
                Guard.NotNull(Platform, Platform.Keychain, "Platform.Keychain");

                Client.GetOrganizations(organizations => {
                    if (organizations == null)
                    {
                        Logger.Warning("Organizations is null");
                        return;
                    }

                    Logger.Trace("Loaded {0} organizations", organizations.Count);

                    var organizationLogins = organizations
                        .OrderBy(organization => organization.Login)
                        .Select(organization => organization.Login);

                    owners = owners.Union(organizationLogins).ToArray();
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

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Owner");
                GUILayout.Label("Repository Name");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                selectedOrg = EditorGUILayout.Popup(0, owners);
                GUILayout.Label("/");
                repoName = EditorGUILayout.TextField(repoName);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Description");
            repoDescription = EditorGUILayout.TextField(repoDescription);

            togglePrivate = GUILayout.Toggle(togglePrivate, "Create as a private repository");
            var repoPrivacyExplanation = togglePrivate ? "Anyone can see this repository. You choose who can commit" : "You choose who can see and commit to this repository";
            GUILayout.Label(repoPrivacyExplanation);

            GUILayout.Space(5);

            if (error != null)
                GUILayout.Label(error, Styles.ErrorLabel);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                GUI.enabled = !string.IsNullOrEmpty(repoName);
                if (GUILayout.Button("Create"))
                {
                    Client.CreateRepository(new NewRepository(repoName) {
                        Private = togglePrivate,
                    }, (repository, ex) => {

                        if (ex != null)
                        {
                            error = ex.Message;
                            return;
                        }

                        if (repository == null)
                        {
                            Logger.Warning("Returned Repository is null");
                            return;
                        }

                        GitClient.RemoteAdd("origin", repository.CloneUrl)
                                 .Then(GitClient.Push("origin", Repository.CurrentBranch))
                                 .ThenInUI(Close)
                                 .Start();
                    }, owners[selectedOrg] == username ? null : owners[selectedOrg]);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }
    }
}
