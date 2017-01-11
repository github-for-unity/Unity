using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GitHub.Unity
{
    public class Settings : ScriptableObject
    {
        private const string SettingsParseError = "Failed to parse settings file at '{0}'";
        private const string RelativeSettingsPath = "{0}/ProjectSettings/{1}";
        private const string LocalSettingsName = "GitHub.local.json";
        private const string TeamSettingsName = "GitHub.json";

        private static Settings asset;

        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<string> teamKeys = new List<string>();
        [SerializeField] private List<object> teamValues = new List<object>();
        [SerializeField] private List<object> values = new List<object>();

        public static bool Reload()
        {
            var newAsset = CreateInstance<Settings>();

            var settings = LoadSettings(GetLocalPath());

            if (settings != null)
            {
                newAsset.keys.AddRange(settings.Keys);
                newAsset.values.AddRange(settings.Values.Select(v => v is IList<object>
                    ? (object)new List<string>(((IList<object>)v).Select(v2 => v2 as string))
                    : (object)(v as string)));
            }

            settings = LoadSettings(GetTeamPath());

            if (settings != null)
            {
                newAsset.teamKeys.AddRange(settings.Keys);
                newAsset.teamValues.AddRange(settings.Values.Select(v => v is IList<object>
                    ? (object)new List<string>(((IList<object>)v).Select(v2 => v2 as string))
                    : (object)(v as string)));
            }

            asset = newAsset;

            return true;
        }

        public static bool Save()
        {
            if (asset == null)
            {
                return false;
            }

            var path = GetLocalPath();

            var settings = File.CreateText(path);
            settings.WriteLine("{");

            for (var index = 0; index < asset.keys.Count; ++index)
            {
                var list = asset.values[index] as List<string>;

                if (list == null)
                {
                    settings.WriteLine("\t\"{0}\": \"{1}\",", Escape(asset.keys[index]), Escape((string)asset.values[index]));
                }
                else
                {
                    settings.WriteLine("\t\"{0}\":\n\t[", Escape(asset.keys[index]));
                    for (var listIndex = 0; listIndex < list.Count; ++listIndex)
                    {
                        settings.WriteLine("\t\t\"{0}\",", Escape(list[listIndex]));
                    }

                    settings.WriteLine("\t],");
                }
            }

            settings.WriteLine("}");
            settings.Close();

            return true;
        }

        public static string Get(string key, string fallback = "")
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return fallback;
            }

            var index = asset.teamKeys.IndexOf(key);

            if (index >= 0)
            {
                return asset.teamValues[index] as string;
            }

            index = asset.keys.IndexOf(key);

            if (index >= 0)
            {
                return asset.values[index] as string;
            }

            return fallback;
        }

        public static bool Set(string key, string value, bool noSave = false)
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return false;
            }

            if (asset.teamKeys.Contains(key))
            {
                return false;
            }

            var index = asset.keys.IndexOf(key);

            if (index >= 0)
            {
                if (!asset.values[index].Equals(value))
                {
                    asset.values[index] = value;

                    Save();
                }

                return true;
            }

            asset.keys.Add(key);
            asset.values.Add(value);

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static bool Unset(string key, bool noSave = false)
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return false;
            }

            if (asset.teamKeys.Contains(key))
            {
                return false;
            }

            var index = asset.keys.IndexOf(key);

            if (index < 0)
            {
                return false;
            }

            asset.keys.RemoveAt(index);
            asset.values.RemoveAt(index);

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static bool Rename(string key, string newKey, bool noSave = false)
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return false;
            }

            if (asset.teamKeys.Contains(key))
            {
                return false;
            }

            var index = asset.keys.IndexOf(key);

            if (index < 0)
            {
                return false;
            }

            asset.keys[index] = newKey;

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static bool AddElement(string key, string value, bool noSave = false)
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return false;
            }

            var index = asset.keys.IndexOf(key);

            List<string> list = null;

            if (index < 0)
            {
                list = new List<string>();
                asset.keys.Add(key);
                asset.values.Add(list);
            }
            else
            {
                asset.values[index] = list = asset.values[index] as List<string> ?? new List<string>();
            }

            list.Add(value);

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static bool RemoveElement(string key, string value, bool noSave = false)
        {
            var localList = GetLocalList(key);

            if (localList == null)
            {
                return false;
            }

            localList.Remove(value);

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static bool RemoveElementAt(string key, int index, bool noSave = false)
        {
            var localList = GetLocalList(key);

            if (localList == null || index >= localList.Count || index < 0)
            {
                return false;
            }

            localList.RemoveAt(index);

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static string GetElement(string key, int index, string fallback = "")
        {
            if (index < 0)
            {
                return fallback;
            }

            var localList = GetLocalList(key);
            var teamList = GetTeamList(key);

            if (localList != null && index < localList.Count)
            {
                return localList[index];
            }

            if (teamList != null && index < teamList.Count + (localList == null ? 0 : localList.Count))
            {
                return teamList[index];
            }

            return fallback;
        }

        public static int GetElementIndex(string key, string value)
        {
            var localList = GetLocalList(key);
            var teamList = GetTeamList(key);

            var index = teamList == null ? -1 : teamList.IndexOf(value);
            if (index > -1)
            {
                return index + (localList == null ? 0 : localList.Count);
            }

            return localList == null ? -1 : localList.IndexOf(value);
        }

        public static bool SetElement(string key, int index, string value, bool noSave = false)
        {
            var localList = GetLocalList(key);

            if (localList == null || index >= localList.Count || index < 0)
            {
                return false;
            }

            localList[index] = value;

            if (!noSave)
            {
                Save();
            }

            return true;
        }

        public static int CountElements(string key)
        {
            var localList = GetLocalList(key);
            var teamList = GetTeamList(key);

            return (localList == null ? 0 : teamList.Count) + (teamList == null ? 0 : teamList.Count);
        }

        private static string GetLocalPath()
        {
            return String.Format(RelativeSettingsPath, Utility.UnityProjectPath, LocalSettingsName);
        }

        private static string GetTeamPath()
        {
            return String.Format(RelativeSettingsPath, Utility.UnityProjectPath, TeamSettingsName);
        }

        private static IDictionary<string, object> LoadSettings(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            object parseResult;
            IDictionary<string, object> settings;

            if (!SimpleJson.TryDeserializeObject(File.ReadAllText(path), out parseResult) ||
                (settings = parseResult as IDictionary<string, object>) == null)
            {
                Debug.LogErrorFormat(SettingsParseError, path);
                return null;
            }

            return settings;
        }

        private static string Escape(string unescaped)
        {
            var builder = new StringBuilder(unescaped);

            builder.Replace("\\", "\\\\");
            builder.Replace("\"", "\\\"");
            builder.Replace("\n", "\\n");
            builder.Replace("\r", "\\r");
            builder.Replace("\t", "\\t");
            builder.Replace("\b", "\\b");
            builder.Replace("\f", "\\f");

            return builder.ToString();
        }

        private static Settings GetAsset()
        {
            if (asset == null)
            {
                Reload();
            }

            return asset;
        }

        private static List<string> GetLocalList(string key)
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return null;
            }

            var index = asset.keys.IndexOf(key);
            return index < 0 ? null : asset.values[index] as List<string>;
        }

        private static List<string> GetTeamList(string key)
        {
            var asset = GetAsset();

            if (asset == null)
            {
                return null;
            }

            var index = asset.teamKeys.IndexOf(key);
            return index < 0 ? null : asset.teamValues[index] as List<string>;
        }
    }
}
