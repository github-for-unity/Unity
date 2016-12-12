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
			GitIgnoreRulesTitle = "gitignore rules",
			GitIgnoreRulesEffect = "Effect",
			GitIgnoreRulesFile = "File",
			GitIgnoreRulesLine = "Line",
			GitIgnoreRulesDescription = "Description",
			NewGitIgnoreRuleButton = "New",
			DeleteGitIgnoreRuleButton = "Delete",
			RemotesTitle = "Remotes",
			RemoteNameTitle = "Name",
			RemoteUserTitle = "User",
			RemoteHostTitle = "Host",
			RemoteAccessTitle = "Access";


		[SerializeField] List<GitRemote> remotes = new List<GitRemote>();
		[SerializeField] string initDirectory;
		[SerializeField] int gitIgnoreRulesSelection = 0;
		[SerializeField] Vector2 scroll;


		int newGitIgnoreRulesSelection = -1;


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
			scroll = GUILayout.BeginScrollView(scroll);
				// Issues

				if (!OnIssuesGUI())
				{
					return;
				}

				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

				GUILayout.Label("TODO: Favourite branches settings?");

				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

				// Remotes

				OnRemotesGUI();

				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

				// gitignore rules list

				OnGitIgnoreRulesGUI();

				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

				GUILayout.Label("TODO: GitHub login settings");
				GUILayout.Label("TODO: Auto-fetch toggle");
				GUILayout.Label("TODO: Auto-push toggle");

				// Install path

				GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);
				OnInstallPathGUI();
			GUILayout.EndScrollView();

			// Effectuate new selection at end of frame
			if (Event.current.type == EventType.Repaint && newGitIgnoreRulesSelection > -1)
			{
				gitIgnoreRulesSelection = newGitIgnoreRulesSelection;
				newGitIgnoreRulesSelection = -1;
				GUIUtility.hotControl = GUIUtility.keyboardControl = -1;
			}
		}


		bool OnIssuesGUI()
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

					return false;
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

					return false;
				}
			}

			if (!Utility.GitFound)
			{
				Styles.BeginInitialStateArea(GitInstallTitle, GitInstallMissingMessage);
					OnInstallPathGUI();
				Styles.EndInitialStateArea();

				return false;
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

				return false;
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

			return true;
		}


		void OnRemotesGUI()
		{
			float remotesWith = position.width - Styles.RemotesTotalHorizontalMargin - 16f;
			float
				nameWidth = remotesWith * Styles.RemotesNameRatio,
				userWidth = remotesWith * Styles.RemotesUserRatio,
				hostWidth = remotesWith * Styles.RemotesHostRation,
				accessWidth = remotesWith * Styles.RemotesAccessRatio;

			GUILayout.Label(RemotesTitle, EditorStyles.boldLabel);
			GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
					TableCell(RemoteNameTitle, nameWidth);
					TableCell(RemoteUserTitle, userWidth);
					TableCell(RemoteHostTitle, hostWidth);
					TableCell(RemoteAccessTitle, accessWidth);
				GUILayout.EndHorizontal();

				for (int index = 0; index < remotes.Count; ++index)
				{
					GitRemote remote = remotes[index];
					GUILayout.BeginHorizontal();
						TableCell(remote.Name, nameWidth);
						TableCell(remote.User, userWidth);
						TableCell(remote.Host, hostWidth);
						TableCell(remote.Function.ToString(), accessWidth);
					GUILayout.EndHorizontal();
				}
			GUILayout.EndVertical();
		}


		void OnGitIgnoreRulesGUI()
		{
			float gitignoreRulesWith = position.width - Styles.GitIgnoreRulesTotalHorizontalMargin - Styles.GitIgnoreRulesSelectorWidth - 16f;
			float
				effectWidth = gitignoreRulesWith * Styles.GitIgnoreRulesEffectRatio,
				fileWidth = gitignoreRulesWith * Styles.GitIgnoreRulesFileRatio,
				lineWidth = gitignoreRulesWith * Styles.GitIgnoreRulesLineRatio;

			GUILayout.Label(GitIgnoreRulesTitle, EditorStyles.boldLabel);
			GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
					GUILayout.Space(Styles.GitIgnoreRulesSelectorWidth);
					TableCell(GitIgnoreRulesEffect, effectWidth);
					TableCell(GitIgnoreRulesFile, fileWidth);
					TableCell(GitIgnoreRulesLine, lineWidth);
				GUILayout.EndHorizontal();

				int count = GitIgnoreRule.Count;
				for (int index = 0; index < count; ++index)
				{
					GitIgnoreRule rule;
					if (GitIgnoreRule.TryLoad(index, out rule))
					{
						GUILayout.BeginHorizontal();
							GUILayout.Space(Styles.GitIgnoreRulesSelectorWidth);

							if (gitIgnoreRulesSelection == index && Event.current.type == EventType.Repaint)
							{
								Rect selectorRect = GUILayoutUtility.GetLastRect();
								selectorRect.Set(selectorRect.x, selectorRect.y + 2f, selectorRect.width - 2f, EditorGUIUtility.singleLineHeight);
								EditorStyles.foldout.Draw(selectorRect, false, false, false, false);
							}

							TableCell(rule.Effect.ToString(), effectWidth);
							// TODO: Tint if the regex is null
							TableCell(rule.FileString, fileWidth);
							TableCell(rule.LineString, lineWidth);
						GUILayout.EndHorizontal();

						if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
						{
							newGitIgnoreRulesSelection = index;
							Event.current.Use();
						}
					}
				}

				GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (GUILayout.Button(NewGitIgnoreRuleButton, EditorStyles.miniButton))
					{
						GitIgnoreRule.New();
						GUIUtility.hotControl = GUIUtility.keyboardControl = -1;
					}
				GUILayout.EndHorizontal();

				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

				// Selected gitignore rule edit

				GitIgnoreRule selectedRule;
				if (GitIgnoreRule.TryLoad(gitIgnoreRulesSelection, out selectedRule))
				{
					GUILayout.BeginVertical(GUI.skin.box);
						GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if (GUILayout.Button(DeleteGitIgnoreRuleButton, EditorStyles.miniButton))
							{
								GitIgnoreRule.Delete(gitIgnoreRulesSelection);
								newGitIgnoreRulesSelection = gitIgnoreRulesSelection - 1;
							}
						GUILayout.EndHorizontal();
						EditorGUI.BeginChangeCheck();
							GitIgnoreRuleEffect newEffect = (GitIgnoreRuleEffect)EditorGUILayout.EnumPopup(GitIgnoreRulesEffect, selectedRule.Effect);
							string newFile = EditorGUILayout.TextField(GitIgnoreRulesFile, selectedRule.FileString);
							string newLine = EditorGUILayout.TextField(GitIgnoreRulesLine, selectedRule.LineString);
							GUILayout.Label(GitIgnoreRulesDescription);
							string newDescription = EditorGUILayout.TextArea(selectedRule.TriggerText, Styles.CommitDescriptionFieldStyle);
						if (EditorGUI.EndChangeCheck())
						{
							GitIgnoreRule.Save(gitIgnoreRulesSelection, newEffect, newFile, newLine, newDescription);
							EvaluateProjectConfigurationTask.Schedule();
						}
					GUILayout.EndVertical();
				}
			GUILayout.EndVertical();
		}


		static void TableCell(string label, float width)
		{
			GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(width), GUILayout.MaxWidth(width));
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
