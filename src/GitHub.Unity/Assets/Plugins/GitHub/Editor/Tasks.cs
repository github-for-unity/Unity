using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System;


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
	interface ITask
	{
		bool Blocking { get; }
		float Progress { get; }
		bool Done { get; }
		bool Queued { get; }
		bool Critical { get; }
		bool Cached { get; }
		Action<ITask> OnEnd { set; }
		string Label { get; }
		void Run();
		void Abort();
	}


	class Tasks
	{
		enum WaitMode
		{
			Background,
			Modal,
			Blocking
		};


		delegate void ProgressBarDisplayMethod(string text, float progress);


		const int
			NoTasksSleep = 100,
			BlockingTaskWaitSleep = 10;
		const string
			CacheFileName = "GitHubCache",
			QuitActionFieldName = "editorApplicationQuit",
			TaskThreadExceptionRestartError = "GitHub task thread restarting after encountering an exception: {0}",
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


		Tasks()
		{
			editorApplicationQuit = (UnityAction)Delegate.Combine(editorApplicationQuit, new UnityAction(OnQuit));
			CacheFilePath = Path.Combine(Application.dataPath, Path.Combine("..", Path.Combine("Temp", CacheFileName)));

			tasks = new Queue<ITask>();
			// TODO: Rebuild task list from file system cache if any

			thread = new Thread(Start);
			thread.Start();
		}


		public static void Add(ITask task)
		{
			if(!task.Queued && Instance.tasks.Count > 0)
			{
				return;
			}

			Instance.tasks.Enqueue(task);
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

					try
					// At least don't start with outdated cache next time
					{
						File.Delete(CacheFilePath);
					}
					finally
					// Build and write cache
					{
						StringBuilder cache = new StringBuilder();

						if(activeTask != null && !activeTask.Done && activeTask.Cached)
						{
							cache.Append(activeTask);
							cache.Append('\n');

							activeTask = null;
						}

						while(tasks.Count > 0)
						{
							ITask task = tasks.Dequeue();

							if(!task.Cached)
							{
								continue;
							}

							cache.Append(task);
							cache.Append('\n');
						}

						File.WriteAllText(CacheFilePath, cache.ToString());
					}

					break;
				}
				catch(Exception e)
				// Something broke internally - reboot
				{
					Debug.LogErrorFormat(TaskThreadExceptionRestartError, e);

					running = false;

					Thread.Sleep(1);
				}
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
				if(activeTask != null && activeTask.Done)
				{
					activeTask = null;
				}

				if(activeTask == null && tasks.Count > 0)
				{
					activeTask = tasks.Dequeue();
				}

				if(activeTask != null)
				{
					ScheduleMainThread(() =>
					{
						if(activeTask != null)
						{
							WaitForTask(activeTask, activeTask.Blocking ? WaitMode.Modal : WaitMode.Background);
						}
					});

					activeTask.Run();
				}
				else
				{
					Thread.Sleep(NoTasksSleep);
				}
			}

			thread.Abort();
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


		void ScheduleMainThread(EditorApplication.CallbackFunction action)
		{
			EditorApplication.delayCall += action;
		}
	}
}
