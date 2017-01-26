using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Primitives;
using Octokit;
using GitHub.Extensions;

namespace GitHub.Api
{
    public class SimpleApiClient : ISimpleApiClient
    {
        public HostAddress HostAddress { get; }
        public UriString OriginalUrl { get; }

        readonly IGitHubClient client;
        static readonly SemaphoreSlim sem = new SemaphoreSlim(1);

        Repository repositoryCache = new Repository();
        string owner;
        bool? isEnterprise;

        public SimpleApiClient(UriString repoUrl, IGitHubClient githubClient)
        {
            Guard.ArgumentNotNull(repoUrl, nameof(repoUrl));
            Guard.ArgumentNotNull(githubClient, nameof(githubClient));

            HostAddress = HostAddress.Create(repoUrl);
            OriginalUrl = repoUrl;
            client = githubClient;
        }

        public async Task GetRepository(Action<Repository> callback)
        {
            Guard.ArgumentNotNull(callback, "callback");
            var repo = await GetRepositoryInternal();
            callback(repo);
        }

        async Task<Repository> GetRepositoryInternal()
        {
            try
            {
                if (owner == null)
                {
                    var ownerLogin = OriginalUrl.Owner;
                    var repositoryName = OriginalUrl.RepositoryName;

                    if (ownerLogin != null && repositoryName != null)
                    {
                        var repo = await client.Repository.Get(ownerLogin, repositoryName);
                        if (repo != null)
                        {
                            repositoryCache = repo;
                        }
                        owner = ownerLogin;
                    }
                }
            }
            // it'll throw if it's private or an enterprise instance requiring authentication
            catch (ApiException apiex)
            {
                if (!HostAddress.IsGitHubDotComUri(OriginalUrl.ToRepositoryUrl()))
                    isEnterprise = apiex.IsGitHubApiException();
            }
            catch {}
            finally
            {
                sem.Release();
            }

            return repositoryCache;
        }
    }
}
