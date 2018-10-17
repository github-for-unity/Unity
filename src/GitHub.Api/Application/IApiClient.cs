using System;

namespace GitHub.Unity
{
    interface IApiClient
    {
        HostAddress HostAddress { get; }
        void CreateRepository(string name, string description, bool isPrivate,
            Action<GitHubRepository, Exception> callback, string organization = null);
        void GetOrganizations(Action<Organization[]> onSuccess, Action<Exception> onError = null);
        void Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result);
        void ContinueLogin(LoginResult loginResult, string code);
        void LoginWithToken(string token, Action<bool> result);
        ITask Logout(UriString host);
        void GetCurrentUser(Action<GitHubUser> onSuccess, Action<Exception> onError = null);
        void GetEnterpriseServerMeta(Action<GitHubHostMeta> onSuccess, Action<Exception> onError = null);
    }
}
