using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class CommitTask : ProcessTask
	{
		const string
			CommitErrorTitle = "GitHub",
			CommitErrorMessage = "Git comit failed:\n{0}",
			CommitErrorOK = "OK";


		public static void Schedule(IEnumerable<string> files, string message, string body)
		{
			AddTask.Schedule(files);
			Schedule(message, body);
		}


		public static void Schedule(string message, string body)
		{
			Tasks.Add(new CommitTask(message, body));
		}


		StringWriter error = new StringWriter();
		string arguments = "";


		public CommitTask(string message, string body)
		{
			arguments = "commit ";
			arguments += string.Format(@" -m ""{0}\n{1}""", message, body);
		}


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		protected override void OnProcessOutputUpdate()
		{
			if (!Done)
			{
				return;
			}

			// Pop up any errors
			StringBuilder buffer = error.GetStringBuilder();
			if (buffer.Length > 0)
			{
				EditorUtility.DisplayDialog(CommitErrorTitle, string.Format(CommitErrorMessage, buffer.ToString()), CommitErrorOK);
			}

			// Always update
			GitStatusTask.Schedule();
		}
	}
}
