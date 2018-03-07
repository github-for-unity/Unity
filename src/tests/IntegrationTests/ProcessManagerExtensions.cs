using GitHub.Unity;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests
{
    static class ProcessManagerExtensions
    {
        static NPath defaultGitPath = "git".ToNPath();

        public static ITask<List<GitBranch>> GetGitBranches(this IProcessManager processManager,
            NPath workingDirectory,
            NPath? gitPath = null)
        {
            var processor = new BranchListOutputProcessor();
            NPath path = gitPath ?? defaultGitPath;

            return new ProcessTaskWithListOutput<GitBranch>(CancellationToken.None, processor)
                .Configure(processManager, path, "branch -vv", workingDirectory, false);
        }

        public static ITask<List<GitLogEntry>> GetGitLogEntries(this IProcessManager processManager,
            NPath workingDirectory,
            IEnvironment environment, IProcessEnvironment gitEnvironment,
            int? logCount = null,
            NPath? gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var processor = new LogEntryOutputProcessor(gitStatusEntryFactory);

            var logNameStatus = @"log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status";

            if (logCount.HasValue)
            {
                logNameStatus = logNameStatus + " -" + logCount.Value;
            }

            NPath path = gitPath ?? defaultGitPath;

            return new ProcessTaskWithListOutput<GitLogEntry>(CancellationToken.None, processor)
                .Configure(processManager, path, logNameStatus, workingDirectory, false);
        }

        public static ITask<GitStatus> GetGitStatus(this IProcessManager processManager,
            NPath workingDirectory,
            IEnvironment environment, IProcessEnvironment gitEnvironment,
            NPath? gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);
            var processor = new StatusOutputProcessor(gitStatusEntryFactory);

            NPath path = gitPath ?? defaultGitPath;

            return new ProcessTask<GitStatus>(CancellationToken.None, processor)
                .Configure(processManager, path, "status -b -u --porcelain", workingDirectory, false);
        }

        public static ITask<List<GitRemote>> GetGitRemoteEntries(this IProcessManager processManager,
            NPath workingDirectory,
            NPath? gitPath = null)
        {
            var processor = new RemoteListOutputProcessor();

            NPath path = gitPath ?? defaultGitPath;

            return new ProcessTaskWithListOutput<GitRemote>(CancellationToken.None, processor)
                .Configure(processManager, path, "remote -v", workingDirectory, false);
        }

        public static ITask<string> GetGitCreds(this IProcessManager processManager,
            NPath workingDirectory,
            IEnvironment environment, IProcessEnvironment gitEnvironment,
            NPath? gitPath = null)
        {
            var processor = new FirstNonNullLineOutputProcessor();

            NPath path = gitPath ?? defaultGitPath;

            var task = new ProcessTask<string>(CancellationToken.None, processor)
                .Configure(processManager, path, "credential-wincred get", workingDirectory, true);

            task.OnStartProcess += p =>
            {
                p.StandardInput.WriteLine("protocol=https");
                p.StandardInput.WriteLine("host=github.com");
                p.StandardInput.Close();
            };
            return task;
        }
    }
}
