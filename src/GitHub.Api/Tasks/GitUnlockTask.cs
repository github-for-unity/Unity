using System;
using System.Text;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitUnlockTask : GitTask
    {
        private readonly string arguments;

        public GitUnlockTask(IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<string> resultDispatcher,
                string path, bool force = false)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");

            var stringBuilder = new StringBuilder("lfs unlock ");

            if (force)
            {
                stringBuilder.Append("--force ");
            }

            stringBuilder.Append(path);
            arguments = stringBuilder.ToString();
        }

        public override bool Blocking => false;
        public override bool Critical => false;
        public override string Label { get; set; } = "git lfs unlock";
        protected override string ProcessArguments => arguments;

    }
}
