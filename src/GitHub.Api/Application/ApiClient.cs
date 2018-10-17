using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GitHub.Logging;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using GitHub.Unity.Json;

namespace GitHub.Unity
{
    class ApiClient : IApiClient
    {
        private static readonly ILogging logger = LogHelper.GetLogger<ApiClient>();
        private static readonly Regex httpStatusErrorRegex = new Regex("(?<=[a-z])([A-Z])", RegexOptions.Compiled);

        public HostAddress HostAddress { get; }

        private readonly IKeychain keychain;
        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;
        private readonly ILoginManager loginManager;
        private readonly IEnvironment environment;
        private IKeychainAdapter keychainAdapter;
        private Connection connection;

        public ApiClient(IKeychain keychain, IProcessManager processManager, ITaskManager taskManager, IEnvironment environment):
            this(UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri), keychain, processManager, taskManager, environment)
        {
        }

        public ApiClient(UriString host, IKeychain keychain, IProcessManager processManager, ITaskManager taskManager, IEnvironment environment)
        {
            Guard.ArgumentNotNull(host, nameof(host));
            Guard.ArgumentNotNull(keychain, nameof(keychain));

            host = new UriString(host.ToRepositoryUri().GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
            HostAddress = HostAddress.Create(host);

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
                EnsureValidCredentials();

                var command = new StringBuilder("publish");

                if (!HostAddress.IsGitHubDotCom())
                {
                    command.Append(" -h ");
                    command.Append(HostAddress.ApiUri.Host);
                }

                command.Append(" -r \"");
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

                var adapter = EnsureKeychainAdapter();

                var octorunTask = new OctorunTask(taskManager.Token, environment, command.ToString(), adapter.Credential.Token)
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

        public void GetEnterpriseServerMeta(Action<GitHubHostMeta> onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            new FuncTask<GitHubHostMeta>(taskManager.Token, () =>
            {
                var octorunTask = new OctorunTask(taskManager.Token, environment, "meta -h " + HostAddress.ApiUri.Host)
                    .Configure(processManager);

                var ret = octorunTask.RunSynchronously();
                if (ret.IsSuccess)
                {
                    var deserializeObject = SimpleJson.DeserializeObject<Dictionary<string, object>>(ret.Output[0]);

                    return new GitHubHostMeta
                    {
                        InstalledVersion = (string)deserializeObject["installed_version"],
                        GithubServicesSha = (string)deserializeObject["github_services_sha"],
                        VerifiablePasswordAuthentication = (bool)deserializeObject["verifiable_password_authentication"]
                    };
                }

                var message = ret.GetApiErrorMessage();

                logger.Trace("Message: {0}", message);

                if (message != null)
                {
                    if (message.Contains("ETIMEDOUT", StringComparison.InvariantCulture))
                    {
                        message = "Connection timed out.";
                    }
                    else if (message.Contains("ECONNREFUSED", StringComparison.InvariantCulture))
                    {
                        message = "Connection refused.";
                    }
                    else if (message.Contains("ENOTFOUND", StringComparison.InvariantCulture))
                    {
                        message = "Address not found.";
                    }
                    else
                    {
                        int httpStatusCode;
                        if (int.TryParse(message, out httpStatusCode))
                        {
                            var httpStatus = ((HttpStatusCode)httpStatusCode).ToString();
                            message = httpStatusErrorRegex.Replace(httpStatus, " $1");
                        }
                    }
                }
                else
                {
                    message = "Error getting server meta";
                }

                throw new ApiClientException(message);
            })
            .FinallyInUI((success, ex, meta) =>
            {
                if (success)
                    onSuccess(meta);
                else
                {
                    logger.Error(ex, "Error getting server meta");
                    onError?.Invoke(ex);
                }
            })
            .Start();
        }

        public void GetOrganizations(Action<Organization[]> onSuccess, Action<Exception> onError = null)
        {
            Guard.ArgumentNotNull(onSuccess, nameof(onSuccess));
            new FuncTask<Organization[]>(taskManager.Token, () =>
            {
                var adapter = EnsureKeychainAdapter();

                var command = HostAddress.IsGitHubDotCom() ? "organizations" : "organizations -h " + HostAddress.ApiUri.Host;
                var octorunTask = new OctorunTask(taskManager.Token, environment,
                        command, adapter.Credential.Token)
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

        private IKeychainAdapter EnsureKeychainAdapter()
        {
            var adapter = KeychainAdapter;
            if (adapter.Credential == null)
            {
                throw new ApiClientException("No Credentials found");
            }

            return adapter;
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

        public void LoginWithToken(string token, Action<bool> result)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(result, "result");

            new FuncTask<bool>(taskManager.Token,
                    () => loginManager.LoginWithToken(HostAddress.ApiUri.Host, token))
                .FinallyInUI((success, ex, res) =>
                {
                    if (!success)
                    {
                        logger.Warning(ex);
                        result(false);
                        return;
                    }

                    result(res);
                })
                .Start();
        }

        public void Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result)
        {
            Guard.ArgumentNotNull(need2faCode, "need2faCode");
            Guard.ArgumentNotNull(result, "result");

            new FuncTask<LoginResultData>(taskManager.Token,
                () => loginManager.Login(HostAddress.ApiUri.Host, username, password))
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

        public void EnsureValidCredentials()
        {
            GetCurrentUser();
        }

        public GitHubUser GetCurrentUser()
        {
            // we can't trust that the system keychain has the username filled out correctly.
            // if it doesn't, we need to grab the username from the server and check it
            // unfortunately this means that things will be slower when the keychain doesn't have all the info
            if (Connection.User == null || KeychainAdapter.Credential.Username != Connection.Username)
            {
                Connection.User = GetValidatedGitHubUser();
            }

            return Connection.User;
        }

        private Connection Connection
        {
            get
            {
                if (connection == null)
                {
                    connection = keychain.Connections.FirstOrDefault(x => x.Host == (UriString)HostAddress.ApiUri.Host);
                }

                return connection;
            }
        }

        private IKeychainAdapter KeychainAdapter
        {
            get
            {
                if (keychainAdapter == null)
                {
                    if (Connection == null)
                        throw new KeychainEmptyException();

                    var loadedKeychainAdapter = keychain.LoadFromSystem(Connection.Host);
                    if (loadedKeychainAdapter == null)
                        throw new KeychainEmptyException();

                    if (string.IsNullOrEmpty(loadedKeychainAdapter.Credential?.Username))
                    {
                        logger.Warning("LoadKeychainInternal: Username is empty");
                        throw new TokenUsernameMismatchException(connection.Username);
                    }

                    if (loadedKeychainAdapter.Credential.Username != connection.Username)
                    {
                        logger.Warning("LoadKeychainInternal: Token username does not match");
                    }

                    keychainAdapter = loadedKeychainAdapter;
                }

                return keychainAdapter;
            }
        }

        private GitHubUser GetValidatedGitHubUser()
        {
            try
            {
                var adapter = EnsureKeychainAdapter();

                var command = HostAddress.IsGitHubDotCom() ? "validate" : "validate -h " + HostAddress.ApiUri.Host;
                var octorunTask = new OctorunTask(taskManager.Token, environment, command, adapter.Credential.Token)
                    .Configure(processManager);

                var ret = octorunTask.RunSynchronously();
                if (ret.IsSuccess)
                {
                    var login = ret.Output[1];

                    if (login != Connection.Username)
                    {
                        logger.Trace("LoadKeychainInternal: Api username does not match");
                        throw new TokenUsernameMismatchException(Connection.Username, login);
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

    class GitHubHostMeta
    {
        public bool VerifiablePasswordAuthentication { get; set; }
        public string GithubServicesSha { get; set; }
        public string InstalledVersion { get; set; }
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
