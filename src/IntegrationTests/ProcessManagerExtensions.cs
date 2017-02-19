using GitHub.Unity;
using System.Collections.Generic;
using System.Text;

namespace IntegrationTests
{
    static class ProcessManagerExtensions
    {
        public static IEnumerable<GitBranch> GetGitBranches(this IProcessManager processManager,
            string workingDirectory,
            string gitPath = null)
        {
            var results = new List<GitBranch>();

            var processor = new BranchListOutputProcessor();
            processor.OnBranch += data => results.Add(data);
            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;
            var process = processManager.Configure(path, "branch -vv", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static IEnumerable<GitLogEntry> GetGitLogEntries(this ProcessManager processManager, string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment,
            int? logCount = null, string gitPath = null)
        {
            var results = new List<GitLogEntry>();

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var processor = new LogEntryOutputProcessor(gitStatusEntryFactory);
            processor.OnLogEntry += data => results.Add(data);

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

            var process = processManager.Configure(path, logNameStatus, workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static GitStatus GetGitStatus(this ProcessManager processManager, string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment,
            string gitPath = null)
        {
            var result = new GitStatus();

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var processor = new StatusOutputProcessor(gitStatusEntryFactory);
            processor.OnStatus += data => result = data;

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var process = processManager.Configure(path, "status -b -u --porcelain", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return result;
        }
 
        public static IEnumerable<GitRemote> GetGitRemoteEntries(this ProcessManager processManager, string workingDirectory,
            string gitPath = null)
        {
            var results = new List<GitRemote>();

            var processor = new RemoteListOutputProcessor();
            processor.OnRemote += data => results.Add(data);

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var process = processManager.Configure(path, "remote -v", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static string GetGitCreds(this ProcessManager processManager, string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment,
            string gitPath = null)
        {
            StringBuilder sb = new StringBuilder();
            var processor = new BaseOutputProcessor();
            processor.OnData += data => sb.AppendLine();

            NPath path = null;
            if (gitPath == null)
                path = "git";
            else
                path = gitPath;

            var process = processManager.Configure(path, "credential-wincred get", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);
            process.OnStart += p =>
            {
                p.StandardInput.WriteLine("protocol=https");
                p.StandardInput.WriteLine("host=github.com");
                p.StandardInput.Close();
            };
            process.Run();
            process.WaitForExit();

            return sb.ToString();
        }
    }
}
