using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;


namespace GitHub.Unity
{
	public class Settings : ScriptableObject
	{
		[SerializeField] List<string>
			keys = new List<string>(),
			values = new List<string>();


		static Settings GetAsset()
		{
			string path = Utility.ExtensionInstallPath + "/GitHub.local.asset";

			Settings asset = AssetDatabase.LoadMainAssetAtPath(path) as Settings;

			if (asset == null)
			{
				AssetDatabase.CreateAsset(CreateInstance<Settings>(), path);
				asset = AssetDatabase.LoadMainAssetAtPath(path) as Settings;
			}

			return asset;
		}


		public static string Get(string key)
		{
			Settings asset = GetAsset();
			int index = asset.keys.IndexOf(key);

			if (index >= 0)
			{
				return asset.values[index];
			}

			return string.Empty;
		}


		public static void Set(string key, string value)
		{
			Settings asset = GetAsset();
			int index = asset.keys.IndexOf(key);

			if (index >= 0)
			{
				asset.values[index] = value;

				EditorUtility.SetDirty(asset);
				return;
			}

			asset.keys.Add(key);
			asset.values.Add(value);

			EditorUtility.SetDirty(asset);
		}
	}
}
