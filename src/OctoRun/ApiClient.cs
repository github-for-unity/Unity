using System;
using System.Threading.Tasks;
using GitHub.Unity;
using Octokit;
using static OctoRun.LoginManager;

namespace OctoRun
{
    class ApiClient
    {
        private readonly ILogging logger = Logging.GetLogger<ApiClient>();

        private readonly GitHubClient client;
        private readonly LoginManager loginManager;
        private readonly IKeychain keychain;

        public ApiClient(IKeychain keychain, HostAddress host)
        {
            this.keychain = keychain;
            client = new GitHubClient(AppConfiguration.ProductHeader, keychain as ICredentialStore, host.ApiUri);
            loginManager = new LoginManager(keychain, ApplicationInfo.ClientId, ApplicationInfo.ClientSecret);
        }

        public LoginResult Login()
        {
            LoginResultData res = null;
            try
            {
                res = loginManager.Login(client);
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
                return new LoginResult(new LoginResultData(LoginResultCodes.Failed, ex.Message), null);
            }

            return new LoginResult(res);
        }

        public LoginResult ContinueLogin()
        {
            LoginResultData result = null;
            try
            {
                result = loginManager.ContinueLogin(client);
            }
            catch (Exception ex)
            {
                return new LoginResult(new LoginResultData(LoginResultCodes.Failed, ex.Message), null);
            }
            return new LoginResult(result);
        }

    }
}