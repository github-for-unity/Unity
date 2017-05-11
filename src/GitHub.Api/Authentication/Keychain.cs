using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace GitHub.Unity
{
    struct Connection
    {
        public UriString Host;
        public string Username;
    }

    class ConnectionCacheItem
    {
        public string Host { get; set; }
        public string Username { get; set; }
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
        const string ConnectionFile = "connections.json";

        private readonly ILogging logger = Logging.GetLogger<Keychain>();

        private readonly ICredentialManager credentialManager;
        private readonly NPath cachePath;

        // loaded at the start of application from cached/serialized data
        private Dictionary<UriString, Connection> connectionCache = new Dictionary<UriString, Connection>();

        // cached credentials loaded from git to pass to GitHub/ApiClient
        private Dictionary<UriString, KeychainAdapter> keychainAdapters =
            new Dictionary<UriString, KeychainAdapter>();

        public Keychain(IEnvironment environment, ICredentialManager credentialManager)
        {
            cachePath = environment.UserCachePath.Combine(ConnectionFile);
            this.credentialManager = credentialManager;
        }

        public KeychainAdapter Connect(UriString host)
        {
            return FindOrCreateAdapter(host);
        }

        public async Task<KeychainAdapter> Load(UriString host)
        {
            logger.Trace("Load Host:{0}", host);

            var keychainAdapter = FindOrCreateAdapter(host);

            var keychainItem = await credentialManager.Load(host);
            keychainAdapter.Set(keychainItem);

            return keychainAdapter;
        }

        private KeychainAdapter FindOrCreateAdapter(UriString host)
        {
            KeychainAdapter value;
            if (!keychainAdapters.TryGetValue(host, out value))
            {
                value = new KeychainAdapter();
                keychainAdapters.Add(host, value);
            }

            return value;
        }

        public void Initialize()
        {
            logger.Trace("Initialize");

            ReadCacheFromDisk();
        }

        private void ReadCacheFromDisk()
        {
            logger.Trace("ReadCacheFromDisk Path:{0}", cachePath.ToString());

            ConnectionCacheItem[] connections = null;
            if (cachePath.FileExists())
            {
                var json = cachePath.ReadAllText();
                try
                {
                    connections = SimpleJson.DeserializeObject<ConnectionCacheItem[]>(json);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error deserializing connection cache: {0}", cachePath);
                    cachePath.Delete();
                }
            }

            if (connections != null)
            {
                connectionCache =
                    connections.Select(item => new Connection { Host = new UriString(item.Host), Username = item.Username })
                               .ToDictionary(connection => connection.Host);
            }
            else
            {
                connectionCache = new Dictionary<UriString, Connection>();
            }
        }

        private void WriteCacheToDisk()
        {
            logger.Trace("WriteCacheToDisk Count:{0} Path:{1}", connectionCache.Count, cachePath.ToString());

            var connectionCacheItems =
                connectionCache.Select(
                    pair =>
                        new ConnectionCacheItem() {
                            Host = pair.Value.Host.ToString(),
                            Username = pair.Value.Username
                        }).ToArray();

            var json = SimpleJson.SerializeObject(connectionCacheItems);
            cachePath.WriteAllText(json);
        }

        /// <summary>
        /// Call Flush() to apply these changes
        /// </summary>
        public void Clear(UriString host)
        {
            logger.Trace("Clear Host:{0}", host);
       
            // delete connection in the connection list
            connectionCache.Remove(host);

            // delete credential in octokit store
            FindOrCreateAdapter(host).Clear();
        }

        /// <summary>
        /// Call Flush() to apply these changes
        /// </summary>
        public void Clear()
        {
            foreach (var k in keychainAdapters.Values.ToArray())
            {
                k.Clear();
            }
            connectionCache.Clear();
        }

        public async Task Flush(UriString host)
        {
            logger.Trace("Flush: {0}", host);

            KeychainAdapter credentialAdapter;
            if (!keychainAdapters.TryGetValue(host, out credentialAdapter))
            {
                throw new ArgumentException($"Host: {host} is not found");
            }

            if (credentialAdapter.OctokitCredentials == Credentials.Anonymous)
            {
                throw new InvalidOperationException("Anonymous credentials cannot be stored");
            }

            // create new connection in the connection cache for this host
            if (connectionCache.ContainsKey(host))
                connectionCache.Remove(host);
            connectionCache.Add(host, new Connection { Host = host, Username = credentialAdapter.OctokitCredentials.Login });

            // flushes credential cache to disk (host and username only)
            WriteCacheToDisk();

            // saves credential in git credential manager (host, username, token)
            await credentialManager.Delete(host);
            await credentialManager.Save(credentialAdapter.Credential);
        }

        public void Save(ICredential credential)
        {
            logger.Trace("Save Host:{0}", credential.Host);

            var credentialAdapter = FindOrCreateAdapter(credential.Host);
            credentialAdapter.Set(credential);
        }

        public void UpdateToken(UriString host, string token)
        {
            logger.Trace("UpdateToken Host:{0}", host);

            KeychainAdapter keychainAdapter;
            if (!keychainAdapters.TryGetValue(host, out keychainAdapter))
            {
                throw new ArgumentException($"Host: {host} is not found");
            }

            var keychainItem = keychainAdapter.Credential;
            keychainItem.UpdateToken(token);
        }

        public bool HasKeys => connectionCache.Any();
    }
}