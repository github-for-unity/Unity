using GitHub.Api;
using GitHub.Unity;
using System.Collections.Generic;

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