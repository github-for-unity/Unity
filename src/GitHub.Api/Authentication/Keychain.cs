using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace GitHub.Unity
{
    public struct Connection
    {
        public UriString Host;
        public string Username;

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (Host?.GetHashCode() ?? 0);
            hash = hash * 23 + (Username?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is Connection)
                return Equals((Connection)other);
            return false;
        }

        public bool Equals(Connection other)
        {
            return
                object.Equals(Host, other.Host) &&
                String.Equals(Username, other.Username)
                ;
        }

        public static bool operator ==(Connection lhs, Connection rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Connection lhs, Connection rhs)
        {
            return !(lhs == rhs);
        }
    }

    class ConnectionCacheItem
    {
        public string Host { get; set; }
        public string Username { get; set; }
    }

    class Keychain : IKeychain
    {
        const string ConnectionFile = "connections.json";

        private readonly ILogging logger = Logging.GetLogger<Keychain>();

        private readonly ICredentialManager credentialManager;
        private readonly NPath cachePath;

        // loaded at the start of application from cached/serialized data
        private Dictionary<UriString, Connection> connectionCache = new Dictionary<UriString, Connection>();

        // cached credentials loaded from git to pass to GitHub/ApiClient
        private readonly Dictionary<UriString, KeychainAdapter> keychainAdapters
            = new Dictionary<UriString, KeychainAdapter>();

        public Keychain(IEnvironment environment, ICredentialManager credentialManager)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));
            Guard.ArgumentNotNull(credentialManager, nameof(credentialManager));

            Guard.NotNull(environment, environment.UserCachePath, nameof(environment.UserCachePath));

            cachePath = environment.UserCachePath.Combine(ConnectionFile);
            this.credentialManager = credentialManager;
        }

        public IKeychainAdapter Connect(UriString host)
        {
            return FindOrCreateAdapter(host);
        }

        public async Task<IKeychainAdapter> Load(UriString host)
        {
            KeychainAdapter keychainAdapter = FindOrCreateAdapter(host);
            var cachedConnection = connectionCache[host];

            logger.Trace($@"Loading KeychainAdapter Host:""{host}"" Cached Username:""{cachedConnection.Username}""");

            var keychainItem = await credentialManager.Load(host);

            if (keychainItem == null)
            {
                logger.Warning("Cannot load host from Credential Manager; removing from cache");
                await Clear(host, false);
            }
            else
            {
                if (keychainItem.Username != cachedConnection.Username)
                {
                    logger.Warning("Keychain Username:\"{0}\" does not match cached Username:\"{1}\"; Hopefully it works", keychainItem.Username, cachedConnection.Username);
                }

                logger.Trace("Loaded from Credential Manager Host:\"{0}\" Username:\"{1}\"", keychainItem.Host, keychainItem.Username); 

                keychainAdapter.Set(keychainItem);
            }

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
                    connections.Select(item => {
                                    logger.Trace("ReadCacheFromDisk Item Host:{0} Username:{1}", item.Host, item.Username);
                                    return new Connection { Host = new UriString(item.Host), Username = item.Username };
                               })
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
                        new ConnectionCacheItem {
                            Host = pair.Value.Host.ToString(),
                            Username = pair.Value.Username
                        }).ToArray();

            var json = SimpleJson.SerializeObject(connectionCacheItems);
            cachePath.WriteAllText(json);
        }

        public async Task Clear(UriString host, bool deleteFromCredentialManager)
        {
            logger.Trace("Clear Host:{0}", host);

            // delete connection in the connection list
            connectionCache.Remove(host);

            //clear octokit credentials
            FindOrCreateAdapter(host).Clear();

            WriteCacheToDisk();

            if (deleteFromCredentialManager)
            {
                await credentialManager.Delete(host);
            }
        }

        public async Task Save(UriString host)
        {
            logger.Trace("Save: {0}", host);

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

        public void SetCredentials(ICredential credential)
        {
            logger.Trace("SetCredentials Host:{0}", credential.Host);

            var keychainAdapter = keychainAdapters[credential.Host];
            keychainAdapter.Set(credential);
        }

        public void SetToken(UriString host, string token)
        {
            logger.Trace("SetToken Host:{0}", host);

            var keychainAdapter = keychainAdapters[host];
            keychainAdapter.UpdateToken(token);
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

        public Connection[] Connections => connectionCache.Values.ToArray();

        public IList<UriString> Hosts => connectionCache.Keys.ToArray();

        public bool HasKeys => connectionCache.Any();

        public bool NeedsLoad => HasKeys && FindOrCreateAdapter(connectionCache.First().Value.Host).OctokitCredentials == Credentials.Anonymous;
    }
}