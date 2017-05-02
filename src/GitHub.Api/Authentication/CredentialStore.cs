using System;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Generic;
using Rackspace.Threading;

namespace GitHub.Unity
{
    class OctokitCredentialAdapter : ICredentialStore
    {
        Credentials octokitCredentials = Credentials.Anonymous;

        public void Save(ICredential credential)
        {
            octokitCredentials = new Credentials(credential.Username, credential.Token);
        }

        /// <summary>
        /// Implementation for Octokit
        /// </summary>
        /// <returns>Octokit credentials</returns>
        Task<Credentials> ICredentialStore.GetCredentials()
        {
            return TaskEx.FromResult(octokitCredentials);
        }
    }

    struct Connection
    {
        UriString host;
        string username;
    }

    /// <summary>
    /// {
    ///     connections: [
    ///         connection: { host: "github.com", user: "shana" },
    ///         connection: { host: "server.com", user: "auser" },
    ///     ]
    /// }
    /// </summary>
    class Keychain : IKeychain
    {
        // loaded at the start of application from cached/serialized data
        private List<Connection> connectionCache = new List<Connection>();

        // cached credentials loaded from git to pass to GitHub/ApiClient
        private Dictionary<UriString, ICredentialStore> octokitStores = new Dictionary<UriString, ICredentialStore>();

        public void Connect(UriString host)
        {
            //octokitStores.Add(host, new OctokitCredentialAdapter());
        }

        public async Task<ICredential> Load(UriString host)
        {
            ICredentialManager credentialBackend = null;
            return await credentialBackend.Load(host);
            
        }

        public void Clear(UriString host)
        {
            // delete connection in the connection list
            // delete credential in octokit store
        }

        public void Flush(UriString host)
        {
            // create new connection in the connection cache for this host
            // flushes credential cache to disk (host and username only)
            // saves credential in git credential manager (host, username, token)
        }

        public Task Save(ICredential credential)
        {
            // save to octokitStore only
            return CompletedTask.Default;
        }
    }

    internal interface IKeychain
    {
        Task<ICredential> Load(UriString host);
        Task Save(ICredential credential);
    }
}
