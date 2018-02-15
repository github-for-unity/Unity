using GitHub.Unity;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    static class ProcessManagerExtensions
    {
        static NPath defaultGitPath = "git".ToNPath();

        public static async Task<IEnumerable<GitBranch>> GetGitBranches(this ProcessManager processManager,
            NPath workingDirectory,
            NPath? gitPath = null)
        {
            var processor = new BranchListOutputProcessor();
            NPath path = gitPath ?? defaultGitPath;

            var results = await new ProcessTaskWithListOutput<GitBranch>(CancellationToken.None, processor)
                .Configure(processManager, path, "branch -vv", workingDirectory, false)
                .Start()
                .Task;

            return results;
        }

        public static async Task<List<GitLogEntry>> GetGitLogEntries(this ProcessManager processManager,
            NPath workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IProcessEnvironment gitEnvironment,
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

            var results = await new ProcessTaskWithListOutput<GitLogEntry>(CancellationToken.None, processor)
                .Configure(processManager, path, logNameStatus, workingDirectory, false)
                .Start()
                .Task;

            return results;
        }

        public static async Task<GitStatus> GetGitStatus(this ProcessManager processManager,
            NPath workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IProcessEnvironment gitEnvironment,
            NPath? gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);
            var processor = new StatusOutputProcessor(gitStatusEntryFactory);

            NPath path = gitPath ?? defaultGitPath;

            var results = await new ProcessTask<GitStatus>(CancellationToken.None, processor)
                .Configure(processManager, path, "status -b -u --porcelain", workingDirectory, false)
                .Start()
                .Task;

            return results;
        }
 
        public static async Task<List<GitRemote>> GetGitRemoteEntries(this ProcessManager processManager,
            NPath workingDirectory,
            NPath? gitPath = null)
        {
            var processor = new RemoteListOutputProcessor();

            NPath path = gitPath ?? defaultGitPath;

            var results = await new ProcessTaskWithListOutput<GitRemote>(CancellationToken.None, processor)
                .Configure(processManager, path, "remote -v", workingDirectory, false)
                .Start()
                .Task;
            return results;
        }

        public static async Task<string> GetGitCreds(this ProcessManager processManager,
            NPath workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IProcessEnvironment gitEnvironment,
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
            return await task.StartAsAsync();
        }
    }
}
