using System.Threading.Tasks;
using Octokit;

namespace GitHub.Api
{
    class SimpleCredentialStore : ICredentialStore
    {
        private readonly HostAddress hostAddress;
        private readonly ICredentialManager credentialBackend;

        public SimpleCredentialStore(HostAddress hostAddress, ICredentialManager credentialBackend)
        {
            this.hostAddress = hostAddress;
            this.credentialBackend = credentialBackend;
        }

        async Task<Credentials> ICredentialStore.GetCredentials()
        {
            var credential = await credentialBackend.Load(hostAddress);
            if (credential != null)
                return new Credentials(credential.Username, credential.Token);
            return Credentials.Anonymous;
        }
    }
}
