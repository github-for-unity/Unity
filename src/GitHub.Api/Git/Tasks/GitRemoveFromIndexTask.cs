using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoveFromIndexTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitRemoveFromIndexTask(IEnumerable<string> files,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNull(files, "files");

            arguments = "reset HEAD";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public override string Name { get { return "git add"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}