using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.Logging;
using System.Runtime.Serialization;
using System.Text;

namespace GitHub.Unity
{
    class ApiClient : IApiClient
    {
        private static readonly ILogging logger = LogHelper.GetLogger<ApiClient>();
        public HostAddress HostAddress { get; }
        public UriString OriginalUrl { get; }

        private readonly IKeychain keychain;
        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;
        private readonly NPath nodeJsExecutablePath;
        private readonly NPath octorunScriptPath;
        private readonly ILoginManager loginManager;

        public ApiClient(UriString hostUrl, IKeychain keychain, IProcessManager processManager, ITaskManager taskManager, NPath nodeJsExecutablePath, NPath octorunScriptPath)
        {
            Guard.ArgumentNotNull(hostUrl, nameof(hostUrl));
            Guard.ArgumentNotNull(keychain, nameof(keychain));

            HostAddress = HostAddress.Create(hostUrl);
            OriginalUrl = hostUrl;
            this.keychain = keychain;
            this.processManager = processManager;
            this.taskManager = taskManager;
            this.nodeJsExecutablePath = nodeJsExecutablePath;
            this.octorunScriptPath = octorunScriptPath;
            loginManager = new LoginManager(keychain,
                processManager: processManager,
                taskManager: taskManager,
                nodeJsExecutablePath: nodeJsExecutablePath,
                octorunScript: octorunScriptPath);
        }

        public async Task Logout(UriString host)
        {
            await LogoutInternal(host);
        }

        private async Task LogoutInternal(UriString host)
        {
            await loginManager.Logout(host);
        }

        public async Task CreateRepository(string name, string description, bool isPrivate, Action<GitHubRepository, Exception> callback, string organization = null)
        {
            Guard.ArgumentNotNull(callback, "callback");
            try
            {
                var repository = await CreateRepositoryInternal(name, organization, description, isPrivate);
                callback(repository, null);
            }
            catch (Exception e)
            {
                callback(null, e);
            }
        }

        public async Task GetOrganizations(Action<Organization[]> onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            await GetOrganizationInternal(onSuccess, onError);
        }

        public async Task GetCurrentUser(Action<GitHubUser> onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            try
            {
                var user = await GetCurrentUser();
                onSuccess(user);
            }
            catch (Exception e)
            {
                onError?.Invoke(e);
            }
        }

        public async Task Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result)
        {
            Guard.ArgumentNotNull(need2faCode, "need2faCode");
            Guard.ArgumentNotNull(result, "result");

            LoginResultData res = null;
            try
            {
                res = await loginManager.Login(OriginalUrl, username, password);
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
                result(false, ex.Message);
                return;
            }

            if (res.Code == LoginResultCodes.CodeRequired)
            {
                var resultCache = new LoginResult(res, result, need2faCode);
                need2faCode(resultCache);
            }
            else
            {
                result(res.Code == LoginResultCodes.Success, res.Message);
            }
        }

        public async Task ContinueLogin(LoginResult loginResult, string code)
        {
            LoginResultData result = null;
            try
            {
                result = await loginManager.ContinueLogin(loginResult.Data, code);
            }
            catch (Exception ex)
            {
                loginResult.Callback(false, ex.Message);
                return;
            }
            if (result.Code == LoginResultCodes.CodeFailed)
            {
                loginResult.TwoFACallback(new LoginResult(result, loginResult.Callback, loginResult.TwoFACallback));
            }
            loginResult.Callback(result.Code == LoginResultCodes.Success, result.Message);
        }

        private async Task<GitHubUser> GetCurrentUser()
        {
            //TODO: ONE_USER_LOGIN This assumes we only support one login
            var keychainConnection = keychain.Connections.FirstOrDefault();
            if (keychainConnection == null)
                throw new KeychainEmptyException();

            var keychainAdapter = await GetValidatedKeychainAdapter(keychainConnection);

            // we can't trust that the system keychain has the username filled out correctly.
            // if it doesn't, we need to grab the username from the server and check it
            // unfortunately this means that things will be slower when the keychain doesn't have all the info
            if (keychainConnection.User == null || keychainAdapter.Credential.Username != keychainConnection.Username)
            {
                keychainConnection.User = await GetValidatedGitHubUser(keychainConnection, keychainAdapter);
            }
            return keychainConnection.User;
        }

