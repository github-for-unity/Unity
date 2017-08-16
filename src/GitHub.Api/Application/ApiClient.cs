using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

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

        private static readonly Unity.ILogging logger = Unity.Logging.GetLogger<ApiClient>();
        public HostAddress HostAddress { get; }
        public UriString OriginalUrl { get; }

        private readonly IKeychain keychain;
        private readonly IGitHubClient githubClient;
        private readonly ILoginManager loginManager;
        private static readonly SemaphoreSlim sem = new SemaphoreSlim(1);

        IList<Organization> organizationsCache;
        Octokit.User userCache;

        string owner;
        bool? isEnterprise;

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

        public async Task CreateRepository(NewRepository newRepository, Action<Octokit.Repository, Exception> callback, string organization = null)
        {
            Guard.ArgumentNotNull(callback, "callback");
            await CreateRepositoryInternal(newRepository, callback, organization);
        }

        public async Task GetOrganizations(Action<IList<Organization>> callback)
        {
            Guard.ArgumentNotNull(callback, "callback");
            var organizations = await GetOrganizationInternal();
            callback(organizations);
        }

        public async Task GetCurrentUser(Action<Octokit.User> callback)
        {
            Guard.ArgumentNotNull(callback, "callback");
            var user = await GetCurrentUserInternal();
            callback(user);
        }

        public async Task GetCurrentUserAndOrganizations(Action<Octokit.User, IList<Organization>> callback)
        {
            Guard.ArgumentNotNull(callback, "callback");
            await GetUsersAndOrganizationInternal(callback);
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

        private async Task CreateRepositoryInternal(NewRepository newRepository, Action<Octokit.Repository, Exception> callback, string organization)
        {
            try
            {
                logger.Trace("Creating repository");

                if (!await EnsureKeychainLoaded())
                {
                    callback(null, new Exception("Keychain Not Loaded"));
                    return;
                }

                Octokit.Repository repository;
                if (!string.IsNullOrEmpty(organization))
                {
                    logger.Trace("Creating repository for organization");

                    repository = await githubClient.Repository.Create(organization, newRepository);
                }
                else
                {
                    logger.Trace("Creating repository for user");

                    repository = await githubClient.Repository.Create(newRepository);
                }

                logger.Trace("Created Repository");

                callback(repository, null);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Creating Repository");
                callback(null, ex);
            }
        }

        private async Task<IList<Organization>> GetOrganizationInternal()
        {
            try
            {
                logger.Trace("Getting Organizations");

                if (!await EnsureKeychainLoaded())
                {
                    return null;
                }

                var organizations = await githubClient.Organization.GetAllForCurrent();

                logger.Trace("Obtained {0} Organizations", organizations?.Count.ToString() ?? "NULL");

                if (organizations != null)
                {
                    organizationsCache = organizations.ToArray();
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Error Getting Organizations");
                throw;
            }

            return organizationsCache;
        }

        private async Task<Octokit.User> GetCurrentUserInternal()
        {
            try
            {
                logger.Trace("Getting Organizations");

                if (!await EnsureKeychainLoaded())
                {
                    return null;
                }

                userCache = await githubClient.User.Current();
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Error Getting Current User");
                throw;
            }

            return userCache;
        }

        private async Task GetUsersAndOrganizationInternal(Action<Octokit.User, IList<Organization>> callback)
        {
            if (!await EnsureKeychainLoaded())
            {
                callback(null, null);
                return;
            }

            var currentUserInternal = GetCurrentUserInternal();
            var organizationInternal = GetOrganizationInternal();

            currentUserInternal.Start(TaskScheduler.Current);
            organizationInternal.Start(TaskScheduler.Current);

            callback(await currentUserInternal,await organizationInternal);
        }

        private async Task<bool> EnsureKeychainLoaded()
        {
            logger.Trace("EnsureKeychainLoaded");

            if (keychain.HasKeys)
            {
                if (!keychain.NeedsLoad)
                {
                    logger.Trace("EnsureKeychainLoaded: Has keys does not need load");
                    return true;
                }

                logger.Trace("EnsureKeychainLoaded: Loading");

                var uriString = keychain.Connections.First().Host;
                var keychainAdapter = await keychain.Load(uriString);

                return keychainAdapter.OctokitCredentials != Credentials.Anonymous;
            }

            logger.Trace("EnsureKeychainLoaded: No keys to load");

            return false;
        }

        public async Task<bool> ValidateCredentials()
        {
            try
            {
                var store = keychain.Connect(OriginalUrl);

                if (store.OctokitCredentials != Credentials.Anonymous)
                {
                    var credential = store.Credential;
                    await githubClient.Authorization.CheckApplicationAuthentication(ApplicationInfo.ClientId, credential.Token);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
