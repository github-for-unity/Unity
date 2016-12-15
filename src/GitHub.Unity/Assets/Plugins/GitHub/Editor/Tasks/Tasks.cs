using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System;
using System.Linq;


/*

	Scenarios:
	Quitting mid-operation both a Blocking and a Critical task.
	Implement escalating re-start delay on the main queue pumpt.
	Detect bad state quit - prompt to resume or halt operations.
	Detect git lock, offer to run cleanup or halt operations.
	Implement ability to resume after halted operations.
	Implement ability to skip a long fetch operation, so we can keep working.

*/


namespace GitHub.Unity
{
	enum TaskQueueSetting
	{
		NoQueue,
		Queue,
		QueueSingle
	}


	interface ITask
	{
		bool Blocking { get; }
		float Progress { get; }
		bool Done { get; }
		TaskQueueSetting Queued { get; }
		bool Critical { get; }
		bool Cached { get; }
		Action<ITask> OnBegin { set; }
		Action<ITask> OnEnd { set; }
		string Label { get; }
		void Run();
		void Abort();
		void Disconnect();
		void Reconnect();
		void WriteCache(TextWriter cache);
	};


	enum CachedTask
	{
		TestTask,
		ProcessTask
	};


	enum FailureSeverity
	{
		Moderate,
		Critical
	};


	class Tasks
	{
		enum WaitMode
		{
			Background,
			Modal,
			Blocking
		};


		delegate void ProgressBarDisplayMethod(string text, float progress);


		internal const string
			TypeKey = "type",
			ProcessKey = "process";


		const int
			NoTasksSleep = 100,
			BlockingTaskWaitSleep = 10,
			FailureDelayDefault = 1,
			FailureDelayLong = 5000;
		const string
			CacheFileName = "GitHubCache",
			QuitActionFieldName = "editorApplicationQuit",
			TaskThreadExceptionRestartError = "GitHub task thread restarting after encountering an exception: {0}",
			TaskCacheWriteExceptionError = "GitHub: Exception when writing task cache: {0}",
			TaskCacheParseError = "GitHub: Failed to parse task cache",
			TaskParseUnhandledTypeError = "GitHub: Trying to parse unhandled cached task: {0}",
			TaskFailureTitle = "GitHub",
			TaskFailureMessage = "{0} failed:\n{1}",
			TaskFailureOK = "OK",
			TaskProgressTitle = "GitHub",
			TaskBlockingTitle = "Critical GitHub task",
			TaskBlockingDescription = "A critical GitHub task ({0}) has yet to complete. What would you like to do?",
			TaskBlockingComplete = "Complete",
			TaskBlockingInterrupt = "Interrupt";
		const BindingFlags kQuitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;


		static FieldInfo quitActionField;
		static ProgressBarDisplayMethod displayBackgroundProgressBar;
		static Action clearBackgroundProgressBar;


		static Tasks Instance { get; set; }
		static string CacheFilePath { get; set; }


		static void SecureQuitActionField()
		{
			if(quitActionField == null)
			{
				quitActionField = typeof(EditorApplication).GetField(QuitActionFieldName, kQuitActionBindingFlags);

				if(quitActionField == null)
				{
					throw new NullReferenceException("Unable to reflect EditorApplication." + QuitActionFieldName);
				}
			}
		}


		static UnityAction editorApplicationQuit
		{
			get
			{
				SecureQuitActionField();
				return (UnityAction)quitActionField.GetValue(null);
			}
			set
			{
				SecureQuitActionField();
				quitActionField.SetValue(null, value);
			}
		}


		// "Everything is broken - let's rebuild from the ashes (read: cache)"
		[InitializeOnLoadMethod]
		static void OnLoad()
		{
			Instance = new Tasks();
		}


		bool running = false;
		Thread thread;
		ITask activeTask;
		Queue<ITask> tasks;
		object tasksLock = new object();
		Exception lastException;


		Tasks()
		{
			editorApplicationQuit = (UnityAction)Delegate.Combine(editorApplicationQuit, new UnityAction(OnQuit));
			CacheFilePath = Path.Combine(Application.dataPath, Path.Combine("..", Path.Combine("Temp", CacheFileName)));
			EditorApplication.playmodeStateChanged += () =>
			{
				if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
				{
					OnPlaymodeEnter();
				}
			};

			tasks = new Queue<ITask>();
			if(File.Exists(CacheFilePath))
			{
				ReadCache();
				File.Delete(CacheFilePath);

				OnSessionRestarted();
			}

			thread = new Thread(Start);
			thread.Start();
		}


