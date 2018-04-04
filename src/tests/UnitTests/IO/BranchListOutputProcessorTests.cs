using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    public class BranchListOutputProcessorTests
    {
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
                new GitBranch("master", "origin/master"),
                new GitBranch("feature/feature-1", ""),
                new GitBranch("bugfixes/bugfix-1", "origin/bugfixes/bugfix-1"),
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitBranch[] expected)
        {
            var results = new List<GitBranch>();

            var outputProcessor = new BranchListOutputProcessor();
            outputProcessor.OnEntry += branch =>
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