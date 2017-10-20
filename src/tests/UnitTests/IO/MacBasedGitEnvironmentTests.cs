using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    public class MacGitEnvironmentTests
    {
        public static IEnumerable<TestCaseData> ValidateGitPath_TestCases()
        {
            var testCase = new TestCaseData(true, true);
            testCase.SetName("Should be found");
            yield return testCase;

            testCase = new TestCaseData(false, false);
            testCase.SetName("Should not be found");
            yield return testCase;
        }

        [TestCaseSource(nameof(ValidateGitPath_TestCases))]
        public void ValidateGitPath(bool inFileSystem, bool found)
        {
            var filesystem = Substitute.For<IFileSystem>();
            filesystem.FileExists(Args.String).Returns(inFileSystem);

            //var linuxBasedGitInstallationStrategy = new ProcessEnvironment(environment);
            //linuxBasedGitInstallationStrategy.ValidateGitInstall("asdf").Should().Be(found);
        }
    }
}