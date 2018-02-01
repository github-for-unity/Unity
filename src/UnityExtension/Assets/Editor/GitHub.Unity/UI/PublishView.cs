using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class PublishView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(400, 350);

        private const string WindowTitle = "Publish";
        private const string PrivateRepoMessage = "You choose who can see and commit to this repository";
        private const string PublicRepoMessage = "Anyone can see this repository. You choose who can commit";
        private const string PublishViewCreateButton = "Publish";
        private const string OwnersDefaultText = "Select a user or org";
        private const string SelectedOwnerLabel = "Owner";
        private const string RepositoryNameLabel = "Repository Name";
        private const string DescriptionLabel = "Description";
        private const string CreatePrivateRepositoryLabel = "Make repository private";
        private const string PublishLimitPrivateRepositoriesError = "You are currently at your limit of private repositories";
        private const string PublishToGithubLabel = "Publish to GitHub";

        [SerializeField] private string username;
        [SerializeField] private string[] owners = { OwnersDefaultText };
        [SerializeField] private string[] publishOwners;
        [SerializeField] private int selectedOwner;
        [SerializeField] private string repoName = String.Empty;
        [SerializeField] private string repoDescription = "";
        [SerializeField] private bool togglePrivate;

        [NonSerialized] private IApiClient client;
        [NonSerialized] private bool isBusy;
        [NonSerialized] private string error;
        [NonSerialized] private bool ownersNeedLoading;

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

        public override void OnEnable()
        {
            base.OnEnable();
            ownersNeedLoading = publishOwners == null && !isBusy;
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        private void MaybeUpdateData()
        {
            if (ownersNeedLoading)
            {
                ownersNeedLoading = false;
                LoadOwners();
            }
        }

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;
        }

        private void LoadOwners()
        {
            var keychainConnections = Platform.Keychain.Connections;
            //TODO: ONE_USER_LOGIN This assumes only ever one user can login

            isBusy = true;

            //TODO: ONE_USER_LOGIN This assumes only ever one user can login
            username = keychainConnections.First().Username;

            Logger.Trace("Loading Owners");

            Client.GetOrganizations(orgs =>
            {
                Logger.Trace("Loaded {0} Owners", orgs.Length);

                publishOwners = orgs
                    .OrderBy(organization => organization.Login)
                    .Select(organization => organization.Login)
                    .ToArray();

                owners = new[] { OwnersDefaultText, username }.Union(publishOwners).ToArray();

                isBusy = false;

                Redraw();
            }, exception =>
            {
                isBusy = false;

                var keychainEmptyException = exception as KeychainEmptyException;
                if (keychainEmptyException != null)
                {
                    Logger.Trace("Keychain empty");
                    PopupWindow.OpenWindow(PopupWindow.PopupViewType.AuthenticationView);
                    return;
                }

                Logger.Error(exception, "Unhandled Exception");
            });
        }

        public override void OnGUI()
        {
            GUILayout.BeginHorizontal(Styles.AuthHeaderBoxStyle);
            {
            	GUILayout.Label(PublishToGithubLabel, EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(isBusy);
            {
                selectedOwner = EditorGUILayout.Popup(SelectedOwnerLabel, selectedOwner, owners);
                repoName = EditorGUILayout.TextField(RepositoryNameLabel, repoName);
                repoDescription = EditorGUILayout.TextField(DescriptionLabel, repoDescription);

                togglePrivate = EditorGUILayout.Toggle(CreatePrivateRepositoryLabel, togglePrivate);

                var repoPrivacyExplanation = togglePrivate ? PrivateRepoMessage : PublicRepoMessage;
                EditorGUILayout.HelpBox(repoPrivacyExplanation, MessageType.None);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(!IsFormValid);
                    if (GUILayout.Button(PublishViewCreateButton))
                    {
                        GUI.FocusControl(null);
                        isBusy = true;

                        var organization = owners[selectedOwner] == username ? null : owners[selectedOwner];

                        var cleanRepoDescription = repoDescription.Trim();
                        cleanRepoDescription = string.IsNullOrEmpty(cleanRepoDescription) ? null : cleanRepoDescription;

                        Client.CreateRepository(new NewRepository(repoName)
                        {
                            Private = togglePrivate,
                            Description = cleanRepoDescription
                        }, (repository, ex) =>
                        {
                            if (ex != null)
                            {
                                Logger.Error(ex, "Repository Create Error Type:{0}", ex.GetType().ToString());

                                error = GetPublishErrorMessage(ex);
                                isBusy = false;
                                return;
                            }

                            if (repository == null)
                            {
                                Logger.Warning("Returned Repository is null");
                                isBusy = false;
                                return;
                            }

                            Logger.Trace("Repository Created");

                            GitClient.RemoteAdd("origin", repository.CloneUrl)
                                     .Then(GitClient.Push("origin", Repository.CurrentBranch.Value.Name))
                                     .ThenInUI(Finish)
                                     .Start();
                        }, organization);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                if (error != null)
                    EditorGUILayout.HelpBox(error, MessageType.Error);

                GUILayout.FlexibleSpace();
            }
            EditorGUI.EndDisabledGroup();
        }

        private string GetPublishErrorMessage(Exception ex)
        {
            if (ex.Message.StartsWith(PublishLimitPrivateRepositoriesError))
            {
                return PublishLimitPrivateRepositoriesError;
            }
            
            return ex.Message;
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        private bool IsFormValid
        {
            get { return !string.IsNullOrEmpty(repoName) && !isBusy && selectedOwner != 0; }
        }
    }
}
