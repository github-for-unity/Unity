using System.Threading.Tasks;
using Octokit;
using System;

namespace GitHub.Api
{
    public interface ISimpleApiClient
    {
        HostAddress HostAddress { get; }
        UriString OriginalUrl { get; }
        void GetRepository(Action<Repository> callback);
        void Login(string username, string password, Action<LoginResult> need2faCode, Action<bool, string> result);
        void ContinueLogin(LoginResult loginResult, string code);
    }
}
