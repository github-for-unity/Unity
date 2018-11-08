using System;

namespace GitHub.Unity
{
    public interface IApiClient
    {
        HostAddress HostAddress { get; }
        UriString OriginalUrl { get; }
        void CreateRepository(string name, string description, bool isPrivate,
            Action<GitHubRepository, Exception> callback, string organization = null);
        void GetOrganizations(Action<Organization[]> onSuccess, Action<Exception> onError = null);
        void Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result);
        void ContinueLogin(LoginResult loginResult, string code);
        ITask Logout(UriString host);
        void GetCurrentUser(Action<GitHubUser> onSuccess, Action<Exception> onError = null);
    }
}
