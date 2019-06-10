using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitCommitTask : ProcessTask<string>
    {
        private const string TaskName = "git commit";

        private readonly string message;
        private readonly string body;
        private readonly string arguments;

        private NPath tempFile;

        public GitCommitTask(string message, string body,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(message, "message");

            this.message = message;
            this.body = body ?? string.Empty;

            Name = TaskName;
            tempFile = NPath.GetTempFilename("GitCommitTask");
            arguments = $"-c i18n.commitencoding=utf8 commit --file \"{tempFile}\"";
        }

        protected override void RaiseOnStart()
        {
            base.RaiseOnStart();
            tempFile.WriteAllLines(new [] { message, Environment.NewLine, body });
        }

        protected override void RaiseOnEnd()
        {
            tempFile.DeleteIfExists();
            base.RaiseOnEnd();
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Committing...";
    }
}
