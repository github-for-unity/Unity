using System.Threading.Tasks;
using Octokit;
using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    interface IApiClient
    {
        HostAddress HostAddress { get; }
        UriString OriginalUrl { get; }
        Task CreateRepository(NewRepository newRepository, Action<Octokit.Repository, Exception> callback, string organization = null);
        Task GetOrganizations(Action<IList<Organization>> callback);
        Task Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result);
        Task ContinueLogin(LoginResult loginResult, string code);
        Task<bool> LoginAsync(string username, string password, Func<LoginResult, string> need2faCode);
        Task<bool> ValidateCredentials();
        Task Logout(UriString host);
        Task GetCurrentUser(Action<Octokit.User> callback);
        Task LoadKeychain(Action<bool> callback);
    }
}
