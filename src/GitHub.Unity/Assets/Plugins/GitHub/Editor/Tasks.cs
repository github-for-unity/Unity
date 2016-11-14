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


namespace GitHub
{
	interface ITask
	{
		bool Blocking {get;}
		float Progress {get;}
		bool Done {get;}
		bool Queued {get;}
		bool Critical {get;}
		bool Cached {get;}
		Action<ITask> OnEnd {set;}
		string Label {get;}
		void Run ();
		void Abort ();
	}


	class Tasks
	{
		enum WaitMode
		{
			Background,
			Modal,
			Blocking
		};


		const int
			kNoTasksSleep = 100,
			kBlockingTaskWaitSleep = 10;
		const string
			kCacheFileName = "GitHubCache",
			kQuitActionFieldName = "editorApplicationQuit",
			kTaskThreadExceptionRestartError = "GitHub task thread restarting after encountering an exception: {0}",
			kTaskProgressTitle = "GitHub",
			kTaskBlockingTitle = "Critical GitHub task",
			kTaskBlockingDescription = "A critical GitHub task ({0}) has yet to complete. What would you like to do?",
			kTaskBlockingComplete = "Complete",
			kTaskBlockingInterrupt = "Interrupt";
		const BindingFlags kQuitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;


		static FieldInfo m_QuitActionField;


		static Tasks Instance {get;set;}
		static string CacheFilePath {get; set;}


		static void SecureQuitActionField ()
		{
			if (m_QuitActionField == null)
			{
				m_QuitActionField = typeof (EditorApplication).GetField (kQuitActionFieldName, kQuitActionBindingFlags);

				if (m_QuitActionField == null)
				{
					throw new NullReferenceException ("Unable to reflect EditorApplication." + kQuitActionFieldName);
				}
			}
		}


		static UnityAction editorApplicationQuit
		{
			get
			{
				SecureQuitActionField ();
				return (UnityAction)m_QuitActionField.GetValue (null);
			}
			set
			{
				SecureQuitActionField ();
				m_QuitActionField.SetValue (null, value);
			}
		}


		// "Everything is broken - let's rebuild from the ashes (read: cache)"
		[InitializeOnLoadMethod]
		static void OnLoad ()
		{
			Instance = new Tasks ();
		}


		bool m_Running = false;
		Thread m_Thread;
		ITask m_ActiveTask;
		Queue<ITask> m_Tasks;


		Tasks ()
		{
			editorApplicationQuit = (UnityAction)Delegate.Combine (editorApplicationQuit, new UnityAction (OnQuit));
			CacheFilePath = Path.Combine (Application.dataPath, Path.Combine ("..", Path.Combine ("Temp", kCacheFileName)));

			m_Tasks = new Queue<ITask> ();
			// TODO: Rebuild task list from file system cache if any

			m_Thread = new Thread (Start);
			m_Thread.Start ();
		}


		public static void Add (ITask task)
		{
			if (!task.Queued && Instance.m_Tasks.Count > 0)
			{
				return;
			}

			Instance.m_Tasks.Enqueue (task);
		}


		void Start ()
		{
			while (true)
			{
				try
				{
					Run ();

					break;
				}
				catch (ThreadAbortException)
				// Aborted by domain unload or explicitly via the editor quit handler. Button down the hatches.
				{
					m_Running = false;

					try
					// At least don't start with outdated cache next time
					{
						File.Delete (CacheFilePath);
					}
					finally
					// Build and write cache
					{
						StringBuilder cache = new StringBuilder ();

						if (m_ActiveTask != null && !m_ActiveTask.Done && m_ActiveTask.Cached)
						{
							cache.Append (m_ActiveTask);
							cache.Append ('\n');

							m_ActiveTask = null;
						}

						while (m_Tasks.Count > 0)
						{
							ITask task = m_Tasks.Dequeue ();

							if (!task.Cached)
							{
								continue;
							}

							cache.Append (task);
							cache.Append ('\n');
						}

						File.WriteAllText (CacheFilePath, cache.ToString ());
					}

					break;
				}
				catch (Exception e)
				// Something broke internally - reboot
				{
					Debug.LogErrorFormat (kTaskThreadExceptionRestartError, e);

					m_Running = false;

					Thread.Sleep (1);
				}
			}
		}


		void OnQuit ()
		{
			// Stop the queue
			m_Running = false;

			if (m_ActiveTask != null && m_ActiveTask.Critical)
			{
				WaitForTask (m_ActiveTask, WaitMode.Blocking);
			}
		}


		void Run ()
		{
			m_Running = true;

			while (m_Running)
			{
				if (m_ActiveTask != null && m_ActiveTask.Done)
				{
					m_ActiveTask = null;
				}

				if (m_ActiveTask == null && m_Tasks.Count > 0)
				{
					m_ActiveTask = m_Tasks.Dequeue ();
				}

				if (m_ActiveTask != null)
				{
					ScheduleMainThread (() => WaitForTask (m_ActiveTask, m_ActiveTask.Blocking ? WaitMode.Modal : WaitMode.Background));

					m_ActiveTask.Run ();
				}
				else
				{
					Thread.Sleep (kNoTasksSleep);
				}
			}

			m_Thread.Abort ();
		}


		void WaitForTask (ITask task, WaitMode mode = WaitMode.Background)
		// Update progress bars to match progress of given task
		{
			if (m_ActiveTask != task)
			{
				return;
			}

			if (mode == WaitMode.Background)
			// Unintrusive background process
			{
				task.OnEnd = OnWaitingBackgroundTaskEnd;

				DisplayBackgroundProgressBar (kTaskProgressTitle, task.Label, task.Progress);

				if (!task.Done)
				{
					ScheduleMainThread (() => WaitForTask (task, mode));
				}
			}
			else if (mode == WaitMode.Modal)
			// Obstruct editor interface, while offering cancel button
			{
				task.OnEnd = OnWaitingModalTaskEnd;

				if (!EditorUtility.DisplayCancelableProgressBar (kTaskProgressTitle, task.Label, task.Progress) && !task.Done)
				{
					ScheduleMainThread (() => WaitForTask (task, mode));
				}
				else if (!task.Done)
				{
					task.Abort ();
				}
			}
			else
			// Offer to interrupt task via dialog box, else block main thread until completion
			{
				if (EditorUtility.DisplayDialog (
					kTaskBlockingTitle,
					string.Format (kTaskBlockingDescription, task.Label),
					kTaskBlockingComplete,
					kTaskBlockingInterrupt
				))
				{
					do
					{
						EditorUtility.DisplayProgressBar (kTaskProgressTitle, task.Label, task.Progress);
						Thread.Sleep (kBlockingTaskWaitSleep);
					}
					while (!task.Done);

					EditorUtility.ClearProgressBar ();
				}
				else
				{
					task.Abort ();
				}
			}
		}


		void DisplayBackgroundProgressBar (string title, string description, float progress)
		{
			// TODO: Use the same progress bar as the various bake systems
			Debug.LogFormat ("Background progress: {0}", progress);
		}


		void OnWaitingBackgroundTaskEnd (ITask task)
		{
			// TODO: Clear the progress bar used above here
			ScheduleMainThread (() => Debug.Log ("TODO: Clear background progress"));
		}


		void OnWaitingModalTaskEnd (ITask task)
		{
			ScheduleMainThread (() => EditorUtility.ClearProgressBar ());
		}


		void ScheduleMainThread (EditorApplication.CallbackFunction action)
		{
			EditorApplication.delayCall += action;
		}
	}
}
