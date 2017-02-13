using System;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GitHub.Api
{
    public class LoginResult
    {
        public bool NeedTwoFA { get { return Data.Code == LoginResultCodes.CodeRequired || Data.Code == LoginResultCodes.CodeFailed; } }
        public bool Success { get { return Data.Code == LoginResultCodes.Success; } }
        public bool Failed { get { return Data.Code == LoginResultCodes.Failed; } }
        public string Message { get { return Data.Message; } }

        internal LoginResultData Data { get; set; }
        internal Action<bool, string> Callback { get; set; }
        internal Action<LoginResult> TwoFACallback { get; set; }

        internal LoginResult(LoginResultData data, Action<bool, string> callback, Action<LoginResult> twofaCallback)
        {
            this.Data = data;
            this.Callback = callback;
            this.TwoFACallback = twofaCallback;
        }
    }

    class SimpleApiClient : ISimpleApiClient
    {
        private static readonly Unity.ILogging logger = Unity.Logging.GetLogger<SimpleApiClient>();
        public HostAddress HostAddress { get; }
        public UriString OriginalUrl { get; }

        private readonly IGitHubClient githubClient;
        private readonly ICredentialManager credentialManager;
        private readonly ILoginManager loginManager;
        private static readonly SemaphoreSlim sem = new SemaphoreSlim(1);

        Repository repositoryCache = new Repository();
        string owner;
        bool? isEnterprise;

        public SimpleApiClient(UriString repoUrl, ICredentialManager credentialManager, IGitHubClient githubClient)
        {
            Guard.ArgumentNotNull(repoUrl, nameof(repoUrl));
            Guard.ArgumentNotNull(credentialManager, nameof(credentialManager));
            Guard.ArgumentNotNull(githubClient, nameof(githubClient));

            HostAddress = HostAddress.Create(repoUrl);
            OriginalUrl = repoUrl;
            this.githubClient = githubClient;
            this.credentialManager = credentialManager;
            loginManager = new LoginManager(credentialManager, ApplicationInfo.ClientId, ApplicationInfo.ClientSecret);
        }

        public async void GetRepository(Action<Repository> callback)
        {
            Guard.ArgumentNotNull(callback, "callback");
            var repo = await GetRepositoryInternal();
            callback(repo);
        }

        public async void Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result)
        {
            Guard.ArgumentNotNull(need2faCode, "need2faCode");
            Guard.ArgumentNotNull(result, "result");

            LoginResultData res = null;
            try
            {
                res = await loginManager.Login(HostAddress, githubClient, username, password);
            }
            catch (Exception ex)
            {
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

        public async void ContinueLogin(LoginResult loginResult, string code)
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

        async Task<Repository> GetRepositoryInternal()
        {
            try
            {
                if (owner == null)
                {
                    var ownerLogin = OriginalUrl.Owner;
                    var repositoryName = OriginalUrl.RepositoryName;

                    if (ownerLogin != null && repositoryName != null)
                    {
                        var repo = await githubClient.Repository.Get(ownerLogin, repositoryName);
                        if (repo != null)
                        {
                            repositoryCache = repo;
                        }
                        owner = ownerLogin;
                    }
                }
            }
            // it'll throw if it's private or an enterprise instance requiring authentication
            catch (ApiException apiex)
            {
                if (!HostAddress.IsGitHubDotComUri(OriginalUrl.ToRepositoryUrl()))
                    isEnterprise = apiex.IsGitHubApiException();
            }
            catch {}
            finally
            {
                sem.Release();
            }

            return repositoryCache;
        }
    }
}
