using Octokit;
using System.Collections.Concurrent;

namespace GitHub.Api
{
    class SimpleApiClientFactory : ISimpleApiClientFactory
    {
        private static readonly ConcurrentDictionary<UriString, ISimpleApiClient> cache = new ConcurrentDictionary<UriString, ISimpleApiClient>();

        private readonly ProductHeaderValue productHeader;
        private readonly ICredentialManager credentialManager;

        public SimpleApiClientFactory(IAppConfiguration appConfiguration, ICredentialManager credentialManager)
        {
            productHeader = appConfiguration.ProductHeader;
            this.credentialManager = credentialManager; ;
        }

        public ISimpleApiClient Create(UriString repositoryUrl)
        {
            var hostAddress = HostAddress.Create(repositoryUrl);
            return cache.GetOrAdd(repositoryUrl,
                new SimpleApiClient(repositoryUrl, credentialManager,
                    new GitHubClient(productHeader,
                        new SimpleCredentialStore(hostAddress, credentialManager),
                        hostAddress.ApiUri)
                )
            );
        }

        public void ClearFromCache(ISimpleApiClient client)
        {
            ISimpleApiClient c;
            cache.TryRemove(client.OriginalUrl, out c);
        }

        public static ISimpleApiClientFactory Instance { get; set; }
    }
}
