using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Rackspace.Threading;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class PublishView : Subview
    {
        private const string Title = "Publish this repository to GitHub";
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
        [NonSerialized] private bool isLoading;
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

                    isLoading = true;

                    Client.LoadKeychain(hasKeys => {
                        if (!hasKeys)
                        {
                            return;
                        }

                        username = keychainConnections.First().Username;

                        Client.GetOrganizations(organizations => {

                            var organizationLogins = (organizations ?? Enumerable.Empty<Organization>())
                                .OrderBy(organization => organization.Login)
                                .Select(organization => organization.Login);

                            owners = new[] { username }.Union(organizationLogins).ToArray();
                        });
                    }).Finally(task => {
                        isLoading = false;
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

            EditorGUI.BeginDisabledGroup(isLoading || isBusy);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Owner");

                        selectedOwner = EditorGUILayout.Popup(0, owners);
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
                        repoName = EditorGUILayout.TextField(repoName);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Description");
                repoDescription = EditorGUILayout.TextField(repoDescription);
                GUILayout.Space(Styles.PublishViewSpacingHeight);

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        togglePrivate = GUILayout.Toggle(togglePrivate, "Create as a private repository");
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
                GUILayout.EndVertical();;

                GUILayout.Space(Styles.PublishViewSpacingHeight);

                if (error != null)
                    GUILayout.Label(error, Styles.ErrorLabel);

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(!IsFormValid);
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
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool IsFormValid
        {
            get { return !string.IsNullOrEmpty(repoName); }
        }
    }
}
