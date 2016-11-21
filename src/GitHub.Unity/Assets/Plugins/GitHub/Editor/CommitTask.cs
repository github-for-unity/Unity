using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class CommitTask : ProcessTask
	{
		StringWriter
		output = new StringWriter(),
		error = new StringWriter();

		string arguments = "";

		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		public CommitTask(string msg, string body)
		{
			arguments = "commit ";
			arguments += String.Format(@" -m ""{0}{1}{2}""", msg, Environment.NewLine, body);
		}

		public static void Schedule(IEnumerable<string> files, string msg, string body)
		{
			Tasks.Add(new AddTask(files));
			Tasks.Add(new CommitTask(msg, body));
		}

		protected override void OnProcessOutputUpdate()
		{
			StringBuilder buffer = output.GetStringBuilder();
			int end = buffer.Length - 1;
			if(!Done)
				// Only try to avoid partial lines if the process did not already end
			{
				for(; end > 0 && buffer[end] != '\n'; --end);
			}

			if(Done)
				// If we are done, hand over the results to any listeners on the main thread
			{
				Debug.Log(buffer.ToString());
				Debug.Log(error.GetStringBuilder().ToString());
				GitStatusTask.Schedule();
			}
		}
	}

	class AddTask : ProcessTask
	{
		StringWriter
		output = new StringWriter(),
		error = new StringWriter();

		string arguments = "";

		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		public AddTask(IEnumerable<string> files)
		{
			arguments = "add ";
			arguments += " -- ";
			foreach (var f in files)
			{
				arguments += " " + f;
			}
		}

		protected override void OnProcessOutputUpdate()
		{
			StringBuilder buffer = output.GetStringBuilder();
			int end = buffer.Length - 1;
			if(!Done)
				// Only try to avoid partial lines if the process did not already end
			{
				for(; end > 0 && buffer[end] != '\n'; --end);
			}

			if(Done)
				// If we are done, hand over the results to any listeners on the main thread
			{
				Debug.Log(buffer.ToString());
				Debug.Log(error.GetStringBuilder().ToString());
			}
		}
	}
}

