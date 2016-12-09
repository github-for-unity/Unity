using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Object = UnityEngine.Object;


namespace GitHub.Unity
{
	[Flags]
	enum ProjectSettingsEvaluation
	{
		None = 						0,
		EditorSettingsMissing = 	1 << 0,
		BadVCSSettings = 			1 << 1,
		BinarySerialization = 		1 << 2,
		MixedSerialization  =		1 << 3
	}


	enum GitIgnoreRuleEffect
	{
		Require = 0,
		Disallow = 1
	}


	abstract class ProjectConfigurationIssue
	{}


	class ProjectSettingsIssue : ProjectConfigurationIssue
	{
		public ProjectSettingsEvaluation Evaluation { get; protected set; }


		public ProjectSettingsIssue(ProjectSettingsEvaluation evaluation)
		{
			Evaluation = evaluation;
		}


		public bool WasCaught(ProjectSettingsEvaluation evaluation)
		{
			return (Evaluation & evaluation) != 0;
		}
	}


	class GitIgnoreException : ProjectConfigurationIssue
	{
		public Exception Exception { get; protected set; }


		public GitIgnoreException(Exception exception)
		{
			Exception = exception;
		}
	}


	class GitIgnoreIssue : ProjectConfigurationIssue
	{
		public string File { get; protected set; }
		public string Line { get; protected set; }
		public string Description { get; protected set; }


		public GitIgnoreIssue(string file, string line, string description)
		{
			File = file;
			Line = line;
			Description = description;
		}
	}


	struct GitIgnoreRule
	{
		const string
			CountKey = "GitIgnoreRuleCount",
			EffectKey = "GitIgnoreRule{0}Effect",
			FileKey = "GitIgnoreRule{0}File",
			LineKey = "GitIgnoreRule{0}Line",
			TriggetTextKey = "GitIgnoreRule{0}TriggerText";


		public static int Count
		{
			get
			{
				return Mathf.Max(0, int.Parse(Settings.Get(GitIgnoreRule.CountKey, "0")));
			}
		}


		public GitIgnoreRuleEffect Effect { get; private set; }
		public Regex File { get; private set; }
		public Regex Line { get; private set; }
		public string TriggerText { get; private set; }


		public static bool TryLoad(int index, out GitIgnoreRule result)
		{
			result = new GitIgnoreRule();

			int effect;
			if (!int.TryParse(Settings.Get(string.Format(EffectKey, index), "-1"), out effect) || effect < 0)
			{
				return false;
			}
			result.Effect = (GitIgnoreRuleEffect)effect;

			string file = Settings.Get(string.Format(FileKey, index));
			if (string.IsNullOrEmpty(file))
			{
				return false;
			}
			result.File = new Regex(file);

			string line = Settings.Get(string.Format(LineKey, index));
			if (string.IsNullOrEmpty(line))
			{
				return false;
			}
			result.Line = new Regex(line);

			result.TriggerText = Settings.Get(string.Format(TriggetTextKey, index));

			return !string.IsNullOrEmpty(result.TriggerText);
		}


		public override string ToString()
		{
			return string.Format("{0} \"{1}\" in \"{2}\": {3}", Effect, Line, File, TriggerText);
		}
	}


	class EvaluateProjectConfigurationTask : ITask
	{
		enum SerializationSetting
		{
			Mixed = 0,
			ForceBinary = 1,
			ForceText = 2
		}


		struct GitIgnoreFile
		{
			public string Path { get; private set; }
			public string[] Contents { get; private set; }


			public GitIgnoreFile(string path)
			{
				Path = path.Substring(Utility.GitRoot.Length + 1);
				Contents = File.ReadAllLines(path).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
			}


			public override string ToString()
			{
				return string.Format("{0}:\n{1}", Path, string.Join("\n", Contents));
			}
		}


		public static string EditorSettingsPath = "ProjectSettings/EditorSettings.asset";


		const string
			GitIgnoreFilePattern = ".gitignore",
			VCSPropertyName = "m_ExternalVersionControlSupport",
			SerializationPropertyName = "m_SerializationMode",
			VisibleMetaFilesValue = "Visible Meta Files",
			HiddenMetaFilesValue = "Hidden Meta Files";

		const int ThreadSyncDelay = 100;


		static Action<IEnumerable<ProjectConfigurationIssue>> onEvaluationResult;


		public static void RegisterCallback(Action<IEnumerable<ProjectConfigurationIssue>> callback)
		{
			onEvaluationResult += callback;
		}


		public static void UnregisterCallback(Action<IEnumerable<ProjectConfigurationIssue>> callback)
		{
			onEvaluationResult -= callback;
		}


		public static void Schedule()
		{
			Tasks.Add(new EvaluateProjectConfigurationTask());
		}


		public static Object LoadEditorSettings()
		{
			return UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(EditorSettingsPath).FirstOrDefault();
		}


