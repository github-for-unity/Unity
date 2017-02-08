using System;
using System.Text;

namespace GitHub.Unity
{
    class GitPullTask : GitTask
    {
        private readonly string arguments;

        private GitPullTask(Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            arguments = "pull";
        }

        private GitPullTask(string repository, string branch,
            Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("pull");

            if (!String.IsNullOrEmpty(repository))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(repository);
            }

            if (!String.IsNullOrEmpty(branch))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(branch);
            }

            arguments = stringBuilder.ToString();
        }

        public static void Schedule(string repository, string branch,
            Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitPullTask(repository, branch, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git pull"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
