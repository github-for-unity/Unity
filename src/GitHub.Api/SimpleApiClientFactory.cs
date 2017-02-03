using System;
using System.Collections.Generic;
using GitHub.Models;
using GitHub.Primitives;
using Octokit;
using System.Collections.Concurrent;

namespace GitHub.Api
{
    public class SimpleApiClientFactory : ISimpleApiClientFactory
    {
        private static readonly ConcurrentDictionary<UriString, ISimpleApiClient> cache = new ConcurrentDictionary<UriString, ISimpleApiClient>();

        private readonly ProductHeaderValue productHeader;
        private readonly ICredentialManager credentialManager;


        public SimpleApiClientFactory(IProgram program, ICredentialManager credentialManager)
        {
            productHeader = program.ProductHeader;
            this.credentialManager = credentialManager; ;
        }

        public ISimpleApiClient Create(UriString repositoryUrl)
        {
            var hostAddress = HostAddress.Create(repositoryUrl);
            return cache.GetOrAdd(repositoryUrl,
                new SimpleApiClient(repositoryUrl,
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
    }
}
