using FluentAssertions;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    class ProcessManagerTests : BaseIOTest
    {
        [Test]
        public void BranchListTest()
        {
            var testEnvironment = new TestEnvironment();
            var processManager = new ProcessManager(new GitEnvironment(testEnvironment));
            var gitBranches = processManager.GetGitBranches(TestGitRepoPath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", string.Empty, false),
                new GitBranch("feature/document", string.Empty, true));
        }
    }
}
