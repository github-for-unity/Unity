using System.Collections.Generic;

namespace GitHub.Unity.Tests
{
    static class ProcessManagerExtensions
    {
        public static IEnumerable<GitBranch> GetGitBranches(this ProcessManager processManager, string testGitRepoPath)
        {
            var processor = new BranchListOutputProcessor();
            var gitBranches = new List<GitBranch>();
            processor.OnBranch += data => gitBranches.Add(data);

            var process = processManager.Configure("git", "branch -vv", testGitRepoPath);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return gitBranches;
        }

        public static IEnumerable<GitLogEntry> GetGitLogEntries(this ProcessManager processManager, string workingDirectory, IEnvironment environment, IFileSystem fileSystem, IGitEnvironment gitEnvironment)
        {
            var results = new List<GitLogEntry>();

            var gitStatusEntryFactory = new GitStatusEntryFactory(environment, fileSystem, gitEnvironment);

            var processor = new LogEntryOutputProcessor(gitStatusEntryFactory);
            processor.OnLogEntry += data => results.Add(data);

            var process = processManager.Configure("git", "log --name-status -1", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return results;
        }

        public static GitStatus GetGitStatus(this ProcessManager processManager, string workingDirectory, IEnvironment environment, IFileSystem fileSystem, IGitEnvironment gitEnvironment)
        {
            var result = new GitStatus();

            var gitStatusEntryFactory = new GitStatusEntryFactory(environment, fileSystem, gitEnvironment);

            var processor = new StatusOutputProcessor(gitStatusEntryFactory);
            processor.OnStatus += data => result = data;

            var process = processManager.Configure("git", "status -b -u --porcelain", workingDirectory);
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();
            process.WaitForExit();

            return result;
        }
    }
}