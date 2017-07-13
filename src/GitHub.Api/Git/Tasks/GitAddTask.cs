using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitAddTask : ProcessTask<string>
    {
        public enum AddFileOption
        {
            All,
            CurrentDirectory
        }

        private readonly string arguments;

        public GitAddTask(IEnumerable<string> files, CancellationToken token, 
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(files, "files");

            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitAddTask(AddFileOption addFileOption, CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            arguments = "add ";

            switch (addFileOption)
            {
                case AddFileOption.All:
                    arguments += "-A";
                    break;

                case AddFileOption.CurrentDirectory:
                    arguments += ".";
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(addFileOption), addFileOption, null);
            }
        }

        public override string Name { get { return "git add"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
