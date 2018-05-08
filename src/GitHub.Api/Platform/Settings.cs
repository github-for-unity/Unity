using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitHub.Unity
{
    abstract class BaseSettings : ISettings
    {
        public abstract bool Exists(string key);
        public abstract string Get(string key, string fallback = "");
        public abstract T Get<T>(string key, T fallback = default(T));
        public abstract void Initialize();
        public abstract void Rename(string oldKey, string newKey);
        public abstract void Set<T>(string key, T value);
        public abstract void Unset(string key);

        protected virtual string SettingsFileName { get; set; }
        protected NPath SettingsPath { get; set; }
    }

    class JsonBackedSettings : BaseSettings
    {
        private string cachePath;
        private CacheData cacheData = new CacheData();
        private Action<string> dirCreate;
        private Func<string, bool> dirExists;
        private Action<string> fileDelete;
        private Func<string, bool> fileExists;
        private Func<string, Encoding, string> readAllText;
        private Action<string, string> writeAllText;
        private readonly ILogging logger;

        public JsonBackedSettings()
        {
            logger = LogHelper.GetLogger(GetType());
            fileExists = (path) => File.Exists(path);
            readAllText = (path, encoding) => File.ReadAllText(path, encoding);
            writeAllText = (path, content) => File.WriteAllText(path, content);
            fileDelete = (path) => File.Delete(path);
            dirExists = (path) => Directory.Exists(path);
            dirCreate = (path) => Directory.CreateDirectory(path);
        }

        public override void Initialize()
        {
            logger.Debug($"Initializing settings {GetType()}");
            cachePath = SettingsPath.Combine(SettingsFileName);

            logger.Debug("Initializing settings file at {0}", cachePath);
            LoadFromCache(cachePath);
        }

        public override bool Exists(string key)
        {
            return cacheData.GitHubUnity.ContainsKey(key);
        }

        public override string Get(string key, string fallback = "")
        {
            return Get<string>(key, fallback);
        }

        public override T Get<T>(string key, T fallback = default(T))
        {
            object value = null;
            if (cacheData.GitHubUnity.TryGetValue(key, out value))
            {
                logger.Debug("Get: {0}", key);
                return (T)value;
            }

            logger.Debug("Miss: {0}", key);
            return fallback;
        }

        public override void Set<T>(string key, T value)
        {
            try
            {
                logger.Trace("Set: {0}", key);

                if (!cacheData.GitHubUnity.ContainsKey(key))
                    cacheData.GitHubUnity.Add(key, value);
                else
                    cacheData.GitHubUnity[key] = value;
                SaveToCache(cachePath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error storing to cache");
                throw;
            }
        }

        public override void Unset(string key)
        {
            if (cacheData.GitHubUnity.ContainsKey(key))
                cacheData.GitHubUnity.Remove(key);
            SaveToCache(cachePath);
        }

        public override void Rename(string oldKey, string newKey)
        {
            object value = null;
            if (cacheData.GitHubUnity.TryGetValue(oldKey, out value))
            {
                cacheData.GitHubUnity.Remove(oldKey);
                Set(newKey, value);
            }
            SaveToCache(cachePath);
        }

        private void LoadFromCache(string path)
        {
            logger.Trace("LoadFromCache: {0}", path);

            EnsureCachePath(path);

            if (!fileExists(path))
                return;

            var data = readAllText(path, Encoding.UTF8);

            try
            {
                cacheData = data.FromJson<CacheData>();
            }
            catch(Exception ex)
            {
                logger.Error(ex, "LoadFromCache Error");
                cacheData = null;
            }

            if (cacheData == null)
            {
                // cache is corrupt, remove
                fileDelete(path);
                return;
            }
        }

        private bool SaveToCache(string path)
        {
            logger.Trace("SaveToCache: {0}", path);

            EnsureCachePath(path);

            try
            {
                var data = cacheData.ToJson();
                writeAllText(path, data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "SaveToCache Error");
                return false;
            }

            return true;
        }

        private void EnsureCachePath(string path)
        {
            if (fileExists(path))
                return;

            var di = Path.GetDirectoryName(path);
            if (!dirExists(di))
                dirCreate(di);
        }

        private class CacheData
        {
            public Dictionary<string, object> GitHubUnity { get; set; } = new Dictionary<string, object>();
        }

    }

    class LocalSettings : JsonBackedSettings
    {
        private const string RelativeSettingsPath = "ProjectSettings";
        private const string settingsFileName = "GitHub.local.json";

        public LocalSettings(IEnvironment environment)
        {
            SettingsPath = environment.UnityProjectPath.Combine(RelativeSettingsPath);
        }

        protected override string SettingsFileName { get { return settingsFileName; } }
    }

    class UserSettings : JsonBackedSettings
    {
        private const string settingsFileName = "settings.json";

        public UserSettings(IEnvironment environment)
        {
            SettingsPath = environment.UserCachePath;
        }

        protected override string SettingsFileName { get { return settingsFileName; } }
    }

    class SystemSettings : JsonBackedSettings
    {
        private const string settingsFileName = "settings.json";

        public SystemSettings(IEnvironment environment)
        {
            SettingsPath = environment.SystemCachePath;
        }

        protected override string SettingsFileName { get { return settingsFileName; } }
    }
}