		public static void Add(ITask task)
		{
			lock(Instance.tasksLock)
			{
				if(
					(task.Queued == TaskQueueSetting.NoQueue && Instance.tasks.Count > 0) ||
					(task.Queued == TaskQueueSetting.QueueSingle && Instance.tasks.Any(t => t.GetType() == task.GetType()))
				)
				{
					return;
				}

				Instance.tasks.Enqueue(task);
			}

			Instance.WriteCache();
		}


		void Start()
		{
			while(true)
			{
				try
				{
					Run();

					break;
				}
				catch(ThreadAbortException)
				// Aborted by domain unload or explicitly via the editor quit handler. Button down the hatches.
				{
					running = false;

					// Disconnect or abort the active task
					if(activeTask != null && !activeTask.Done)
					{
						if(activeTask.Cached)
						{
							try
							{
								activeTask.Disconnect();
							}
							finally
							{
								activeTask = null;
							}
						}
						else
						{
							try
							{
								activeTask.Abort();
							}
							finally
							{
								activeTask = null;
							}
						}
					}

					break;
				}
				catch(Exception e)
				// Something broke internally - reboot
				{
					running = false;
					bool repeat = lastException != null && e.TargetSite.Equals(lastException.TargetSite);
					lastException = e;

					if(!repeat)
					{
						Debug.LogErrorFormat(TaskThreadExceptionRestartError, e);
						Thread.Sleep(FailureDelayDefault);
					}
					else
					{
						Thread.Sleep(FailureDelayLong);
					}
				}
			}
		}


		void OnPlaymodeEnter()
		// About to enter playmode
		{
			if(activeTask != null)
			{
				ClearBackgroundProgressBar();
				EditorUtility.ClearProgressBar();
			}
		}


		void OnSessionRestarted()
		// A recompile or playmode enter/exit cause the script environment to reload while we had tasks at hand
		{
			ClearBackgroundProgressBar();
			EditorUtility.ClearProgressBar();
			if(activeTask != null)
			{
				activeTask.Reconnect();
			}
		}


		void OnQuit()
		{
			// Stop the queue
			running = false;

			if(activeTask != null && activeTask.Critical)
			{
				WaitForTask(activeTask, WaitMode.Blocking);
			}
		}


		void Run()
		{
			running = true;

			while(running)
			{
				// Clear any completed task
				if(activeTask != null && activeTask.Done)
				{
					activeTask = null;
				}

				// Grab a new task
				if(activeTask == null)
				{
					lock(tasksLock)
					{
						if(tasks.Count > 0)
						{
							activeTask = tasks.Dequeue();
							activeTask.OnBegin = task => ScheduleMainThread(WriteCache);
						}
					}
				}

				if(activeTask != null)
				// Run and monitor active task
				{
					ScheduleMainThread(() =>
					{
						if(activeTask != null)
						{
							WaitForTask(activeTask, activeTask.Blocking ? WaitMode.Modal : WaitMode.Background);
						}
					});

					activeTask.Run();
					WriteCache();
				}
				else
				// Wait for something to do
				{
					Thread.Sleep(NoTasksSleep);
				}
			}

			thread.Abort();
		}


		void WriteCache()
		{
			try
			{
				StreamWriter cache = File.CreateText(CacheFilePath);
				cache.Write("[");

				// Cache the active task
				if(activeTask != null && !activeTask.Done && activeTask.Cached)
				{
					activeTask.WriteCache(cache);
				}
				else
				{
					cache.Write("false");
				}

				// Cache the queue
				lock(tasksLock)
				{
					foreach(ITask task in tasks)
					{
						if(!task.Cached)
						{
							continue;
						}

						cache.Write(",\n");
						task.WriteCache(cache);
					}
				}

				cache.Write("]");
				cache.Close();
			}
			catch(Exception e)
			{
				Debug.LogErrorFormat(TaskCacheWriteExceptionError, e);
			}
		}


