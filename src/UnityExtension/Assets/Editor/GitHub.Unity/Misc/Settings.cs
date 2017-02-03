using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class Settings : ISettings
    {
        private const string SettingsParseError = "Failed to parse settings file at '{0}'";
        private const string RelativeSettingsPath = "{0}/ProjectSettings/{1}";
        private const string LocalSettingsName = "GitHub.local.json";
        private readonly string localCachePath;

        private CacheData cacheData = new CacheData();
        private Action<string> dirCreate;
        private Func<string, bool> dirExists;
        private Action<string> fileDelete;

        private Func<string, bool> fileExists;
        private Func<string, Encoding, string> readAllText;
        private Action<string, string> writeAllText;

        public Settings()
        {
            fileExists = (path) => File.Exists(path);
            readAllText = (path, encoding) => File.ReadAllText(path, encoding);
            writeAllText = (path, content) => File.WriteAllText(path, content);
            fileDelete = (path) => File.Delete(path);
            dirExists = (path) => Directory.Exists(path);
            dirCreate = (path) => Directory.CreateDirectory(path);
            localCachePath = String.Format(RelativeSettingsPath, Utility.UnityProjectPath, LocalSettingsName);
        }

        public void Initialize()
        {
            LoadFromCache(localCachePath);
        }

        public bool Exists(string key)
        {
            return cacheData.LocalSettings.ContainsKey(key);
        }

        public string Get(string key, string fallback = "")
        {
            return Get<string>(key, fallback);
        }

        public T Get<T>(string key, T fallback = default(T))
        {
            object value = null;
            if (cacheData.LocalSettings.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return fallback;
        }

        public void Set<T>(string key, T value)
        {
            if (!cacheData.LocalSettings.ContainsKey(key))
                cacheData.LocalSettings.Add(key, value);
            else
                cacheData.LocalSettings[key] = value;
            SaveToCache(localCachePath);
        }

        public void Unset(string key)
        {
            if (cacheData.LocalSettings.ContainsKey(key))
                cacheData.LocalSettings.Remove(key);
            SaveToCache(localCachePath);
        }

        public void Rename(string oldKey, string newKey)
        {
            object value = null;
            if (cacheData.LocalSettings.TryGetValue(oldKey, out value))
            {
                cacheData.LocalSettings.Remove(oldKey);
                Set(newKey, value);
            }
            SaveToCache(localCachePath);
        }

        private void LoadFromCache(string cachePath)
        {
            EnsureCachePath(cachePath);

            if (!fileExists(cachePath))
                return;

            var data = readAllText(cachePath, Encoding.UTF8);

            try
            {
                cacheData = SimpleJson.DeserializeObject<CacheData>(data);
            }
            catch
            {
                cacheData = null;
            }

            if (cacheData == null)
            {
                // cache is corrupt, remove
                fileDelete(cachePath);
                return;
            }
        }

        private bool SaveToCache(string cachePath)
        {
            EnsureCachePath(cachePath);

            try
            {
                var data = SimpleJson.SerializeObject(cacheData);
                writeAllText(cachePath, data);
            }
            catch (Exception ex)
            {
                Logger.Error(SettingsParseError, cachePath);
                Logger.Debug("{0}", ex);
                return false;
            }

            return true;
        }

        private void EnsureCachePath(string cachePath)
        {
            if (fileExists(cachePath))
                return;

            var di = Path.GetDirectoryName(cachePath);
            if (!dirExists(di))
                dirCreate(di);
        }

        private class CacheData
        {
            public Dictionary<string, object> LocalSettings = new Dictionary<string, object>();
        }
    }
}
