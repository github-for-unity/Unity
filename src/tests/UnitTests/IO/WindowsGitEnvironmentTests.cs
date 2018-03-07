using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Environment = System.Environment;
using GitHub.Unity;
using TestUtils;

namespace UnitTests
{
    public class WindowsGitEnvironmentTests : GitEnvironmentTestsBase
    {
        public static IEnumerable<TestCaseData> GetDefaultGitPath_TestCases()
        {
            const string localAppDataPath = @"C:\Users\Stanley\AppData\Local";

            var gitHubRootPath = Path.Combine(localAppDataPath, "GitHub");

            var gitHubRootPathChildren = new[]
                {
                    "IgnoreTemplates_d0aa732a2b4666b3029e2320f1a06cd39e99c9fc",
                    "lfs-amd64_1.3.1",
                    "PortableGit_d7effa1a4a322478cd29c826b52a0c118ad3db11",
                    "TutorialRepository_d0aa732a2b4666b3029e2320f1a06cd39e99c9fc",
                }
                .Select(s => Path.Combine(gitHubRootPath, s))
                .ToArray();

            var gitExecutablePath = Path.Combine(localAppDataPath, @"GitHub\PortableGit_d7effa1a4a322478cd29c826b52a0c118ad3db11\cmd\git.exe");

            var testCase = new TestCaseData(localAppDataPath, gitHubRootPath, gitHubRootPathChildren, gitExecutablePath);
            testCase.SetName("Should be found");
            yield return testCase;

            gitHubRootPathChildren = new[]
                {
                    "IgnoreTemplates_d0aa732a2b4666b3029e2320f1a06cd39e99c9fc",
                    "lfs-amd64_1.3.1",
                    "TutorialRepository_d0aa732a2b4666b3029e2320f1a06cd39e99c9fc",
                }
                .Select(s => Path.Combine(gitHubRootPath, s))
                .ToArray();

            testCase = new TestCaseData(localAppDataPath, gitHubRootPath, gitHubRootPathChildren, null);
            testCase.SetName("Should be null");
            yield return testCase;
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
            var filesystem = Substitute.For<IFileSystem>();
            filesystem.FileExists(Args.String).Returns(inFileSystem);

            //var linuxBasedGitInstallationStrategy = new ProcessEnvironment(environment);
            //linuxBasedGitInstallationStrategy.ValidateGitInstall("asdf").Should().Be(found);
        }
    }
}