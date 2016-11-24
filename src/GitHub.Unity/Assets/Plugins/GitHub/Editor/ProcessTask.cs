using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


namespace GitHub.Unity
{
	class ProcessTask : ITask
	{
		const int ExitMonitorSleep = 10;


		[MenuItem("Assets/GitHub/Process Test")]
		static void Test()
		{
			EditorApplication.delayCall += () => Tasks.Add(new ProcessTask());
		}


		public virtual bool Blocking { get { return true; } }
		public virtual float Progress { get; protected set; }
		public virtual bool Done { get; protected set; }
		public virtual bool Queued { get { return true; } }
		public virtual bool Critical { get { return true; } }
		public virtual bool Cached { get { return true; } }
		public virtual Action<ITask> OnBegin { get; set; }
		public virtual Action<ITask> OnEnd { get; set; }
		public virtual string Label { get { return "Process task"; } }


		protected virtual string ProcessName { get { return "sleep"; } }
		protected virtual string ProcessArguments { get { return "20"; } }
		protected virtual CachedTask CachedTaskType { get { return CachedTask.ProcessTask; } }
		protected virtual TextWriter OutputBuffer { get { return null; } }
		protected virtual TextWriter ErrorBuffer { get { return null; } }


		Process process;


		protected ProcessTask()
		{}


		public static ProcessTask Parse(IDictionary<string, object> data)
		// Try to reattach to the process. Assume that we're done if that fails.
		{
			Process resumedProcess;

			try
			{
				resumedProcess = Process.GetProcessById((int)(Int64)data[Tasks.ProcessKey]);
				resumedProcess.StartInfo.RedirectStandardOutput = resumedProcess.StartInfo.RedirectStandardError = true;
			}
			catch(Exception)
			{
				resumedProcess = null;
			}

			return new ProcessTask()
			{
				process = resumedProcess,
				Done = resumedProcess == null,
				Progress = resumedProcess == null ? 1f : 0f
			};
		}


		public void Run()
		{
			Debug.LogFormat("{0} {1}", Label, process == null ? "start" : "reconnect");

			Done = false;
			Progress = 0.0f;

			if(OnBegin != null)
			{
				OnBegin(this);
			}

			if(process == null)
			// Only start the process if we haven't already reconnected to an existing instance
			{
				process = Process.Start(new ProcessStartInfo(ProcessName, ProcessArguments)
				{
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					WorkingDirectory = Utility.GitRoot
				});
			}

			TextWriter
				output = OutputBuffer,
				error = ErrorBuffer;

			// NOTE: WaitForExit is too low level here. Won't be properly interrupted by thread abort.
			do
			{
				// Wait a bit
				Thread.Sleep(ExitMonitorSleep);

				// Read all available process output //

				bool updated = false;

				while(!process.StandardOutput.EndOfStream)
				{
					char read = (char)process.StandardOutput.Read();
					if(output != null)
					{
						output.Write(read);
						updated = true;
					}
				}

				while(!process.StandardError.EndOfStream)
				{
					char read = (char)process.StandardError.Read();
					if(error != null)
					{
						error.Write(read);
						updated = true;
					}
				}

				// Notify if anything was read //

				if(updated)
				{
					OnProcessOutputUpdate();
				}
			}
			while(!process.HasExited);

			Progress = 1.0f;
			Done = true;

			OnProcessOutputUpdate();

			Debug.LogFormat("{0} end", Label);

			if(OnEnd != null)
			{
				OnEnd(this);
			}
		}


		public void Abort()
		{
			Debug.LogFormat("Aborting {0}", Label);

			try
			{
				process.Kill();
			}
			catch(Exception)
			{}

			Done = true;

			if(OnEnd != null)
			{
				OnEnd(this);
			}
		}


		public void Disconnect()
		{
			Debug.LogFormat("Disconnect {0}", Label);

			process = null;
		}


		public void Reconnect()
		{}


		public void WriteCache(TextWriter cache)
		{
			Debug.LogFormat("Writing cache for {0}", Label);

			cache.Write(
@"{{
	""{0}"": ""{1}"",
	""{2}"": {3}
}}",
				Tasks.TypeKey,		CachedTaskType,
				Tasks.ProcessKey,	process == null ? -1 : process.Id
			);
		}


		protected virtual void OnProcessOutputUpdate()
		{}
	}
}
