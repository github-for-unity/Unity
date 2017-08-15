using System;
using System.Linq;
using Octokit;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class PublishView : Subview
    {
        private static readonly Vector2 PublishViewSize = new Vector2(300, 250);

        private const string WindowTitle = "Publish";
        private const string Header = "Publish this repository to GitHub";
        private const string PrivateRepoMessage = "You choose who can see and commit to this repository";
        private const string PublicRepoMessage = "Anyone can see this repository. You choose who can commit";
        private const string PublishViewCreateButton = "Create";

        [SerializeField] private string username;
        [SerializeField] private string[] owners = { };
        [SerializeField] private int selectedOwner;
        [SerializeField] private string repoName = String.Empty;
        [SerializeField] private string repoDescription = "";
        [SerializeField] private bool togglePrivate;

        [NonSerialized] private IApiClient client;
        [NonSerialized] private bool isBusy;
        [NonSerialized] private string error;

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

                    client = ApiClient.Create(host, Platform.Keychain);
                }

                return client;
            }
        }

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            PopulateView();
        }

        private void PopulateView()
        {
            try
            {
                var keychainConnections = Platform.Keychain.Connections;
                if (keychainConnections.Any())
                {
                    Logger.Trace("GetCurrentUser");

                    Client.GetCurrentUser(user => {
                        if (user == null)
                        {
                            Logger.Warning("Unable to get current user");
                            return;
                        }

                        owners = new[] { user.Login };
                        username = user.Login;

                        Logger.Trace("GetOrganizations");

                        Client.GetOrganizations(organizations =>
                        {
                            if (organizations == null)
                            {
                                Logger.Warning("Unable to get list of organizations");
                                return;
                            }

                            Logger.Trace("Loaded {0} organizations", organizations.Count);

                            var organizationLogins = organizations
                                .OrderBy(organization => organization.Login)
                                .Select(organization => organization.Login);

                            owners = owners.Union(organizationLogins).ToArray();
                        });
                    });
                }
                else
                {
                    Logger.Warning("No Keychain connections to use");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error PopulateView & GetOrganizations");
                throw;
            }
        }

        public override void OnGUI()
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

            GUILayout.Space(Styles.PublishViewSpacingHeight);

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Owner");

                    GUI.enabled = !isBusy;
                    selectedOwner = EditorGUILayout.Popup(0, owners);
                    GUI.enabled = true;
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(8));
                {
                    GUILayout.Space(20);
                    GUILayout.Label("/");
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Repository Name");
                    GUI.enabled = !isBusy;
                    repoName = EditorGUILayout.TextField(repoName);
                    GUI.enabled = true;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Description");
            GUI.enabled = !isBusy;
            repoDescription = EditorGUILayout.TextField(repoDescription);
            GUI.enabled = true;
            GUILayout.Space(Styles.PublishViewSpacingHeight);

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = !isBusy;
                    togglePrivate = GUILayout.Toggle(togglePrivate, "Create as a private repository");
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(Styles.PublishViewSpacingHeight);
                    var repoPrivacyExplanation = togglePrivate ? PrivateRepoMessage : PublicRepoMessage;
                    GUILayout.Label(repoPrivacyExplanation, Styles.LongMessageStyle);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();


            GUILayout.Space(Styles.PublishViewSpacingHeight);

            if (error != null)
                GUILayout.Label(error, Styles.ErrorLabel);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUI.enabled = !string.IsNullOrEmpty(repoName) && !isBusy;
                if (GUILayout.Button(PublishViewCreateButton))
                {
                    isBusy = true;

                    var organization = owners[selectedOwner] == username ? null : owners[selectedOwner];

                    Client.CreateRepository(new NewRepository(repoName)
                    {
                        Private = togglePrivate,
                    }, (repository, ex) =>
                    {
                        Logger.Trace("Create Repository Callback");

                        if (ex != null)
                        {
                            error = ex.Message;
                            isBusy = false;
                            return;
                        }

                        if (repository == null)
                        {
                            Logger.Warning("Returned Repository is null");
                            isBusy = false;
                            return;
                        }

                        GitClient.RemoteAdd("origin", repository.CloneUrl)
                                 .Then(GitClient.Push("origin", Repository.CurrentBranch.Value.Name))
                                 .ThenInUI(Parent.Finish)
                                 .Start();
                    }, organization);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        public override string Title
        {
            get { return WindowTitle; }
        }

        public override Vector2 Size
        {
            get { return PublishViewSize; }
        }
    }
}
