using System;
using System.Text;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    class AuthenticationService
    {
        private readonly IApiClient client;

        private LoginResult loginResultData;

        public AuthenticationService(UriString host, IKeychain keychain,
            IProcessManager processManager, ITaskManager taskManager,
            IEnvironment environment
        )
        {
            client = new ApiClient(keychain, processManager, taskManager, environment, host);
        }

        public HostAddress HostAddress { get { return client.HostAddress; } }

        public void Login(string username, string password, Action<string> twofaRequired, Action<bool, string> authResult)
        {
            loginResultData = null;
            client.Login(username, password, r =>
            {
                loginResultData = r;
                twofaRequired(r.Message);
            }, authResult);
        }

        public void LoginWithToken(string token, Action<bool> authResult)
        {
            client.LoginWithToken(token, authResult);
        }

        public void LoginWith2fa(string code)
        {
            if (loginResultData == null)
                throw new InvalidOperationException("Call Login() first");
            client.ContinueLogin(loginResultData, code);
        }

        public void GetServerMeta(Action<GitHubHostMeta> serverMeta, Action<string> error)
        {
            loginResultData = null;
            client.GetEnterpriseServerMeta(data =>
            {
                serverMeta(data);
            }, exception => {
                error(exception.Message);
            });
        }

        public Uri GetLoginUrl(string state)
        {
            var query = new StringBuilder();

            query.Append("client_id=");
            query.Append(Uri.EscapeDataString(ApplicationInfo.ClientId));
            query.Append("&redirect_uri=");
            query.Append(Uri.EscapeDataString(OAuthCallbackManager.CallbackUrl.ToString()));
            query.Append("&scope=");
            query.Append(Uri.EscapeDataString("user,repo"));
            query.Append("&state=");
            query.Append(Uri.EscapeDataString(state));

            var uri = new Uri(HostAddress.WebUri, "login/oauth/authorize");
            var uriBuilder = new UriBuilder(uri)
            {
                Query = query.ToString()
            };
            return uriBuilder.Uri;
        }

        public void LoginWithOAuthCode(string code, Action<bool, string> result)
        {
            client.CreateOAuthToken(code, result);
        }
    }
}
