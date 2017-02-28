using System;

namespace GitHub.Unity
{
    class GitTrackedFilesTask : GitTask
    {
        private readonly ITaskResultDispatcher<GitStatus> resultDispatcher;
        private readonly TrackedFilesOutputProcessor processor;
        private GitStatus gitStatus;

        public GitTrackedFilesTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher<GitStatus> resultDispatcher,
                IGitObjectFactory gitObjectFactory)
            : base(environment, processManager)
        {
            this.resultDispatcher = resultDispatcher;
            processor = new TrackedFilesOutputProcessor(gitObjectFactory);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnStatus += status => {
                gitStatus = status;
                resultDispatcher.ReportSuccess(gitStatus);
            };
            return new ProcessOutputManager(process, processor);
        }

        public override bool Blocking { get { return false; } }
        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git lfs locks"; } }

        protected override string ProcessArguments
        {
            get { return "lfs locks"; }
        }
    }

    class GitStatusTask : GitTask
    {
        private readonly ITaskResultDispatcher<GitStatus> resultDispatcher;
        private readonly StatusOutputProcessor processor;
        private GitStatus gitStatus;

        public GitStatusTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher<GitStatus> resultDispatcher,
                IGitObjectFactory gitObjectFactory)
            : base(environment, processManager)
        {
            this.resultDispatcher = resultDispatcher;
            processor = new StatusOutputProcessor(gitObjectFactory);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnStatus += status => {
                gitStatus = status;
                resultDispatcher.ReportSuccess(gitStatus);
            };
            return new ProcessOutputManager(process, processor);
        }

        public override bool Blocking { get { return false; } }
        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git status"; } }

        protected override string ProcessArguments
        {
            get { return "status -b -u --porcelain"; }
        }
    }
}
