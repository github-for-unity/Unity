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
        //public static IEnumerable<TestCaseData> GetDefaultGitPath_TestCases()
        //{
        //    var testCase = new TestCaseData(true, MacGitEnvironment.DefaultGitPath);
        //    testCase.SetName("Should be found");
        //    yield return testCase;

        //    testCase = new TestCaseData(false, null);
        //    testCase.SetName("Should be null");
        //    yield return testCase;
        //}

        //[TestCaseSource(nameof(GetDefaultGitPath_TestCases))]
        //public void GetDefaultGitPath(bool fileFound, string filePath)
        //{
        //    var environment = Substitute.For<IEnvironment>();

        //    var filesystem = Substitute.For<IFileSystem>();
        //    filesystem.FileExists(Args.String).Returns(fileFound);

        //    var linuxBasedGitInstallationStrategy = new MacGitEnvironment(environment, filesystem);
        //    linuxBasedGitInstallationStrategy.FindGitInstallationPath(TODO).Should().Be(filePath);
        //}

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
            var environment = Substitute.For<IEnvironment>();

            var filesystem = Substitute.For<IFileSystem>();
            filesystem.FileExists(Args.String).Returns(inFileSystem);

            var linuxBasedGitInstallationStrategy = new MacGitEnvironment(environment, filesystem);
            linuxBasedGitInstallationStrategy.ValidateGitInstall("asdf").Should().Be(found);
        }
    }
}