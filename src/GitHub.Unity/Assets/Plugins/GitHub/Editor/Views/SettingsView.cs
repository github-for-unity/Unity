using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using  System.Linq;


namespace GitHub.Unity
{
	[System.Serializable]
	class SettingsView : Subview
	{
		const string
			EditorSettingsMissingTitle = "Missing editor settings",
			EditorSettingsMissingMessage = "No valid editor settings found when looking in expected path '{0}'. Please save the project.",
			BadVCSSettingsTitle = "Update settings",
			BadVCSSettingsMessage = "To use git, you will need to set project Version Control Mode to either 'Visible Meta Files' or 'Hidden Meta Files'.",
			SelectEditorSettingsButton = "View settings",
			NoActiveRepositoryTitle = "No repository found",
			NoActiveRepositoryMessage = "Your current project is not currently in an active git repository:",
			TextSerialisationMessage = "For optimal git use, it is recommended that you configure Unity to serialize assets using text serialization. Note that this may cause editor slowdowns for projects with very large datasets.",
			BinarySerialisationMessage = "This project is currently configured for binary serialization.",
			MixedSerialisationMessage = "This project is currently configured for mixed serialization.",
			GitIgnoreExceptionWarning = "Exception when searching .gitignore files: {0}",
			GitIgnoreIssueWarning = "{0}: {2}\n\nIn line \"{1}\"",
			GitIgnoreIssueNoLineWarning = "{0}: {1}",
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


		public override void Refresh()
		{
			GitListRemotesTask.Schedule();
			GitStatusTask.Schedule();
		}


		void OnRemotesUpdate(IList<GitRemote> entries)
		{
			remotes.Clear();
			remotes.AddRange(entries);
			Repaint();
		}


		public override void OnGUI()
		{
			ProjectSettingsIssue settingsIssues = Utility.Issues.Select(i => i as ProjectSettingsIssue).FirstOrDefault(i => i != null);

			if (settingsIssues != null)
			{
				if (settingsIssues.WasCaught(ProjectSettingsEvaluation.EditorSettingsMissing))
				{
					Styles.BeginInitialStateArea(
						EditorSettingsMissingTitle,
						string.Format(EditorSettingsMissingMessage, EvaluateProjectConfigurationTask.EditorSettingsPath)
					);
					Styles.EndInitialStateArea();

					return;
				}
				else if (settingsIssues.WasCaught(ProjectSettingsEvaluation.BadVCSSettings))
				{
					Styles.BeginInitialStateArea(BadVCSSettingsTitle, BadVCSSettingsMessage);
						GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

						// Button to select editor settings - for remedying the bad setting
						if (Styles.InitialStateActionButton(SelectEditorSettingsButton))
						{
							Selection.activeObject = EvaluateProjectConfigurationTask.LoadEditorSettings();
						}
					Styles.EndInitialStateArea();

					return;
				}
			}

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
					if (Styles.InitialStateActionButton(GitInitButton))
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
				Styles.EndInitialStateArea();

				return;
			}

			if (settingsIssues != null)
			{
				if (
					settingsIssues.WasCaught(ProjectSettingsEvaluation.BinarySerialization) ||
					settingsIssues.WasCaught(ProjectSettingsEvaluation.MixedSerialization)
				)
				{
					GUILayout.Label(TextSerialisationMessage, Styles.LongMessageStyle);
				}

				if (settingsIssues.WasCaught(ProjectSettingsEvaluation.BinarySerialization))
				{
					Styles.Warning(BinarySerialisationMessage);
					if (Styles.InitialStateActionButton(SelectEditorSettingsButton))
					{
						Selection.activeObject = EvaluateProjectConfigurationTask.LoadEditorSettings();
					}
				}
				else if (settingsIssues.WasCaught(ProjectSettingsEvaluation.MixedSerialization))
				{
					Styles.Warning(MixedSerialisationMessage);
					if (Styles.InitialStateActionButton(SelectEditorSettingsButton))
					{
						Selection.activeObject = EvaluateProjectConfigurationTask.LoadEditorSettings();
					}
				}
			}

			GitIgnoreException gitIgnoreException = Utility.Issues.Select(i => i as GitIgnoreException).FirstOrDefault(i => i != null);
			if (gitIgnoreException != null)
			{
				Styles.Warning(string.Format(GitIgnoreExceptionWarning, gitIgnoreException.Exception));
			}

			foreach (GitIgnoreIssue issue in Utility.Issues.Select(i => i as GitIgnoreIssue).Where(i => i != null))
			{
				if (string.IsNullOrEmpty(issue.Line))
				{
					Styles.Warning(string.Format(GitIgnoreIssueNoLineWarning, issue.File, issue.Description));
				}
				else
				{
					Styles.Warning(string.Format(GitIgnoreIssueWarning, issue.File, issue.Line, issue.Description));
				}
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
			initDirectory = Utility.UnityProjectPath;
			GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
		}


		static bool ValidateInitDirectory(string path)
		{
			if (Utility.UnityProjectPath.IndexOf(path) != 0)
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
