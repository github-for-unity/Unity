using Octokit;
using System.Collections.Concurrent;

namespace GitHub.Unity
{
    class ApiClientFactory : IApiClientFactory
    {
        private static readonly ConcurrentDictionary<UriString, IApiClient> cache = new ConcurrentDictionary<UriString, IApiClient>();

        private readonly ProductHeaderValue productHeader;
        private readonly ICredentialManager credentialManager;

        public ApiClientFactory(IAppConfiguration appConfiguration, ICredentialManager credentialManager)
        {
            productHeader = appConfiguration.ProductHeader;
            this.credentialManager = credentialManager; ;
        }

        public IApiClient Create(UriString repositoryUrl)
        {
            var hostAddress = HostAddress.Create(repositoryUrl);
            return cache.GetOrAdd(repositoryUrl,
                new ApiClient(repositoryUrl, credentialManager,
                    new GitHubClient(productHeader,
                        new CredentialStore(repositoryUrl, credentialManager),
                        hostAddress.ApiUri),
                    new GitHubClient(productHeader,
                        new AppCredentialStore(repositoryUrl, ApplicationInfo.ClientId, ApplicationInfo.ClientSecret),
                        hostAddress.ApiUri)
                )
            );
        }

        public void ClearFromCache(IApiClient client)
        {
            IApiClient c;
            cache.TryRemove(client.OriginalUrl, out c);
        }

        public static IApiClientFactory Instance { get; set; }
    }
}
