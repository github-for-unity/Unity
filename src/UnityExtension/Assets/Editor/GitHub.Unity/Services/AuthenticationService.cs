using System;

namespace GitHub.Unity
{
    class AuthenticationService
    {
        private readonly IApiClient client;

        private LoginResult loginResultData;

        public AuthenticationService(UriString host, IKeychain keychain)
        {
            client = ApiClient.Create(host, keychain);
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
