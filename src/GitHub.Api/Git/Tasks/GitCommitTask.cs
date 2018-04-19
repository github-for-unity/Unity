﻿using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitCommitTask : ProcessTask<string>
    {
        private const string TaskName = "git commit";
        private readonly string arguments;

        public GitCommitTask(string messageFile,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(messageFile, "messageFile");

            Name = TaskName;
            arguments = "-c i18n.commitencoding=utf8 commit ";
            arguments += String.Format(" --file \"{0}\"", messageFile);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
