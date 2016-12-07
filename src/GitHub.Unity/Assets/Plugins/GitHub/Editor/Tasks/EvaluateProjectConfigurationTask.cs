using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Threading;
using System.Linq;

using Object = UnityEngine.Object;


namespace GitHub.Unity
{
	[Flags]
	enum ProjectEvaluation
	{
		None = 						0,
		EditorSettingsMissing = 	1 << 0,
		BadVCSSettings = 			1 << 1,
		BinarySerialization = 		1 << 2,
		MixedSerialization  =		1 << 3
	}


	class EvaluateProjectConfigurationTask : ITask
	{
		enum SerializationSetting
		{
			Mixed = 0,
			ForceBinary = 1,
			ForceText = 2
		}


		public static string EditorSettingsPath = "ProjectSettings/EditorSettings.asset";


		const string
			VCSPropertyName = "m_ExternalVersionControlSupport",
			SerializationPropertyName = "m_SerializationMode",
			VisibleMetaFilesValue = "Visible Meta Files",
			HiddenMetaFilesValue = "Hidden Meta Files";

		const int ThreadSyncDelay = 100;


		static Action<ProjectEvaluation> onEvaluationResult;


		public static void RegisterCallback(Action<ProjectEvaluation> callback)
		{
			onEvaluationResult += callback;
		}


		public static void UnregisterCallback(Action<ProjectEvaluation> callback)
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


		ProjectEvaluation result;


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

			result = ProjectEvaluation.None;

			if(OnBegin != null)
			{
				OnBegin(this);
			}

			Tasks.ScheduleMainThread(EvaluateLocalConfiguration);
			while(!Done) { Thread.Sleep(ThreadSyncDelay); }

			Progress = 1f;
			Done = true;

			if(OnEnd != null)
			{
				OnEnd(this);
			}

			if (onEvaluationResult != null)
			{
				onEvaluationResult(result);
			}
		}


		void EvaluateLocalConfiguration()
		{
			Object settingsAsset = LoadEditorSettings();
			if (settingsAsset == null)
			{
				result |= ProjectEvaluation.EditorSettingsMissing;
				return;
			}
			SerializedObject settingsObject = new SerializedObject(settingsAsset);

			string vcsSetting = settingsObject.FindProperty(VCSPropertyName).stringValue;
			if (!vcsSetting.Equals(VisibleMetaFilesValue) && !vcsSetting.Equals(HiddenMetaFilesValue))
			{
				result |= ProjectEvaluation.BadVCSSettings;
			}

			SerializationSetting serializationSetting = (SerializationSetting)settingsObject.FindProperty(SerializationPropertyName).intValue;
			if (serializationSetting == SerializationSetting.ForceBinary)
			{
				result |= ProjectEvaluation.BinarySerialization;
			}
			else if (serializationSetting == SerializationSetting.Mixed)
			{
				result |= ProjectEvaluation.MixedSerialization;
			}

			Done = true;
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
