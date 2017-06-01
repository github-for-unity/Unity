using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;
using TestUtils;

namespace UnitTests
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
        //    filesystem.FileExists(Args.String).Returns(fileFound);

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
            filesystem.FileExists(Args.String).Returns(inFileSystem);

            //var linuxBasedGitInstallationStrategy = new LinuxEnvironment(environment);
            //linuxBasedGitInstallationStrategy.ValidateGitInstall("asdf".ToNPath()).Should().Be(found);
        }

        [TestCase(@"c:\Source\file.txt", @"c:\Source", TestName = "should be found")]
        [TestCase(@"c:\Documents\file.txt", null, TestName = "file outside root should not be found")]
        [TestCase(@"c:\file.txt", null, TestName = "file outside root inside sibling should not be found")]
        public void FindRoot(string input, string expected)
        {
            var fs = (IFileSystem)BuildFindRootFileSystem();
            NPath.FileSystem = fs;

            var environment = Substitute.For<IEnvironment>();
            environment.FileSystem.Returns(fs);

            var windowsGitEnvironment = new ProcessEnvironment(environment);
            var result = windowsGitEnvironment.FindRoot(input.ToNPath());

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