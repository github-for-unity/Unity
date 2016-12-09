using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;


namespace GitHub.Unity
{
	class Utility : ScriptableObject
	{
		static bool ready = false;
		static Action onReady;


		public static string GitInstallPath
		{
			get
			{
				return Settings.Get("GitInstallPath");
			}
			set
			{
				Settings.Set("GitInstallPath", value);
			}
		}


		public static string GitRoot { get; protected set; }
		public static string UnityAssetsPath { get; protected set; }
		public static string UnityProjectPath { get; protected set; }
		public static string ExtensionInstallPath { get; protected set; }
		public static List<ProjectConfigurationIssue> Issues { get; protected set; }


		public static bool GitFound
		{
			get
			{
				return !string.IsNullOrEmpty(GitInstallPath);
			}
		}


		public static bool ActiveRepository
		{
			get
			{
				return !string.IsNullOrEmpty(GitRoot);
			}
		}


		public static bool IsWindows
		{
			get
			{
				switch (Application.platform)
				{
					case RuntimePlatform.WindowsPlayer:
					case RuntimePlatform.WindowsEditor:
					return true;
					default:
					return false;
				}
			}
		}


		public static void RegisterReadyCallback(Action callback)
		{
			onReady += callback;
			if (ready)
			{
				callback();
			}
		}


		public static void UnregisterReadyCallback(Action callback)
		{
			onReady -= callback;
		}


		[InitializeOnLoadMethod]
		static void Prepare()
		{
			// Unity paths
			UnityAssetsPath = Application.dataPath;
			UnityProjectPath = UnityAssetsPath.Substring(0, UnityAssetsPath.Length - "Assets".Length - 1);

			// Juggling to find out where we got installed
			Utility instance = FindObjectOfType(typeof(Utility)) as Utility;
			if (instance == null)
			{
				instance = CreateInstance<Utility>();
			}
			MonoScript script = MonoScript.FromScriptableObject(instance);
			if (script == null)
			{
				ExtensionInstallPath = string.Empty;
			}
			else
			{
				ExtensionInstallPath = AssetDatabase.GetAssetPath(script);
				ExtensionInstallPath = ExtensionInstallPath.Substring(0, ExtensionInstallPath.LastIndexOf('/'));
				ExtensionInstallPath = ExtensionInstallPath.Substring(0, ExtensionInstallPath.LastIndexOf('/'));
			}
			DestroyImmediate(instance);

			// Evaluate project settings
			EvaluateProjectConfigurationTask.UnregisterCallback(OnEvaluationResult);
			EvaluateProjectConfigurationTask.RegisterCallback(OnEvaluationResult);
			EvaluateProjectConfigurationTask.Schedule();

			// Root paths
			if (string.IsNullOrEmpty(GitInstallPath))
			{
				FindGitTask.Schedule(path =>
				{
					if (!string.IsNullOrEmpty(path))
					{
						GitInstallPath = path;
						DetermineGitRoot();
						OnPrepareCompleted();
					}
				});
			}
			else
			{
				DetermineGitRoot();
				OnPrepareCompleted();
			}
		}


		static void OnPrepareCompleted()
		{
			ready = true;
			if (onReady != null)
			{
				onReady();
			}
		}


		static void OnEvaluationResult(IEnumerable<ProjectConfigurationIssue> result)
		{
			Issues = new List<ProjectConfigurationIssue>(result);
		}


		static void DetermineGitRoot()
		{
			GitRoot = FindRoot(UnityAssetsPath);
		}


		// TODO: replace with libgit2sharp call
		static string FindRoot(string path)
		{
			if (string.IsNullOrEmpty(Path.GetDirectoryName(path)))
			{
				return null;
			}

			if (Directory.Exists(Path.Combine(path, ".git")))
			{
				return path;
			}

			return FindRoot(Directory.GetParent(path).FullName);
		}


		public static string RepositoryPathToAbsolute(string repositoryPath)
		{
			return Path.Combine(Utility.GitRoot, repositoryPath);
		}


		public static string RepositoryPathToAsset(string repositoryPath)
		{
			string localDataPath = UnityAssetsPath.Substring(GitRoot.Length + 1);
			return (repositoryPath.IndexOf (localDataPath) == 0) ?
				("Assets" + repositoryPath.Substring(localDataPath.Length)).Replace(Path.DirectorySeparatorChar, '/') :
				null;
		}


		public static string AssetPathToRepository(string assetPath)
		{
			string localDataPath = UnityAssetsPath.Substring(GitRoot.Length + 1);
			return Path.Combine(localDataPath.Substring(0, localDataPath.Length - "Assets".Length), assetPath.Replace('/', Path.DirectorySeparatorChar));
		}


		public static void ParseLines(StringBuilder buffer, Action<string> lineParser, bool parseAll)
		{
			int end = buffer.Length - 1;

			if(!parseAll)
			// Try to avoid partial lines unless asked not to
			{
				for(; end > 0 && buffer[end] != '\n'; --end);
			}

			if(end > 0)
			// Parse lines if we have any buffer to parse
			{
				for(int index = 0, last = -1; index <= end; ++index)
				{
					if(buffer[index] == '\n')
					{
						int start = last + 1;
						// TODO: Figure out how we get out of doing that ToString call
						string line = buffer.ToString(start, index - start);
						lineParser(line);
						last = index;
					}
				}

				buffer.Remove(0, end + 1);
			}
		}


		public static Texture2D GetIcon(string filename)
		{
			return AssetDatabase.LoadMainAssetAtPath(ExtensionInstallPath + "/Icons/" + filename) as Texture2D;
		}


		// Based on: https://www.rosettacode.org/wiki/Find_common_directory_path#C.23
		public static string FindCommonPath(string separator, IEnumerable<string> paths)
		{
			string commonPath = string.Empty;
			List<string> separatedPath = paths
				.First(first => first.Length == paths.Max(second => second.Length))
				.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries)
				.ToList();

			foreach (string pathSegment in separatedPath.AsEnumerable())
			{
				string pathExtension = pathSegment + separator;

				if (commonPath.Length == 0 && paths.All(path => path.StartsWith(pathExtension)))
				{
					commonPath = pathExtension;
				}
				else if (paths.All(path => path.StartsWith(commonPath + pathExtension)))
				{
					commonPath += pathExtension;
				}
				else
				{
					break;
				}
			}

			return commonPath;
		}
	}
}
