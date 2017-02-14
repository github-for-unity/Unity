using System.Threading.Tasks;
using Octokit;
using System;

namespace GitHub.Api
{
    interface IApiClient
    {
        HostAddress HostAddress { get; }
        UriString OriginalUrl { get; }
        void GetRepository(Action<Repository> callback);
        Task Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result);
        Task ContinueLogin(LoginResult loginResult, string code);
        Task<bool> LoginAsync(string username, string password, Func<LoginResult, string> need2faCode);
        Task<bool> ValidateCredentials();
    }
}
