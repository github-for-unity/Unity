using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitAddTask : ProcessTask<string>
    {
        private const string TaskName = "git add";
        private readonly string arguments;

        public GitAddTask(IEnumerable<string> files, CancellationToken token, 
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitAddTask(CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            arguments = "add -A";
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Staging files...";
    }
}
