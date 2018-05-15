using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public NPath SettingsPath { get; set; }

        protected virtual string SettingsFileName { get; set; }
    }

    class JsonBackedSettings : BaseSettings
    {
        private string cachePath;
        protected Dictionary<string, object> cacheData;
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
            cachePath = SettingsPath.Combine(SettingsFileName);
            LoadFromCache(cachePath);
        }

        public override bool Exists(string key)
        {
            if (cacheData == null)
                Initialize();

            return cacheData.ContainsKey(key);
        }

        public override string Get(string key, string fallback = "")
        {
            return Get<string>(key, fallback);
        }

        public override T Get<T>(string key, T fallback = default(T))
        {
            if (cacheData == null)
                Initialize();

            object value = null;
            if (cacheData.TryGetValue(key, out value))
            {
                if (typeof(T) == typeof(DateTimeOffset))
                {
                    DateTimeOffset dt;
                    if (DateTimeOffset.TryParseExact(value?.ToString(), Constants.Iso8601Formats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        value = dt;
                        cacheData[key] = dt;
                    }
                }

                if (value == null && fallback != null)
                {
                    value = fallback;
                    cacheData[key] = fallback;
                }
                else if (!(value is T))
                {
                    try
                    {
                        value = value.FromObject<T>();
                        cacheData[key] = value;
                    }
                    catch
                    {
                        value = fallback;
                        cacheData[key] = fallback;
                    }
                }
                return (T)value;
            }
            return fallback;
        }

        public override void Set<T>(string key, T value)
        {
            if (cacheData == null)
                Initialize();

            try
            {
                object val = value;
                if (value is DateTimeOffset)
                    val = ((DateTimeOffset)(object)value).ToString(Constants.Iso8601Format);
                if (!cacheData.ContainsKey(key))
                    cacheData.Add(key, val);
                else
                    cacheData[key] = val;
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
            if (cacheData == null)
                Initialize();

            if (cacheData.ContainsKey(key))
                cacheData.Remove(key);
            SaveToCache(cachePath);
        }

        public override void Rename(string oldKey, string newKey)
        {
            if (cacheData == null)
                Initialize();

            object value = null;
            if (cacheData.TryGetValue(oldKey, out value))
            {
                cacheData.Remove(oldKey);
                Set(newKey, value);
            }
            SaveToCache(cachePath);
        }

        protected virtual void LoadFromCache(string path)
        {
            EnsureCachePath(path);

            if (!fileExists(path))
            {
                cacheData = new Dictionary<string, object>();
                return;
            }

            var data = readAllText(path, Encoding.UTF8);

            try
            {
                var c = data.FromJson<Dictionary<string, object>>();
                if (c != null)
                {
                    // upgrade from old format
                    if (c.ContainsKey("GitHubUnity"))
                    {
                        var oldRoot = c["GitHubUnity"];
                        cacheData = oldRoot.FromObject<Dictionary<string, object>>();
                        SaveToCache(path);
                    }
                    else
                        cacheData = c;
                }
                else
                    cacheData = null;
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
                cacheData = new Dictionary<string, object>();
            }
        }

        protected virtual bool SaveToCache(string path)
        {
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
        private const string settingsFileName = "usersettings.json";
        private const string oldSettingsFileName = "settings.json";

        public UserSettings(IEnvironment environment)
        {
            SettingsPath = environment.UserCachePath;
        }

        public override void Initialize()
        {
            var cachePath = SettingsPath.Combine(settingsFileName);
            if (!cachePath.FileExists())
            {
                var oldSettings = SettingsPath.Combine(oldSettingsFileName);
                if (oldSettings.FileExists())
                    oldSettings.Copy(cachePath);
            }
            base.Initialize();
        }

        protected override string SettingsFileName { get { return settingsFileName; } }
    }

    class SystemSettings : JsonBackedSettings
    {
        private const string settingsFileName = "systemsettings.json";
        private const string oldSettingsFileName = "settings.json";

        public SystemSettings(IEnvironment environment)
        {
            SettingsPath = environment.SystemCachePath;
        }

        public override void Initialize()
        {
            var cachePath = SettingsPath.Combine(settingsFileName);
            if (!cachePath.FileExists())
            {
                var oldSettings = SettingsPath.Combine(oldSettingsFileName);
                if (oldSettings.FileExists())
                    oldSettings.Copy(cachePath);
            }
            base.Initialize();
        }

        protected override string SettingsFileName { get { return settingsFileName; } }
    }
}
