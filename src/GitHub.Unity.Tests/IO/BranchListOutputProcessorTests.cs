using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Api;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class BranchListOutputProcessorTests
    {
        //[Test]
        public void IntegrationTest_MonoRepo()
        {
            var filesystem = new FileSystem();
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = @"D:\code\github\UnityInternal\src\UnityExtension";
            var gitEnvironment = new WindowsGitEnvironment(environment, filesystem);
            var fact = new GitObjectFactory(environment, gitEnvironment, filesystem);
            var pm = new ProcessManager(environment, gitEnvironment, filesystem);
            var results = pm.GetGitBranches(@"D:\code\github\UnityInternal");
        }

        [Test]
        public void ShouldProcessOutput()
        {
            var output = new[]
            {
                "* master                          ef7ecf9 [origin/master] Some project master",
                "  feature/feature-1               f47d41b Untracked Feature 1",
                "  bugfixes/bugfix-1               e1b7f22 [origin/bugfixes/bugfix-1] Tracked Local Bugfix"
            };

            AssertProcessOutput(output, new[]
            {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/feature-1", "", false),
                new GitBranch("bugfixes/bugfix-1", "origin/bugfixes/bugfix-1", false),
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitBranch[] expected)
        {
            var results = new List<GitBranch>();

            var outputProcessor = new BranchListOutputProcessor();
            outputProcessor.OnBranch += branch =>
            {
                results.Add(branch);
            };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            results.ShouldAllBeEquivalentTo(expected);
        }
    }
}