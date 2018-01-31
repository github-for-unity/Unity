using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using System.Runtime.Serialization;

namespace GitHub.Unity
{
    class ApiClient : IApiClient
    {
        public static IApiClient Create(UriString repositoryUrl, IKeychain keychain)
        {
            logger.Trace("Creating ApiClient: {0}", repositoryUrl);

            var credentialStore = keychain.Connect(repositoryUrl);
            var hostAddress = HostAddress.Create(repositoryUrl);

            return new ApiClient(repositoryUrl, keychain,
                new GitHubClient(AppConfiguration.ProductHeader, credentialStore, hostAddress.ApiUri));
        }

        private static readonly ILogging logger = Logging.GetLogger<ApiClient>();
        public HostAddress HostAddress { get; }
        public UriString OriginalUrl { get; }

        private readonly IKeychain keychain;
        private readonly IGitHubClient githubClient;
        private readonly ILoginManager loginManager;

        public ApiClient(UriString hostUrl, IKeychain keychain, IGitHubClient githubClient)
        {
            Guard.ArgumentNotNull(hostUrl, nameof(hostUrl));
            Guard.ArgumentNotNull(keychain, nameof(keychain));
            Guard.ArgumentNotNull(githubClient, nameof(githubClient));

            HostAddress = HostAddress.Create(hostUrl);
            OriginalUrl = hostUrl;
            this.keychain = keychain;
            this.githubClient = githubClient;
            loginManager = new LoginManager(keychain, ApplicationInfo.ClientId, ApplicationInfo.ClientSecret);
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
                res = await loginManager.Login(OriginalUrl, githubClient, username, password);
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
                res = await loginManager.Login(OriginalUrl, githubClient, username, password);
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

                GitHubRepository repository;
                if (!string.IsNullOrEmpty(organization))
                {
                    logger.Trace("Creating repository for organization");

                    repository = (await githubClient.Repository.Create(organization, newRepository)).ToGitHubRepository();
                }
                else
                {
                    logger.Trace("Creating repository for user");

                    repository = (await githubClient.Repository.Create(newRepository)).ToGitHubRepository();
                }

                logger.Trace("Created Repository");
                return repository;
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

                var organizations = await githubClient.Organization.GetAllForCurrent();

                logger.Trace("Obtained {0} Organizations", organizations?.Count.ToString() ?? "NULL");

                if (organizations != null)
                {
                    var array = organizations.Select(organization => new Organization() {
                        Name = organization.Name,
                        Login = organization.Login
                    }).ToArray();
                    onSuccess(array);
                }
            }
            catch(Exception ex)
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

                return (await githubClient.User.Current()).ToGitHubUser();
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

                return keychainAdapter.OctokitCredentials != Credentials.Anonymous;
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
