using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Unity.Extensions;

namespace GitHub.Unity
{
    class GitAddTask : GitTask
    {
        private string arguments = "";

        private GitAddTask(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " " + file;
            }
        }

        public static void Schedule(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitAddTask(files, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            base.OnProcessOutputUpdate();

            // Always update
            StatusService.Instance.Run();
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override bool Critical
        {
            get { return true; }
        }

        public override bool Cached
        {
            get { return true; }
        }

        public override string Label
        {
            get { return "git add"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }
    }
}
