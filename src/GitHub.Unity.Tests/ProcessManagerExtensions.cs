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
    }
}