		bool ReadCache()
		{
			string text = File.ReadAllText(CacheFilePath);

			object parseResult;
			IList<object> cache;

			// Parse root list with at least one item (active task) or fail
			if(!SimpleJson.TryDeserializeObject(text, out parseResult) || (cache = parseResult as IList<object>) == null || cache.Count < 1)
			{
				Debug.LogError(TaskCacheParseError);
				return false;
			}

			// Parse active task
			IDictionary<string, object> taskData = cache[0] as IDictionary<string, object>;
			ITask cachedActiveTask = (taskData != null) ? ParseTask(taskData) : null;

			// Parse tasks list or fail
			Queue<ITask> cachedTasks = new Queue<ITask>(cache.Count - 1);
			for(int index = 1; index < cache.Count; ++index)
			{
				taskData = cache[index] as IDictionary<string, object>;

				if(taskData == null)
				{
					Debug.LogError(TaskCacheParseError);
					return false;
				}

				cachedTasks.Enqueue(ParseTask(taskData));
			}

			// Apply everything only after fully successful parse
			activeTask = cachedActiveTask;
			tasks = cachedTasks;

			return true;
		}


		ITask ParseTask(IDictionary<string, object> data)
		{
			CachedTask type;

			try
			{
				type = (CachedTask)Enum.Parse(typeof(CachedTask), (string)data[TypeKey]);
			}
			catch(Exception)
			{
				return null;
			}

			try
			{
				switch(type)
				{
					case CachedTask.TestTask:
					return TestTask.Parse(data);
					case CachedTask.ProcessTask:
					return ProcessTask.Parse(data);
					default:
						Debug.LogErrorFormat(TaskParseUnhandledTypeError, type);
					return null;
				}
			}
			catch(Exception)
			{
				return null;
			}
		}


		void WaitForTask(ITask task, WaitMode mode = WaitMode.Background)
		// Update progress bars to match progress of given task
		{
			if(activeTask != task)
			{
				return;
			}

			if(mode == WaitMode.Background)
			// Unintrusive background process
			{
				task.OnEnd = OnWaitingBackgroundTaskEnd;

				DisplayBackgroundProgressBar(task.Label, task.Progress);

				if(!task.Done)
				{
					ScheduleMainThread(() => WaitForTask(task, mode));
				}
			}
			else if(mode == WaitMode.Modal)
			// Obstruct editor interface, while offering cancel button
			{
				task.OnEnd = OnWaitingModalTaskEnd;

				if(!EditorUtility.DisplayCancelableProgressBar(TaskProgressTitle, task.Label, task.Progress) && !task.Done)
				{
					ScheduleMainThread(() => WaitForTask(task, mode));
				}
				else if(!task.Done)
				{
					task.Abort();
				}
			}
			else
			// Offer to interrupt task via dialog box, else block main thread until completion
			{
				if(EditorUtility.DisplayDialog(
					TaskBlockingTitle,
					string.Format(TaskBlockingDescription, task.Label),
					TaskBlockingComplete,
					TaskBlockingInterrupt
				))
				{
					do
					{
						EditorUtility.DisplayProgressBar(TaskProgressTitle, task.Label, task.Progress);
						Thread.Sleep(BlockingTaskWaitSleep);
					}
					while(!task.Done);

					EditorUtility.ClearProgressBar();
				}
				else
				{
					task.Abort();
				}
			}
		}


		static void DisplayBackgroundProgressBar(string description, float progress)
		{
			if(displayBackgroundProgressBar == null)
			{
				Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.AsyncProgressBar");
				displayBackgroundProgressBar = (ProgressBarDisplayMethod)Delegate.CreateDelegate(
					typeof(ProgressBarDisplayMethod),
					type.GetMethod("Display", new Type[]{ typeof(string), typeof(float) })
				);
			}

			displayBackgroundProgressBar(description, progress);
		}


		static void ClearBackgroundProgressBar()
		{
			if(clearBackgroundProgressBar == null)
			{
				Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.AsyncProgressBar");
				clearBackgroundProgressBar = (Action)Delegate.CreateDelegate(typeof(Action), type.GetMethod("Clear", new Type[]{}));
			}

			clearBackgroundProgressBar();
		}


		void OnWaitingBackgroundTaskEnd(ITask task)
		{
			ScheduleMainThread(() => ClearBackgroundProgressBar());
		}


		void OnWaitingModalTaskEnd(ITask task)
		{
			ScheduleMainThread(() => EditorUtility.ClearProgressBar());
		}


		public static void ScheduleMainThread(Action action)
		{
			EditorApplication.delayCall += () => action();
		}


		public static void ReportFailure(FailureSeverity severity, ITask task, string error)
		{
			if (severity == FailureSeverity.Moderate)
			{
				Debug.LogErrorFormat(TaskFailureMessage, task.Label, error);
			}
			else
			{
				ScheduleMainThread(() => EditorUtility.DisplayDialog(TaskFailureTitle, string.Format(TaskFailureMessage, task.Label, error), TaskFailureOK));
			}
		}
	}
}
