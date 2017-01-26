using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LinuxBasedGitEnvironmentTests
    {
        public static IEnumerable<TestCaseData> GetDefaultGitPath_TestCases()
        {
            var testCase = new TestCaseData(true, LinuxBasedGitEnvironment.DefaultGitPath);
            testCase.SetName("Should be found");
            yield return testCase;

            testCase = new TestCaseData(false, null);
            testCase.SetName("Should be null");
            yield return testCase;
        }

        [TestCaseSource(nameof(GetDefaultGitPath_TestCases))]
        public void GetDefaultGitPath(bool fileFound, string filePath)
        {
            var environment = Substitute.For<IEnvironment>();

            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.FileExists(Arg.Any<string>()).Returns(fileFound);

            var linuxBasedGitInstallationStrategy = new LinuxBasedGitEnvironment(fileSystem, environment);
            linuxBasedGitInstallationStrategy.FindGitInstallationPath().Should().Be(filePath);
        }

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

            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.FileExists(Arg.Any<string>()).Returns(inFileSystem);

            var linuxBasedGitInstallationStrategy = new LinuxBasedGitEnvironment(fileSystem, environment);
            linuxBasedGitInstallationStrategy.ValidateGitInstall("asdf").Should().Be(found);
        }
    }
}