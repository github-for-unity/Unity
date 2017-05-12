using GitHub.Unity;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    static class ProcessManagerExtensions
    {
        public static async Task<IEnumerable<GitBranch>> GetGitBranches(this ProcessManager processManager,
            string workingDirectory,
            string gitPath = null)
        {
            var processor = new BranchListOutputProcessor();
            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var results = await new ProcessTaskWithListOutput<GitBranch>(CancellationToken.None, processor)
                .Configure(processManager, path, "branch -vv", workingDirectory, false)
                .Start()
                .Task;

            return results;
        }

        public static async Task<List<GitLogEntry>> GetGitLogEntries(this ProcessManager processManager,
            string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IProcessEnvironment gitEnvironment,
            int? logCount = null, string gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var processor = new LogEntryOutputProcessor(gitStatusEntryFactory);

            var logNameStatus = @"log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status";

            if (logCount.HasValue)
            {
                logNameStatus = logNameStatus + " -" + logCount.Value;
            }

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var results = await new ProcessTaskWithListOutput<GitLogEntry>(CancellationToken.None, processor)
                .Configure(processManager, path, logNameStatus, workingDirectory, false)
                .Start()
                .Task;

            return results;
        }

        public static async Task<GitStatus> GetGitStatus(this ProcessManager processManager,
            string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IProcessEnvironment gitEnvironment,
            string gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);
            var processor = new StatusOutputProcessor(gitStatusEntryFactory);

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var results = await new ProcessTask<GitStatus?>(CancellationToken.None, processor)
                .Configure(processManager, path, "status -b -u --porcelain", workingDirectory, false)
                .Start()
                .Task;

            return results.Value;
        }
 
        public static async Task<List<GitRemote>> GetGitRemoteEntries(this ProcessManager processManager,
            string workingDirectory,
            string gitPath = null)
        {
            var processor = new RemoteListOutputProcessor();

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var results = await new ProcessTaskWithListOutput<GitRemote>(CancellationToken.None, processor)
                .Configure(processManager, path, "remote -v", workingDirectory, false)
                .Start()
                .Task;
            return results;
        }

        public static async Task<string> GetGitCreds(this ProcessManager processManager,
            string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IProcessEnvironment gitEnvironment,
            string gitPath = null)
        {
            var processor = new FirstNonNullLineOutputProcessor();

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var task = new ProcessTask<string>(CancellationToken.None, processor)
                .Configure(processManager, path, "credential-wincred get", workingDirectory, true);

            task.OnStartProcess += p =>
            {
                p.StandardInput.WriteLine("protocol=https");
                p.StandardInput.WriteLine("host=github.com");
                p.StandardInput.Close();
            };
            return await task.Start().Task;
        }
    }
}
