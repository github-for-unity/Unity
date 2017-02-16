using GitHub.Api;
using GitHub.Unity;
using System.Collections.Generic;
using System.Text;

namespace GitHub.Unity.Tests
{
    static class ProcessManagerExtensions
    {
        public static IEnumerable<GitBranch> GetGitBranches(this ProcessManager processManager, string workingDirectory)
        {
            var results = new List<GitBranch>();

            var processor = new BranchListOutputProcessor();
            processor.OnBranch += data => results.Add(data);

            var process = processManager.Configure("git", "branch -vv", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static IEnumerable<GitLogEntry> GetGitLogEntries(this ProcessManager processManager, string workingDirectory,
            IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment,
            int? logCount = null)
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

            var process = processManager.Configure("git", logNameStatus, workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static GitStatus GetGitStatus(this ProcessManager processManager, string workingDirectory, IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment)
        {
            var result = new GitStatus();

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var processor = new StatusOutputProcessor(gitStatusEntryFactory);
            processor.OnStatus += data => result = data;

            var process = processManager.Configure("git", "status -b -u --porcelain", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return result;
        }
 
        public static IEnumerable<GitRemote> GetGitRemoteEntries(this ProcessManager processManager, string workingDirectory)
        {
            var results = new List<GitRemote>();

            var processor = new RemoteListOutputProcessor();
            processor.OnRemote += data => results.Add(data);

            var process = processManager.Configure("git", "remote -v", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static string GetGitCreds(this ProcessManager processManager, string workingDirectory, IEnvironment environment, IFileSystem filesystem, IGitEnvironment gitEnvironment)
        {
            StringBuilder sb = new StringBuilder();
            var processor = new BaseOutputProcessor();
            processor.OnData += data => sb.AppendLine();
            var process = processManager.Configure("git", "credential-wincred get", workingDirectory);
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
