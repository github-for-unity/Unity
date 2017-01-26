using System.Threading.Tasks;
using GitHub.Primitives;
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

        Credentials LoadCredentials()
        {
            var keyHost = hostAddress.CredentialCacheKeyHost;
            using (var credential = credentialBackend.Load(keyHost))
            {
                if (credential != null)
                    return new Credentials(credential.Key, credential.Value);
            }
            return Credentials.Anonymous;
        }

        void RemoveCredentials()
        {
            var keyHost = hostAddress.CredentialCacheKeyHost;
            using (var credential = credentialBackend.Load(keyHost))
            {
                if (credential != null)
                    credentialBackend.Delete(credential);
            }
        }

        Task<Credentials> ICredentialStore.GetCredentials()
        {
            return TaskEx.Run(() => LoadCredentials());
        }
    }
}
