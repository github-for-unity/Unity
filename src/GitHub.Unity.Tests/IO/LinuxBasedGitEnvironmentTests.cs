using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Api;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LinuxGitEnvironmentTests: GitEnvironmentTestsBase
    {
        //public static IEnumerable<TestCaseData> GetDefaultGitPath_TestCases()
        //{
        //    var testCase = new TestCaseData(true, LinuxGitEnvironment.DefaultGitPath);
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
        //    filesystem.FileExists(Arg.Any<string>()).Returns(fileFound);

        //    var linuxBasedGitInstallationStrategy = new LinuxGitEnvironment(environment, filesystem);
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
            filesystem.FileExists(Arg.Any<string>()).Returns(inFileSystem);

            var linuxBasedGitInstallationStrategy = new LinuxGitEnvironment(environment, filesystem);
            linuxBasedGitInstallationStrategy.ValidateGitInstall("asdf").Should().Be(found);
        }

        [TestCase(@"c:\Source\file.txt", @"c:\Source", TestName = "should be found")]
        [TestCase(@"c:\Documents\file.txt", null, TestName = "file outside root should not be found")]
        [TestCase(@"c:\file.txt", null, TestName = "file outside root inside sibling should not be found")]
        public void FindRoot(string input, string expected)
        {
            var filesystem = (IFileSystem)BuildFindRootFileSystem();

            var environment = Substitute.For<IEnvironment>();

            var windowsGitEnvironment = new LinuxGitEnvironment(environment, filesystem);
            var result = windowsGitEnvironment.FindRoot(input);

            if (expected == null)
            {
                result.Should().BeNull();
            }
            else
            {
                result.Should().Be(expected);
            }
        }
    }
}