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
        void GetRepository(Action<Octokit.Repository> callback);
        void GetOrganizations(Action<IList<Organization>> callback);
        Task Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result);
        Task ContinueLogin(LoginResult loginResult, string code);
        Task<bool> LoginAsync(string username, string password, Func<LoginResult, string> need2faCode);
        Task<bool> ValidateCredentials();
        void Logout(UriString host);
    }
}
