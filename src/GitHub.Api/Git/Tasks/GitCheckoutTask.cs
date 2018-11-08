using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitCheckoutTask : ProcessTask<string>
    {
        private const string TaskName = "git checkout";
        private readonly string arguments;

        public GitCheckoutTask(IEnumerable<string> files, CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "checkout ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitCheckoutTask(CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            arguments = "checkout -- .";
        }

        public GitCheckoutTask(
            string changeset,
            IEnumerable<string> files,
            CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "checkout ";
            arguments += changeset;
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }

            Message = "Checking out files at  rev " + changeset.Substring(0, 7); 
        }        

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Checking out files...";
    }
}