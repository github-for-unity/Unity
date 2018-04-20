using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitHub.Logging;

namespace GitHub.Unity
{
    [Serializable]
    public class Connection
    {
        public string Host { get; set; }
        public string Username { get; set; }
        [NonSerialized] internal GitHubUser User;

        // for json serialization
        public Connection()
        {
        }

        public Connection(string host, string username)
        {
            Host = host;
            Username = username;
        }

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

    class Keychain : IKeychain
    {
        const string ConnectionFile = "connections.json";

        private readonly ILogging logger = LogHelper.GetLogger<Keychain>();
        private readonly ICredentialManager credentialManager;
        private readonly NPath cachePath;
        // cached credentials loaded from git to pass to GitHub/ApiClient
        private readonly Dictionary<UriString, KeychainAdapter> keychainAdapters = new Dictionary<UriString, KeychainAdapter>();

        // loaded at the start of application from cached/serialized data
        private readonly Dictionary<UriString, Connection> connections = new Dictionary<UriString, Connection>();

        public event Action ConnectionsChanged;

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
            Guard.ArgumentNotNull(host, nameof(host));

            return FindOrCreateAdapter(host);
        }

        public async Task<IKeychainAdapter> Load(UriString host)
        {
            Guard.ArgumentNotNull(host, nameof(host));

            var keychainAdapter = FindOrCreateAdapter(host);
            var connection = GetConnection(host);

            //logger.Trace($@"Loading KeychainAdapter Host:""{host}"" Cached Username:""{cachedConnection.Username}""");

            var keychainItem = await credentialManager.Load(host);
            if (keychainItem == null)
            {
                logger.Warning("Cannot load host from Credential Manager; removing from cache");
                await Clear(host, false);
                keychainAdapter = null;
            }
            else
            {
                if (keychainItem.Username != connection.Username)
                {
                    logger.Warning("Keychain Username:\"{0}\" does not match cached Username:\"{1}\"; Hopefully it works", keychainItem.Username, connection.Username);
                }

                //logger.Trace("Loaded from Credential Manager Host:\"{0}\" Username:\"{1}\"", keychainItem.Host, keychainItem.Username); 
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
            //logger.Trace("Initialize");
            LoadConnectionsFromDisk();
        }

        public async Task Clear(UriString host, bool deleteFromCredentialManager)
        {
            //logger.Trace("Clear Host:{0}", host);

            Guard.ArgumentNotNull(host, nameof(host));
        
            //clear octokit credentials
            await RemoveCredential(host, deleteFromCredentialManager);
            RemoveConnection(host);
        }

        public async Task Save(UriString host)
        {
            //logger.Trace("Save: {0}", host);

            Guard.ArgumentNotNull(host, nameof(host));

            var keychainAdapter = await AddCredential(host);
            AddConnection(new Connection(host, keychainAdapter.Credential.Username));
        }

        public void SetCredentials(ICredential credential)
        {
            //logger.Trace("SetCredentials Host:{0}", credential.Host);

            Guard.ArgumentNotNull(credential, nameof(credential));

            var keychainAdapter = GetKeychainAdapter(credential.Host);
            keychainAdapter.Set(credential);
        }

        public void SetToken(UriString host, string token, string username)
        {
            //logger.Trace("SetToken Host:{0}", host);

            Guard.ArgumentNotNull(host, nameof(host));
            Guard.ArgumentNotNull(token, nameof(token));
            Guard.ArgumentNotNull(username, nameof(username));

            var keychainAdapter = GetKeychainAdapter(host);
            keychainAdapter.UpdateToken(token, username);
        }

        private void LoadConnectionsFromDisk()
        {
            //logger.Trace("ReadCacheFromDisk Path:{0}", cachePath.ToString());
            if (cachePath.FileExists())
            {
                var json = cachePath.ReadAllText();
                try
                {
                    var conns = SimpleJson.DeserializeObject<Connection[]>(json);
                    UpdateConnections(conns);
                }
                catch (IOException ex)
                {
                    logger.Error(ex, "Error reading connection cache: {0}", cachePath);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error deserializing connection cache: {0}", cachePath);
                    // try to fix the corrupted file with the data we have
                    SaveConnectionsToDisk(raiseChangedEvent: false);
                }
            }
        }

        private void SaveConnectionsToDisk(bool raiseChangedEvent = true)
        {
            //logger.Trace("WriteCacheToDisk Count:{0} Path:{1}", connectionCache.Count, cachePath.ToString());
            try
            {
                var json = SimpleJson.SerializeObject(connections.Values.ToArray());
                cachePath.WriteAllText(json);
            }
            catch (IOException ex)
            {
                logger.Error(ex, "Error writing connection cache: {0}", cachePath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error serializing connection cache: {0}", cachePath);
            }

            if (raiseChangedEvent)
                ConnectionsChanged?.Invoke();
        }

        private KeychainAdapter GetKeychainAdapter(UriString host)
        {
            KeychainAdapter credentialAdapter;
            if (!keychainAdapters.TryGetValue(host, out credentialAdapter))
            {
                throw new ArgumentException($"{host} is not found", nameof(host));
            }
            return credentialAdapter;
        }

        private async Task<KeychainAdapter> AddCredential(UriString host)
        {
            var keychainAdapter = GetKeychainAdapter(host);
            if (string.IsNullOrEmpty(keychainAdapter.Credential.Token))
            {
                throw new InvalidOperationException("Anonymous credentials cannot be stored");
            }

            // saves credential in git credential manager (host, username, token)
            await credentialManager.Delete(host);
            await credentialManager.Save(keychainAdapter.Credential);
            return keychainAdapter;
        }

        private async Task RemoveCredential(UriString host, bool deleteFromCredentialManager)
        {
            KeychainAdapter k;
            if (keychainAdapters.TryGetValue(host, out k))
            {
                k.Clear();
                keychainAdapters.Remove(host);
            }

            if (deleteFromCredentialManager)
            {
                await credentialManager.Delete(host);
            }
        }

        private Connection GetConnection(UriString host)
        {
            if (!connections.ContainsKey(host))
                throw new ArgumentException($"{host} is not found", nameof(host));
            return connections[host];
        }

        private void AddConnection(Connection connection)
        {
            // create new connection in the connection cache for this host
            if (connections.ContainsKey(connection.Host))
                connections[connection.Host] = connection;
            else
                connections.Add(connection.Host, connection);
            SaveConnectionsToDisk();
        }

        private void RemoveConnection(UriString host)
        {
            // create new connection in the connection cache for this host
            if (connections.ContainsKey(host))
            {
                connections.Remove(host);
                SaveConnectionsToDisk();
            }
        }

        private void UpdateConnections(Connection[] conns)
        {
            var updated = false;
            // remove all the connections we have in memory that are no longer in the connection cache file
            foreach (var host in connections.Values.Except(conns).Select(x => x.Host).ToArray())
            {
                connections.Remove(host);
                updated = true;
            }

            // update existing connections and add new ones from the cache file
            foreach (var connection in conns)
            {
                if (connections.ContainsKey(connection.Host))
                    connections[connection.Host] = connection;
                else
                    connections.Add(connection.Host, connection);
                updated = true;
            }
            if (updated)
                ConnectionsChanged?.Invoke();
        }

        public Connection[] Connections => connections.Values.ToArray();
        public IList<UriString> Hosts => connections.Keys.ToArray();
        public bool HasKeys => connections.Any();
    }
}