        private async Task<GitHubRepository> CreateRepositoryInternal(string repositoryName, string organization, string description, bool isPrivate)
        {
            try
            {
                logger.Trace("Creating repository");

                var user = await GetCurrentUser();
                var keychainAdapter = keychain.Connect(OriginalUrl);

                var command = new StringBuilder("publish -r \"");
                command.Append(repositoryName);
                command.Append("\"");

                if (!string.IsNullOrEmpty(description))
                {
                    command.Append(" -d \"");
                    command.Append(description);
                    command.Append("\"");
                }

                if (!string.IsNullOrEmpty(organization))
                {
                    command.Append(" -o \"");
                    command.Append(organization);
                    command.Append("\"");
                }

                if (isPrivate)
                {
                    command.Append(" -p");
                }

                var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScriptPath, command.ToString(),
                    user: user.Login, userToken: keychainAdapter.Credential.Token)
                    .Configure(processManager);

                var ret = await octorunTask.StartAwait();
                if (ret.IsSuccess && ret.Output.Length == 2)
                {
                    return new GitHubRepository
                    {
                        Name = ret.Output[0],
                        CloneUrl = ret.Output[1]
                    };
                }

                throw new ApiClientException(ret.GetApiErrorMessage() ?? "Publish failed");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Creating Repository");
                throw;
            }
        }

        private async Task GetOrganizationInternal(Action<Organization[]> onSuccess, Action<Exception> onError = null)
        {
            try
            {
                logger.Trace("Getting Organizations");

                var user = await GetCurrentUser();
                var keychainAdapter = keychain.Connect(OriginalUrl);

                var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScriptPath, "organizations",
                        user: user.Login, userToken: keychainAdapter.Credential.Token)
                    .Configure(processManager);

                var ret = await octorunTask.StartAsAsync();
                if (ret.IsSuccess)
                {
                    var organizations = new List<Organization>();
                    for (var i = 0; i < ret.Output.Length; i = i + 2)
                    {
                        organizations.Add(new Organization
                        {
                            Name = ret.Output[i],
                            Login = ret.Output[i + 1]
                        });
                    }

                    onSuccess(organizations.ToArray());
                    return;
                }

                throw new ApiClientException(ret.GetApiErrorMessage() ?? "Error getting organizations");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Getting Organizations");
                onError?.Invoke(ex);
            }
        }

        private async Task<IKeychainAdapter> GetValidatedKeychainAdapter(Connection keychainConnection)
        {
            var keychainAdapter = await keychain.Load(keychainConnection.Host);
            if (keychainAdapter == null)
                throw new KeychainEmptyException();

            if (string.IsNullOrEmpty(keychainAdapter.Credential?.Username))
            {
                logger.Warning("LoadKeychainInternal: Username is empty");
                throw new TokenUsernameMismatchException(keychainConnection.Username);
            }

            if (keychainAdapter.Credential.Username != keychainConnection.Username)
            {
                logger.Warning("LoadKeychainInternal: Token username does not match");
            }

            return keychainAdapter;
        }

        private async Task<GitHubUser> GetValidatedGitHubUser(Connection keychainConnection, IKeychainAdapter keychainAdapter)
        {
            try
            {
                var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScriptPath, "validate",
                        user: keychainConnection.Username, userToken: keychainAdapter.Credential.Token)
                    .Configure(processManager);

                var ret = await octorunTask.StartAsAsync();
                if (ret.IsSuccess)
                {
                    var login = ret.Output[1];

                    if (login != keychainConnection.Username)
                    {
                        logger.Trace("LoadKeychainInternal: Api username does not match");
                        throw new TokenUsernameMismatchException(keychainConnection.Username, login);
                    }

                    return new GitHubUser
                    {
                        Name = ret.Output[0],
                        Login = login
                    };
                }

                throw new ApiClientException(ret.GetApiErrorMessage() ?? "Error validating current user");
            }
            catch (KeychainEmptyException)
            {
                logger.Warning("Keychain is empty");
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Getting Current User");
                throw;
            }
        }
    }

    class GitHubUser
    {
        public string Name { get; set; }
        public string Login { get; set; }
    }

    class GitHubRepository
    {
        public string Name { get; set; }
        public string CloneUrl { get; set; }
    }

    [Serializable]
    public class ApiClientException : Exception
    {
        public ApiClientException()
        { }

        public ApiClientException(string message) : base(message)
        { }

        public ApiClientException(string message, Exception innerException) : base(message, innerException)
        { }

        protected ApiClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }

    [Serializable]
    class TokenUsernameMismatchException : ApiClientException
    {
        public string CachedUsername { get; }
        public string CurrentUsername { get; }

        public TokenUsernameMismatchException(string cachedUsername, string currentUsername = null)
        {
            CachedUsername = cachedUsername;
            CurrentUsername = currentUsername;
        }
        protected TokenUsernameMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }

    [Serializable]
    class KeychainEmptyException : ApiClientException
    {
        public KeychainEmptyException()
        {
        }

        protected KeychainEmptyException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
