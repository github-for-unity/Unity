using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class AuthenticationService
    {
        private readonly IAppConfiguration appConfiguration;
        private readonly ICredentialManager credentialManager;
        private IApiClient client;

        private LoginResult loginResultData;

        public AuthenticationService(IAppConfiguration appConfiguration, ICredentialManager credentialManager)
        {
            this.appConfiguration = appConfiguration;
            this.credentialManager = credentialManager;
            this.client = ApiClientFactory.Instance.Create(UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri));
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
