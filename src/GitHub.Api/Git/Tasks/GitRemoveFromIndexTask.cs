using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitRemoveFromIndexTask : ProcessTask<string>
    {
        private const string TaskName = "git reset HEAD";
        private readonly string arguments;

        public GitRemoveFromIndexTask(IEnumerable<string> files,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(files, "files");

            Name = TaskName;
            arguments = "reset HEAD";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Unstaging files...";
    }
}
