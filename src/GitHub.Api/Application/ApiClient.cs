using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILoginManager loginManager;
        private readonly IEnvironment environment;

        public ApiClient(UriString hostUrl, IKeychain keychain, IProcessManager processManager, ITaskManager taskManager, IEnvironment environment)
        {
            Guard.ArgumentNotNull(keychain, nameof(keychain));

            var host = String.IsNullOrEmpty(hostUrl)
                ? UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri)
                : new UriString(hostUrl.ToRepositoryUri()
                    .GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));

            HostAddress = HostAddress.Create(host);
            OriginalUrl = host;
            this.keychain = keychain;
            this.processManager = processManager;
            this.taskManager = taskManager;
            this.environment = environment;
            loginManager = new LoginManager(keychain, processManager, taskManager, environment);
        }

        public ITask Logout(UriString host)
        {
            return loginManager.Logout(host);
        }

        public void CreateRepository(string name, string description, bool isPrivate,
            Action<GitHubRepository, Exception> callback, string organization = null)
        {
            Guard.ArgumentNotNull(callback, "callback");

            new FuncTask<GitHubRepository>(taskManager.Token, () =>
            {
                // this validates the user, again
                GetCurrentUser();

                var command = new StringBuilder("publish -r \"");
                command.Append(name);
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

                var octorunTask = new OctorunTask(taskManager.Token, keychain, environment, command.ToString())
                    .Configure(processManager);

                var ret = octorunTask.RunSynchronously();
                if (ret.IsSuccess && ret.Output.Length == 2)
                {
                    return new GitHubRepository
                    {
                        Name = ret.Output[0],
                        CloneUrl = ret.Output[1]
                    };
                }

                throw new ApiClientException(ret.GetApiErrorMessage() ?? "Publish failed");
            })
            .FinallyInUI((success, ex, repository) =>
            {
                if (success)
                    callback(repository, null);
                else
                {
                    logger.Error(ex, "Error creating repository");
                    callback(null, ex);
                }
            })
            .Start();
        }

        public void GetOrganizations(Action<Organization[]> onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            new FuncTask<Organization[]>(taskManager.Token, () =>
            {
                var octorunTask = new OctorunTask(taskManager.Token, keychain, environment,
                        "organizations")
                    .Configure(processManager);

                var ret = octorunTask.RunSynchronously();
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
                    return organizations.ToArray();
                }

                throw new ApiClientException(ret.GetApiErrorMessage() ?? "Error getting organizations");
            })
            .FinallyInUI((success, ex, orgs) =>
            {
                if (success)
                    onSuccess(orgs);
                else
                {
                    logger.Error(ex, "Error Getting Organizations");
                    onError?.Invoke(ex);
                }
            })
            .Start();
        }

        public void GetCurrentUser(Action<GitHubUser> onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            new FuncTask<GitHubUser>(taskManager.Token, GetCurrentUser)
                .FinallyInUI((success, ex, user) =>
                {
                    if (success)
                        onSuccess(user);
                    else
                        onError?.Invoke(ex);
                })
                .Start();
        }

        public void Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result)
        {
            Guard.ArgumentNotNull(need2faCode, "need2faCode");
            Guard.ArgumentNotNull(result, "result");

            new FuncTask<LoginResultData>(taskManager.Token,
                () => loginManager.Login(OriginalUrl, username, password))
                .FinallyInUI((success, ex, res) =>
                {
                    if (!success)
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
                })
                .Start();
        }

        public void ContinueLogin(LoginResult loginResult, string code)
        {
            new FuncTask<LoginResultData>(taskManager.Token,
                () => loginManager.ContinueLogin(loginResult.Data, code))
                .FinallyInUI((success, ex, result) =>
                {
                    if (!success)
                    {
                        loginResult.Callback(false, ex.Message);
                        return;
                    }
                    if (result.Code == LoginResultCodes.CodeFailed)
                    {
                        loginResult.TwoFACallback(new LoginResult(result, loginResult.Callback, loginResult.TwoFACallback));
                    }
                    loginResult.Callback(result.Code == LoginResultCodes.Success, result.Message);
                })
                .Start();
        }

        private GitHubUser GetCurrentUser()
        {
            var keychainConnection = keychain.Connections.FirstOrDefault(x => x.Host == OriginalUrl);
            if (keychainConnection == null)
                throw new KeychainEmptyException();

            var keychainAdapter = GetValidatedKeychainAdapter(keychainConnection);

            // we can't trust that the system keychain has the username filled out correctly.
            // if it doesn't, we need to grab the username from the server and check it
            // unfortunately this means that things will be slower when the keychain doesn't have all the info
            if (keychainConnection.User == null || keychainAdapter.Credential.Username != keychainConnection.Username)
            {
                keychainConnection.User = GetValidatedGitHubUser(keychainConnection, keychainAdapter);
            }
            return keychainConnection.User;
        }

        private IKeychainAdapter GetValidatedKeychainAdapter(Connection keychainConnection)
        {
            var keychainAdapter = keychain.Load(keychainConnection.Host);
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

        private GitHubUser GetValidatedGitHubUser(Connection keychainConnection, IKeychainAdapter keychainAdapter)
        {
            try
            {
                var octorunTask = new OctorunTask(taskManager.Token, keychain, environment, "validate")
                    .Configure(processManager);

                var ret = octorunTask.RunSynchronously();
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
