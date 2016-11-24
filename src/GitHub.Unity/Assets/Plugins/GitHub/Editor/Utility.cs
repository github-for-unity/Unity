using UnityEngine;
using UnityEditor;
using System.IO;


namespace GitHub.Unity
{
	class Utility
	{
		public static string GitRoot { get; protected set; }
		public static string UnityDataPath { get; protected set; }


		[InitializeOnLoadMethod]
		static void Prepare()
		{
			GitRoot = FindRoot(UnityDataPath = Application.dataPath);
		}


		// TODO: replace with libgit2sharp call
		static string FindRoot(string path)
		{
			if (string.IsNullOrEmpty(Path.GetDirectoryName(path)))
			{
				return UnityDataPath;
			}

			if (Directory.Exists(Path.Combine(path, ".git")))
			{
				return path;
			}

			return FindRoot(Directory.GetParent(path).FullName);
		}
	}
}
