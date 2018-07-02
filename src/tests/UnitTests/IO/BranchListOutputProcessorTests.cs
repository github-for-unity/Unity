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
                "  bugfixes/bugfix-1               e1b7f22 [origin/bugfixes/bugfix-1] Tracked Local Bugfix",
                "  bugfixes/bugfix-2               e1b7f22 [origin/bugfixes/bugfix-2: ahead 3] Ahead with some changes",
                "  bugfixes/bugfix-3               e1b7f22 [origin/bugfixes/bugfix-3: ahead 3, behind 116] Ahead and Behind",
                "  bugfixes/bugfix-4               e1b7f22 [origin/bugfixes/bugfix-4: gone] No longer on server",
            };

            AssertProcessOutput(output, new[]
            {
                new GitBranch("master", "origin/master"),
                new GitBranch("feature/feature-1"),
                new GitBranch("bugfixes/bugfix-1", "origin/bugfixes/bugfix-1"),
                new GitBranch("bugfixes/bugfix-2", "origin/bugfixes/bugfix-2"),
                new GitBranch("bugfixes/bugfix-3", "origin/bugfixes/bugfix-3"),
                new GitBranch("bugfixes/bugfix-4", "origin/bugfixes/bugfix-4"),
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
