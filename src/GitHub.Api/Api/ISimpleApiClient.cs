using System.Threading.Tasks;
using GitHub.Primitives;
using Octokit;
using System;

namespace GitHub.Api
{
    public interface ISimpleApiClient
    {
        HostAddress HostAddress { get; }
        UriString OriginalUrl { get; }
        Task GetRepository(Action<Repository> callback);
    }
}
