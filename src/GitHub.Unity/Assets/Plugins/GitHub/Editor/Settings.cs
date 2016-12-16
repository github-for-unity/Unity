using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace GitHub.Unity
{
	public class Settings : ScriptableObject
	{
		const string
			SettingsParseError = "Failed to parse settings file at '{0}'",
			RelativeSettingsPath = "{0}/ProjectSettings/{1}",
			LocalSettingsName = "GitHub.local.json",
			TeamSettingsName = "GitHub.json";


		[SerializeField] List<string>
			keys = new List<string>(),
			teamKeys = new List<string>();
		[SerializeField] List<object>
			values = new List<object>(),
			teamValues = new List<object>();


		static Settings asset;


		static string GetLocalPath()
		{
			return string.Format(RelativeSettingsPath, Utility.UnityProjectPath, LocalSettingsName);
		}


		static string GetTeamPath()
		{
			return string.Format(RelativeSettingsPath, Utility.UnityProjectPath, TeamSettingsName);
		}


		static IDictionary<string, object> LoadSettings(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}

			object parseResult;
			IDictionary<string, object> settings;

			if(!SimpleJson.TryDeserializeObject(File.ReadAllText(path), out parseResult) || (settings = parseResult as IDictionary<string, object>) == null)
			{
				Debug.LogErrorFormat(SettingsParseError, path);
				return null;
			}

			return settings;
		}


		public static bool Reload()
		{
			Settings newAsset = CreateInstance<Settings>();

			IDictionary<string, object> settings = LoadSettings(GetLocalPath());

			if (settings != null)
			{
				newAsset.keys.AddRange(settings.Keys);
				newAsset.values.AddRange(settings.Values.Select(
					v => (v is IList<object>) ?
						(object)new List<string>(((IList<object>)v).Select(v2 => v2 as string))
					:
						(object)(v as string)
				));
			}

			settings = LoadSettings(GetTeamPath());

			if (settings != null)
			{
				newAsset.teamKeys.AddRange(settings.Keys);
				newAsset.teamValues.AddRange(settings.Values.Select(
					v => (v is IList<object>) ?
						(object)new List<string>(((IList<object>)v).Select(v2 => v2 as string))
					:
						(object)(v as string)
				));
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

			string path = GetLocalPath();

			StreamWriter settings = File.CreateText(path);
			settings.Write("{\n");

			for (int index = 0; index < asset.keys.Count; ++index)
			{
				List<string> list = asset.values[index] as List<string>;

				if (list == null)
				{
					settings.Write("\t\"{0}\": \"{1}\",\n", Escape(asset.keys[index]), Escape((string)asset.values[index]));
				}
				else
				{
					settings.Write("\t\"{0}\":\n\t[\n", Escape(asset.keys[index]));
					for (int listIndex = 0; listIndex < list.Count; ++listIndex)
					{
						settings.Write("\t\t\"{0}\",\n", Escape(list[listIndex]));
					}
					settings.Write("\t],\n");
				}
			}

			settings.Write("}\n");
			settings.Close();

			return true;
		}


		static string Escape(string unescaped)
		{
			StringBuilder builder = new StringBuilder(unescaped);

			builder.Replace("\\", "\\\\");
			builder.Replace("\"", "\\\"");
			builder.Replace("\n", "\\n");
			builder.Replace("\r", "\\r");
			builder.Replace("\t", "\\t");
			builder.Replace("\b", "\\b");
			builder.Replace("\f", "\\f");

			return builder.ToString();
		}


		static Settings GetAsset()
		{
			if (asset == null)
			{
				Reload();
			}

			return asset;
		}


		public static string Get(string key, string fallback = "")
		{
			Settings asset = GetAsset();

			if (asset == null)
			{
				return fallback;
			}

			int index = asset.teamKeys.IndexOf(key);

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
			Settings asset = GetAsset();

			if (asset == null)
			{
				return false;
			}

			if (asset.teamKeys.Contains(key))
			{
				return false;
			}

			int index = asset.keys.IndexOf(key);

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
			Settings asset = GetAsset();

			if (asset == null)
			{
				return false;
			}

			if (asset.teamKeys.Contains(key))
			{
				return false;
			}

			int index = asset.keys.IndexOf(key);

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
			Settings asset = GetAsset();

			if (asset == null)
			{
				return false;
			}

			if (asset.teamKeys.Contains(key))
			{
				return false;
			}

			int index = asset.keys.IndexOf(key);

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


		static List<string> GetLocalList(string key)
		{
			Settings asset = GetAsset();

			if (asset == null)
			{
				return null;
			}

			int index = asset.keys.IndexOf(key);
			return index < 0 ? null : asset.values[index] as List<string>;
		}


		static List<string> GetTeamList(string key)
		{
			Settings asset = GetAsset();

			if (asset == null)
			{
				return null;
			}

			int index = asset.teamKeys.IndexOf(key);
			return index < 0 ? null : asset.teamValues[index] as List<string>;
		}


		public static int CountElements(string key)
		{
			List<string>
				localList = GetLocalList(key),
				teamList = GetTeamList(key);

			return (localList == null ? 0 : teamList.Count) + (teamList == null ? 0 : teamList.Count);
		}


		public static int GetElementIndex(string key, string value)
		{
			List<string>
				localList = GetLocalList(key),
				teamList = GetTeamList(key);

			int index = (teamList == null ? -1 : teamList.IndexOf(value));
			if (index > -1)
			{
				return index + (localList == null ? 0 : localList.Count);
			}

			return localList == null ? -1 : localList.IndexOf(value);
		}


		public static string GetElement(string key, int index, string fallback = "")
		{
			List<string>
				localList = GetLocalList(key),
				teamList = GetTeamList(key);

			if (index < 0)
			{
				return fallback;
			}

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


		public static bool SetElement(string key, int index, string value, bool noSave = false)
		{
			List<string> localList = GetLocalList(key);

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


		public static bool RemoveElement(string key, string value, bool noSave = false)
		{
			List<string> localList = GetLocalList(key);

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
			List<string> localList = GetLocalList(key);

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


		public static bool AddElement(string key, string value, bool noSave = false)
		{
			Settings asset = GetAsset();

			if (asset == null)
			{
				return false;
			}

			int index = asset.keys.IndexOf(key);

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
	}
}
