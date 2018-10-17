using System;

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
            client = host == null
                ? new ApiClient(keychain, processManager, taskManager, environment)
                : new ApiClient(host, keychain, processManager, taskManager, environment);
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
    }
}
