using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class AuthenticationService
    {
        private readonly IAppConfiguration appConfiguration;
        private readonly IApiClient client;

        private LoginResult loginResultData;

        public AuthenticationService(IAppConfiguration appConfiguration, IKeychain keychain)
        {
            this.appConfiguration = appConfiguration;
            client = ApiClient.Create(UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri), keychain, appConfiguration);
        }

        public void Login(string username, string password, Action<string> twofaRequired, Action<bool, string> authResult)
        {

            loginResultData = null;
            client.Login(username, password, r =>
            {
                loginResultData = r;
                twofaRequired(r.Message);
            }, authResult);
        }

        public void LoginWith2fa(string code)
        {
            if (loginResultData == null)
                throw new InvalidOperationException("Call Login() first");
            client.ContinueLogin(loginResultData, code);
        }
    }
}
