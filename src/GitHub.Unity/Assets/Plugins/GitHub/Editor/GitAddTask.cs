using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class GitAddTask : ProcessTask
	{
		const string
			AddErrorTitle = "GitHub",
			AddErrorMessage = "Git add failed:\n{0}",
			AddErrorOK = "OK";


		public static void Schedule(IEnumerable<string> files)
		{
			Tasks.Add(new GitAddTask(files));
		}


		StringWriter error = new StringWriter();
		string arguments = "";


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		public GitAddTask(IEnumerable<string> files)
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
			if (!Done)
			{
				return;
			}

			// Pop up any errors
			StringBuilder buffer = error.GetStringBuilder();
			if (buffer.Length > 0)
			{
				EditorUtility.DisplayDialog(AddErrorTitle, string.Format(AddErrorMessage, buffer.ToString()), AddErrorOK);
			}

			// Always update
			GitStatusTask.Schedule();
		}
	}
}
