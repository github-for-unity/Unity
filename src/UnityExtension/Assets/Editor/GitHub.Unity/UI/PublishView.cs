using System;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField] private Connection[] connections;
        [SerializeField] private string[] connectionLabels;
        [SerializeField] private int selectedConnection;

        [SerializeField] private string[] owners = { OwnersDefaultText };
        [SerializeField] private string[] publishOwners;
        [SerializeField] private int selectedOwner;
        [SerializeField] private string repoName = String.Empty;
        [SerializeField] private string repoDescription = "";
        [SerializeField] private bool togglePrivate;

        [NonSerialized] private Dictionary<string, IApiClient> clients = new Dictionary<string, IApiClient>();
        [NonSerialized] private IApiClient selectedClient;
        [NonSerialized] private string error;
        [NonSerialized] private bool connectionsNeedLoading;
        [NonSerialized] private bool ownersNeedLoading;

        public override void OnEnable()
        {
            base.OnEnable();
            ownersNeedLoading = publishOwners == null && !IsBusy;
            connectionsNeedLoading = connections == null && !IsBusy;
        }

        public override void OnDataUpdate(bool first)
        {
            base.OnDataUpdate(first);
            MaybeUpdateData(first);
        }

        private void MaybeUpdateData(bool first)
        {
            if (connectionsNeedLoading)
            {
                connectionsNeedLoading = false;
                connections = Platform.Keychain.Connections.OrderByDescending(HostAddress.IsGitHubDotCom).ToArray();
                connectionLabels = connections.Select(c => HostAddress.IsGitHubDotCom(c) ? "GitHub" : c.Host.ToUriString().Host).ToArray();

                var connection = connections.First();
                selectedConnection = 0;
                selectedClient = GetApiClient(connection);
            }

            if (ownersNeedLoading)
            {
                ownersNeedLoading = false;
                LoadOwners();
            }
        }

        private IApiClient GetApiClient(Connection connection)
        {
            IApiClient client;

            if (!clients.TryGetValue(connection.Host, out client))
            {
                client = new ApiClient(Platform.Keychain, Platform.ProcessManager, TaskManager, Environment, connection.Host);

                clients.Add(connection.Host, client);
            }

            return client;
        }

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;
        }

        private void LoadOwners()
        {
            IsBusy = true;

            selectedClient.GetOrganizations(orgs =>
            {
                publishOwners = orgs
                    .OrderBy(organization => organization.Login)
                    .Select(organization => organization.Login)
                    .ToArray();

                owners = new[] { OwnersDefaultText, connections[selectedConnection].Username }.Union(publishOwners).ToArray();
                Refresh();
            },
            exception =>
            {
                var keychainEmptyException = exception as KeychainEmptyException;
                if (keychainEmptyException != null)
                {
                    PopupWindow.OpenWindow(PopupWindow.PopupViewType.AuthenticationView);
                    return;
                }
                Logger.Error(exception, "Unhandled Exception");
                IsBusy = false;
            });
        }

        public override void OnUI()
        {
            GUILayout.BeginHorizontal(Styles.AuthHeaderBoxStyle);
            {
                GUILayout.Label(PublishToGithubLabel, EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(IsBusy);
            {
                if (connections.Length > 1)
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        selectedConnection = EditorGUILayout.Popup("Connections:", selectedConnection, connectionLabels);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        selectedClient = GetApiClient(connections[selectedConnection]);
                        ownersNeedLoading = true;
                        Redraw();
                    }
                }

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
                        IsBusy = true;

                        var organization = owners[selectedOwner] == connections[selectedConnection].Username ? null : owners[selectedOwner];

                        var cleanRepoDescription = repoDescription.Trim();
                        cleanRepoDescription = string.IsNullOrEmpty(cleanRepoDescription) ? null : cleanRepoDescription;

                        selectedClient.CreateRepository(repoName, cleanRepoDescription, togglePrivate, (repository, ex) =>
                        {
                            if (ex != null)
                            {
                                Logger.Error(ex, "Repository Create Error Type:{0}", ex.GetType().ToString());
                                error = GetPublishErrorMessage(ex);
                                IsBusy = false;
                                return;
                            }

                            UsageTracker.IncrementPublishViewButtonPublish();

                            if (repository == null)
                            {
                                Logger.Warning("Returned Repository is null");
                                IsBusy = false;
                                return;
                            }
                            Repository.RemoteAdd("origin", repository.CloneUrl)
                                .Then(Repository.Push("origin"))
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

        private bool IsFormValid
        {
            get { return !string.IsNullOrEmpty(repoName) && !IsBusy && selectedOwner != 0; }
        }

        public override bool IsBusy { get; set; }
    }
}
