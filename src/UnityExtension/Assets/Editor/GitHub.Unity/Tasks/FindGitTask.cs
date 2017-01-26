using System;
using System.IO;

namespace GitHub.Unity
{
    class FindGitTask : ProcessTask
    {
        private FindGitTask(Action<string> onSuccess, Action onFailure = null)
            : base(onSuccess, onFailure)
        {
        }

        public static bool ValidateGitInstall(string path)
        {
            if (!Path.GetFileName(path).Equals(Path.GetFileName(DefaultGitPath)) || !File.Exists(path))
            {
                return false;
            }

            return true;
        }

        public static void Schedule(Action<string> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new FindGitTask(onSuccess, onFailure));
        }

        public static string DefaultGitPath
        {
            get
            {
                return Utility.IsWindows
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Local\\GitHub\\PortableGit_\\cmd\\git.exe")
                    : "/usr/bin/git";
            }
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override bool Cached
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "find git"; }
        }

        protected override string ProcessName
        {
            get { return Utility.IsWindows ? "where" : "which"; }
        }

        protected override string ProcessArguments
        {
            get { return "git"; }
        }
    }
}
