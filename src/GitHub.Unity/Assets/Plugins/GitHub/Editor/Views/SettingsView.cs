using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;


namespace GitHub.Unity
{
	class SettingsView : Subview
	{
		const string
			NoActiveRepositoryTitle = "No repository found",
			NoActiveRepositoryMessage = "Your current project is not currently in an active git repository:",
			GitInitBrowseTitle = "Pick desired repository root",
			GitInitButton = "Set up git",
			InvalidInitDirectoryTitle = "Invalid repository root",
			InvalidInitDirectoryMessage = "Your selected folder '{0}' is not a valid repository root for your current project.",
			InvalidInitDirectoryOK = "OK",
			GitInstallTitle = "Git install",
			GitInstallMissingMessage = "GitHub was unable to locate a valid git install. Please specify install location or install git.",
			GitInstallBrowseTitle = "Select git binary",
			GitInstallPickInvalidTitle = "Invalid git install",
			GitInstallPickInvalidMessage = "The selected file is not a valid git install.",
			GitInstallPickInvalidOK = "OK",
			GitInstallFindButton = "Find install",
			GitInstallButton = "New git install",
			GitInstallURL = "http://desktop.github.com",
			RemotesTitle = "Remotes",
			RemoteNameTitle = "Name",
			RemoteUserTitle = "User",
			RemoteHostTitle = "Host",
			RemoteAccessTitle = "Access";


		[SerializeField] List<GitRemote> remotes = new List<GitRemote>();
		[SerializeField] string initDirectory;


		protected override void OnShow()
		{
			GitListRemotesTask.RegisterCallback(OnRemotesUpdate);
		}


		protected override void OnHide()
		{
			GitListRemotesTask.UnregisterCallback(OnRemotesUpdate);
		}


		void OnRemotesUpdate(IList<GitRemote> entries)
		{
			remotes.Clear();
			remotes.AddRange(entries);
			Repaint();
		}


		public override void OnGUI()
		{
			if (!Utility.GitFound)
			{
				Styles.BeginInitialStateArea(GitInstallTitle, GitInstallMissingMessage);
					OnInstallPathGUI();
				Styles.EndInitialStateArea();

				return;
			}
			else if (!Utility.ActiveRepository)
			{
				Styles.BeginInitialStateArea(NoActiveRepositoryTitle, NoActiveRepositoryMessage);
					// Init directory path field
					Styles.PathField(ref initDirectory, () => EditorUtility.OpenFolderPanel(GitInitBrowseTitle, initDirectory, ""), ValidateInitDirectory);

					GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

					// Git init, which starts the config flow
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(GitInitButton, GUILayout.ExpandWidth(false)))
						{
							if (ValidateInitDirectory(initDirectory))
							{
								Init();
							}
							else
							{
								ResetInitDirectory();
							}
						}
					GUILayout.EndHorizontal();
				Styles.EndInitialStateArea();

				return;
			}

			GUILayout.Label("TODO: Favourite branches settings?");

			// Remotes

			float remotesWith = position.width - Styles.RemotesTotalHorizontalMargin;
			float
				nameWidth = remotesWith * Styles.RemotesNameRatio,
				userWidth = remotesWith * Styles.RemotesUserRatio,
				hostWidth = remotesWith * Styles.RemotesHostRation,
				accessWidth = remotesWith * Styles.RemotesAccessRatio;

			GUILayout.Label(RemotesTitle, EditorStyles.boldLabel);
			GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
					GUILayout.Label(RemoteNameTitle, EditorStyles.miniLabel, GUILayout.Width(nameWidth), GUILayout.MaxWidth(nameWidth));
					GUILayout.Label(RemoteUserTitle, EditorStyles.miniLabel, GUILayout.Width(userWidth), GUILayout.MaxWidth(userWidth));
					GUILayout.Label(RemoteHostTitle, EditorStyles.miniLabel, GUILayout.Width(hostWidth), GUILayout.MaxWidth(hostWidth));
					GUILayout.Label(RemoteAccessTitle, EditorStyles.miniLabel, GUILayout.Width(accessWidth), GUILayout.MaxWidth(accessWidth));
				GUILayout.EndHorizontal();

				for (int index = 0; index < remotes.Count; ++index)
				{
					GitRemote remote = remotes[index];
					GUILayout.BeginHorizontal();
						GUILayout.Label(remote.Name, EditorStyles.miniLabel, GUILayout.Width(nameWidth), GUILayout.MaxWidth(nameWidth));
						GUILayout.Label(remote.User, EditorStyles.miniLabel, GUILayout.Width(userWidth), GUILayout.MaxWidth(userWidth));
						GUILayout.Label(remote.Host, EditorStyles.miniLabel, GUILayout.Width(hostWidth), GUILayout.MaxWidth(hostWidth));
						GUILayout.Label(remote.Function.ToString(), EditorStyles.miniLabel, GUILayout.Width(accessWidth), GUILayout.MaxWidth(accessWidth));
					GUILayout.EndHorizontal();
				}
			GUILayout.EndVertical();

			GUILayout.Label("TODO: GitHub login settings");
			GUILayout.Label("TODO: Auto-fetch toggle");
			GUILayout.Label("TODO: Auto-push toggle");

			// Install path

			GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);
			OnInstallPathGUI();
		}


		void OnInstallPathGUI()
		{
			// Install path field
			EditorGUI.BeginChangeCheck();
				string gitInstallPath = Utility.GitInstallPath;
				Styles.PathField(
					ref gitInstallPath,
					() => EditorUtility.OpenFilePanel(
						GitInstallBrowseTitle,
						Path.GetDirectoryName(FindGitTask.DefaultGitPath),
						Path.GetExtension(FindGitTask.DefaultGitPath)
					),
					ValidateGitInstall
				);
			if (EditorGUI.EndChangeCheck())
			{
				Utility.GitInstallPath = gitInstallPath;
			}

			GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

			GUILayout.BeginHorizontal();
				// Find button - for attempting to locate a new install
				if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
				{
					FindGitTask.Schedule(path =>
					{
						if (!string.IsNullOrEmpty(path))
						{
							Utility.GitInstallPath = path;
							GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
						}
					});
				}
				GUILayout.FlexibleSpace();

				// Install button if git is not installed or we want a new install
				if (GUILayout.Button(GitInstallButton, GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(GitInstallURL);
				}
			GUILayout.EndHorizontal();
		}


		void ResetInitDirectory()
		{
			initDirectory = Utility.UnityDataPath.Substring(0, Utility.UnityDataPath.Length - "Assets".Length);
			GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
		}


		static bool ValidateInitDirectory(string path)
		{
			if (Utility.UnityDataPath.IndexOf(path) != 0)
			{
				EditorUtility.DisplayDialog(
					InvalidInitDirectoryTitle,
					string.Format(InvalidInitDirectoryMessage, path),
					InvalidInitDirectoryOK
				);

				return false;
			}

			return true;
		}


		static bool ValidateGitInstall(string path)
		{
			if (!FindGitTask.ValidateGitInstall(path))
			{
				EditorUtility.DisplayDialog(
					GitInstallPickInvalidTitle,
					string.Format(GitInstallPickInvalidMessage, path),
					GitInstallPickInvalidOK
				);

				return false;
			}

			return true;
		}


		void Init()
		{
			Debug.LogFormat("TODO: Init '{0}'", initDirectory);
		}
	}
}
