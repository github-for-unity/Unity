using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitCommitFilesTask : BaseTask
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ITaskResultDispatcher<string> resultDispatcher;
        private readonly IEnumerable<string> files;
        private readonly string message;
        private readonly string body;

        public GitCommitFilesTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            IEnumerable<string> files, string message, string body)
        {
            Guard.ArgumentNotNull(files, "files");
            Guard.ArgumentNotNull(message, "message");

            this.environment = environment;
            this.processManager = processManager;
            this.resultDispatcher = resultDispatcher;
            this.files = files;
            this.message = message;
            this.body = body;
        }

        public override Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                TaskEx.FromResult(false);

            var gitAddTask = new GitAddTask(environment, processManager, new TaskResultDispatcher<string>(s => { }), files);
            return gitAddTask.RunAsync(cancellationToken)
                             .ContinueWith(_ =>
                             {
                                 var gitCommitTask = new GitCommitTask(environment, processManager,
                                     new TaskResultDispatcher<string>(s => { }), message, body);
                                 return gitCommitTask.RunAsync(cancellationToken);
                             }, cancellationToken, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, ThreadingHelper.TaskScheduler)
                             .ContinueWith(_ => {
                                 resultDispatcher.ReportSuccess(string.Empty);
                                 return true;
                             }, cancellationToken, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, ThreadingHelper.TaskScheduler);
        }

        public override void Run(CancellationToken cancellationToken)
        {
            RunAsync(cancellationToken).Wait();
        }

        public override string Label { get { return "Add and Commit Files task"; } }
    }
}