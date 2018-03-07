using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using GitHub.Logging;
using System.Runtime.Serialization;
using System.Text;

namespace GitHub.Unity
{
    class ApiClient : IApiClient
    {
        public static IApiClient Create(UriString repositoryUrl, IKeychain keychain, IProcessManager processManager, ITaskManager taskManager, NPath nodeJsExecutablePath, NPath octorunScriptPath)
        {
            logger.Trace("Creating ApiClient: {0}", repositoryUrl);

            var credentialStore = keychain.Connect(repositoryUrl);
            var hostAddress = HostAddress.Create(repositoryUrl);

            return new ApiClient(repositoryUrl, keychain,
                new GitHubClient(ApplicationConfiguration.ProductHeader, credentialStore, hostAddress.ApiUri),
                processManager, taskManager, nodeJsExecutablePath, octorunScriptPath);
        }

        private static readonly ILogging logger = LogHelper.GetLogger<ApiClient>();
        public HostAddress HostAddress { get; }
        public UriString OriginalUrl { get; }

        private readonly IKeychain keychain;
        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;
        private readonly NPath nodeJsExecutablePath;
        private readonly NPath octorunScriptPath;
        private readonly ILoginManager loginManager;

        public ApiClient(UriString hostUrl, IKeychain keychain, IGitHubClient githubClient, IProcessManager processManager, ITaskManager taskManager, NPath nodeJsExecutablePath, NPath octorunScriptPath)
        {
            Guard.ArgumentNotNull(hostUrl, nameof(hostUrl));
            Guard.ArgumentNotNull(keychain, nameof(keychain));
            Guard.ArgumentNotNull(githubClient, nameof(githubClient));

            HostAddress = HostAddress.Create(hostUrl);
            OriginalUrl = hostUrl;
            this.keychain = keychain;
            this.processManager = processManager;
            this.taskManager = taskManager;
            this.nodeJsExecutablePath = nodeJsExecutablePath;
            this.octorunScriptPath = octorunScriptPath;
            loginManager = new LoginManager(keychain, ApplicationInfo.ClientId, ApplicationInfo.ClientSecret,
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

        public async Task CreateRepository(NewRepository newRepository, Action<GitHubRepository, Exception> callback, string organization = null)
        {
            Guard.ArgumentNotNull(callback, "callback");
            try
            {
                var repository = await CreateRepositoryInternal(newRepository, organization);
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

        public async Task ValidateCurrentUser(Action onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            try
            {
                await ValidateCurrentUserInternal();
                onSuccess();
            }
            catch (Exception e)
            {
                onError?.Invoke(e);
            }
        }

        public async Task GetCurrentUser(Action<GitHubUser> callback)
        {
            Guard.ArgumentNotNull(callback, "callback");
            var user = await GetCurrentUserInternal();
            callback(user);
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

        public async Task<bool> LoginAsync(string username, string password, Func<LoginResult, string> need2faCode)
        {
            Guard.ArgumentNotNull(need2faCode, "need2faCode");

            LoginResultData res = null;
            try
            {
                res = await loginManager.Login(OriginalUrl, username, password);
            }
            catch (Exception)
            {
                return false;
            }

            if (res.Code == LoginResultCodes.CodeRequired)
            {
                var resultCache = new LoginResult(res, null, null);
                var code = need2faCode(resultCache);
                return await ContinueLoginAsync(resultCache, need2faCode, code);
            }
            else
            {
                return res.Code == LoginResultCodes.Success;
            }
        }

        public async Task<bool> ContinueLoginAsync(LoginResult loginResult, Func<LoginResult, string> need2faCode, string code)
        {
            LoginResultData result = null;
            try
            {
                result = await loginManager.ContinueLogin(loginResult.Data, code);
            }
            catch (Exception)
            {
                return false;
            }

            if (result.Code == LoginResultCodes.CodeFailed)
            {
                var resultCache = new LoginResult(result, null, null);
                code = need2faCode(resultCache);
                if (String.IsNullOrEmpty(code))
                    return false;
                return await ContinueLoginAsync(resultCache, need2faCode, code);
            }
            return result.Code == LoginResultCodes.Success;
        }

        private async Task<GitHubRepository> CreateRepositoryInternal(NewRepository newRepository, string organization)
        {
            try
            {
                logger.Trace("Creating repository");

                await ValidateKeychain();
                await ValidateCurrentUserInternal();

                var uriString = keychain.Connections.First().Host;
                var keychainAdapter = await keychain.Load(uriString);

                var command = new StringBuilder("publish -r ");
                command.Append(newRepository.Name);

                if (!string.IsNullOrEmpty(newRepository.Description))
                {
                    command.Append(" -d ");
                    command.Append(newRepository.Description);
                }

                if (!string.IsNullOrEmpty(organization))
                {
                    command.Append(" -o ");
                    command.Append(organization);
                }

                if (newRepository.Private ?? false)
                {
                    command.Append(" -p");
                }

                var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScriptPath, command.ToString(), 
                    user: keychainAdapter.Credential.Username, userToken: keychainAdapter.Credential.Token)
                    .Configure(processManager);

                var ret = await octorunTask.StartAwait();

                if (ret.Count == 0)
                {
                    throw new ApiClientException("Publish failed");
                }

                if (ret[0] == "success")
                {
                    return new GitHubRepository()
                    {
                        Name = ret[1],
                        CloneUrl = ret[2],
                    };
                }

                if (ret.Count > 3)
                {
                    throw new ApiClientException(ret[3]);
                }

                throw new ApiClientException("Publish failed");
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

                await ValidateKeychain();
                await ValidateCurrentUserInternal();

                var uriString = keychain.Connections.First().Host;
                var keychainAdapter = await keychain.Load(uriString);

                var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScriptPath, "organizations",
                        user: keychainAdapter.Credential.Username, userToken: keychainAdapter.Credential.Token)
                    .Configure(processManager);

                var ret = await octorunTask.StartAsAsync();

                logger.Trace("Return: {0}", string.Join(";", ret.ToArray()));

                if (ret.Count == 0)
                {
                    throw new ApiClientException("Error getting organizations");
                }

                if (ret[0] == "success")
                {
                    var organizations = new List<Organization>();
                    for (var i = 1; i < ret.Count; i = i + 2)
                    {
                        organizations.Add(new Organization
                        {
                            Name = ret[i],
                            Login = ret[i + 1]
                        });
                    }

                    onSuccess(organizations.ToArray());
                    return;
                }

                throw new ApiClientException("Error getting organizations");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Getting Organizations");
                onError?.Invoke(ex);
            }
        }

        private async Task<GitHubUser> GetCurrentUserInternal()
        {
            try
            {
                logger.Trace("Getting Current User");
                await ValidateKeychain();

                var uriString = keychain.Connections.First().Host;
                var keychainAdapter = await keychain.Load(uriString);

                logger.Trace("Username: {0} Token: {1}", keychainAdapter.Credential.Username, keychainAdapter.Credential.Token);

                var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScriptPath, "validate",
                    user: keychainAdapter.Credential.Username, userToken: keychainAdapter.Credential.Token)
                    .Configure(processManager);

                var ret = await octorunTask.StartAsAsync();

                logger.Trace("Return: {0}", string.Join(";", ret.ToArray()));

                if (ret[0] == "success")
                {
                    return new GitHubUser {
                        Name = ret[1],
                        Login = ret[2]
                    };
                }

                throw new ApiClientException("Error validating current user");
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

        private async Task ValidateCurrentUserInternal()
        {
            logger.Trace("Validating User");

            var apiUser = await GetCurrentUserInternal();
            var apiUsername = apiUser.Login;

            var cachedUsername = keychain.Connections.First().Username;

            if (apiUsername != cachedUsername)
            {
                throw new TokenUsernameMismatchException(cachedUsername, apiUsername);
            }
        }

        private async Task<bool> LoadKeychainInternal()
        {
            if (keychain.HasKeys)
            {
                if (!keychain.NeedsLoad)
                {
                    logger.Trace("LoadKeychainInternal: Previously Loaded");
                    return true;
                }

                logger.Trace("LoadKeychainInternal: Loading");

                //TODO: ONE_USER_LOGIN This assumes only ever one user can login
                var uriString = keychain.Connections.First().Host;

                var keychainAdapter = await keychain.Load(uriString);
                logger.Trace("LoadKeychainInternal: Loaded");

                return keychainAdapter.Credential.Token != null;
            }

            logger.Trace("LoadKeychainInternal: No keys to load");

            return false;
        }

        private async Task ValidateKeychain()
        {
            if (!await LoadKeychainInternal())
            {
                throw new KeychainEmptyException();
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

        public TokenUsernameMismatchException(string cachedUsername, string currentUsername)
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
        { }
        public KeychainEmptyException(string message) : base(message)
        { }

        public KeychainEmptyException(string message, Exception innerException) : base(message, innerException)
        { }

        protected KeychainEmptyException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
