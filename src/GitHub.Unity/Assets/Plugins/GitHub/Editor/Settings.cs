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
			LocalSettingsName = "GitHub.local.json";


		[SerializeField] List<string> keys = new List<string>();
		[SerializeField] List<object> values = new List<object>();


		static Settings asset;


		static string GetPath()
		{
			return string.Format("{0}/ProjectSettings/{1}", Utility.UnityProjectPath, LocalSettingsName);
		}


		public static bool Reload()
		{
			string path = GetPath();

			Settings newAsset = CreateInstance<Settings>();

			if (!File.Exists(path))
			{
				asset = newAsset;
				return true;
			}

			object parseResult;
			IDictionary<string, object> settings;

			if(!SimpleJson.TryDeserializeObject(File.ReadAllText(path), out parseResult) || (settings = parseResult as IDictionary<string, object>) == null)
			{
				Debug.LogErrorFormat(SettingsParseError, path);
				return false;
			}

			newAsset.keys.Clear();
			newAsset.keys.AddRange(settings.Keys);
			newAsset.values.Clear();
			newAsset.values.AddRange(settings.Values.Select(
				v => (v is IList<object>) ?
					(object)new List<string>(((IList<object>)v).Select(v2 => v2 as string))
				:
					(object)(v as string)
			));

			asset = newAsset;

			return true;
		}


		public static bool Save()
		{
			if (asset == null)
			{
				return false;
			}

			string path = GetPath();

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

			int index = asset.keys.IndexOf(key);

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


		static List<string> GetList(string key)
		{
			Settings asset = GetAsset();

			if (asset == null)
			{
				return null;
			}

			int index = asset.keys.IndexOf(key);
			return index < 0 ? null : asset.values[index] as List<string>;
		}


		public static int CountElements(string key)
		{
			List<string> list = GetList(key);
			return list == null ? 0 : list.Count;
		}


		public static int GetElementIndex(string key, string value)
		{
			List<string> list = GetList(key);
			return list == null ? -1 : list.IndexOf(value);
		}


		public static string GetElement(string key, int index, string fallback = "")
		{
			List<string> list = GetList(key);
			return (list == null || index >= list.Count || index < 0) ? fallback : list[index];
		}


		public static bool SetElement(string key, int index, string value, bool noSave = false)
		{
			List<string> list = GetList(key);

			if (list == null || index >= list.Count || index < 0)
			{
				return false;
			}

			list[index] = value;

			if (!noSave)
			{
				Save();
			}

			return true;
		}


		public static bool RemoveElement(string key, string value, bool noSave = false)
		{
			List<string> list = GetList(key);

			if (list == null)
			{
				return false;
			}

			list.Remove(value);

			if (!noSave)
			{
				Save();
			}

			return true;
		}


		public static bool RemoveElementAt(string key, int index, bool noSave = false)
		{
			List<string> list = GetList(key);

			if (list == null || index >= list.Count || index < 0)
			{
				return false;
			}

			list.RemoveAt(index);

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