		List<ProjectConfigurationIssue> issues = new List<ProjectConfigurationIssue>();


		public bool Blocking { get { return false; } }
		public float Progress { get; protected set; }
		public bool Done { get; protected set; }
		public TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
		public bool Critical { get { return false; } }
		public bool Cached { get { return false; } }
		public Action<ITask> OnBegin { set; protected get; }
		public Action<ITask> OnEnd { set; protected get; }
		public string Label { get { return "Project Evaluation"; } }


		public void Run()
		{
			Done = false;
			Progress = 0f;

			issues.Clear();

			if(OnBegin != null)
			{
				OnBegin(this);
			}

			// Unity project config
			Tasks.ScheduleMainThread(EvaluateLocalConfiguration);

			// Git config
			EvaluateGitIgnore();

			// Wait for main thread work to complete
			while(!Done) { Thread.Sleep(ThreadSyncDelay); }

			Progress = 1f;
			Done = true;

			if(OnEnd != null)
			{
				OnEnd(this);
			}

			if (onEvaluationResult != null)
			{
				onEvaluationResult(issues);
			}
		}


		void EvaluateLocalConfiguration()
		{
			ProjectSettingsEvaluation result = ProjectSettingsEvaluation.None;

			Object settingsAsset = LoadEditorSettings();
			if (settingsAsset == null)
			{
				result |= ProjectSettingsEvaluation.EditorSettingsMissing;
				return;
			}
			SerializedObject settingsObject = new SerializedObject(settingsAsset);

			string vcsSetting = settingsObject.FindProperty(VCSPropertyName).stringValue;
			if (!vcsSetting.Equals(VisibleMetaFilesValue) && !vcsSetting.Equals(HiddenMetaFilesValue))
			{
				result |= ProjectSettingsEvaluation.BadVCSSettings;
			}

			SerializationSetting serializationSetting = (SerializationSetting)settingsObject.FindProperty(SerializationPropertyName).intValue;
			if (serializationSetting == SerializationSetting.ForceBinary)
			{
				result |= ProjectSettingsEvaluation.BinarySerialization;
			}
			else if (serializationSetting == SerializationSetting.Mixed)
			{
				result |= ProjectSettingsEvaluation.MixedSerialization;
			}

			if (result != ProjectSettingsEvaluation.None)
			{
				issues.Add(new ProjectSettingsIssue(result));
			}

			Done = true;
		}


		void EvaluateGitIgnore()
		{
			// Read rules
			List<GitIgnoreRule> rules = new List<GitIgnoreRule>(GitIgnoreRule.Count);
			for (int index = 0; index < rules.Capacity; ++index)
			{
				GitIgnoreRule rule;
				if (GitIgnoreRule.TryLoad(index, out rule))
				{
					rules.Add(rule);
				}
			}

			if (!rules.Any())
			{
				return;
			}

			// Read gitignore files
			GitIgnoreFile[] files;
			try
			{
				files = Directory.GetFiles(Utility.GitRoot, GitIgnoreFilePattern, SearchOption.AllDirectories).Select(p => new GitIgnoreFile(p)).ToArray();

				if (files.Length < 1)
				{
					return;
				}
			}
			catch (Exception e)
			{
				issues.Add(new GitIgnoreException(e));

				return;
			}

			// Evaluate each rule
			for (int ruleIndex = 0; ruleIndex < rules.Count; ++ruleIndex)
			{
				GitIgnoreRule rule = rules[ruleIndex];
				for (int fileIndex = 0; fileIndex < files.Length; ++fileIndex)
				{
					GitIgnoreFile file = files[fileIndex];
					// Check against all files with matching path
					if (!rule.File.IsMatch(file.Path))
					{
						continue;
					}

					// Validate all lines in that file
					for (int lineIndex = 0; lineIndex < file.Contents.Length; ++lineIndex)
					{
						string line = file.Contents[lineIndex];
						bool match = rule.Line.IsMatch(line);
						bool broken = false;

						if (rule.Effect == GitIgnoreRuleEffect.Disallow && match)
						// This line is not allowed
						{
							issues.Add(new GitIgnoreIssue(file.Path, line, rule.TriggerText));
						}
						else if (rule.Effect == GitIgnoreRuleEffect.Require)
						// If the line is required, see if we're there
						{
							if (match)
							// We found it! No sense in searching further in this file.
							{
								break;
							}
							else if (lineIndex == file.Contents.Length - 1)
							// We reached the last line without finding it
							{
								issues.Add(new GitIgnoreIssue(file.Path, string.Empty, rule.TriggerText));
							}
						}
					}
				}
			}
		}


		public void Abort()
		{
			Done = true;
		}


		public void Disconnect() {}
		public void Reconnect() {}
		public void WriteCache(TextWriter cache) {}
	}
}
