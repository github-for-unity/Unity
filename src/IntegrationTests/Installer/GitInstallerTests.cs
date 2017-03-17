using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture, Ignore]
    public class GitInstallerTests
    {
        private SubstituteFactory Factory { get; set; }

        private const string WindowsPortableGitZip =
            CreateEnvironmentOptions.DefaultExtensionFolder + @"\resources\windows\PortableGit-2.11.1-32-bit.zip";

        private const string WindowsGitLfsZip =
            CreateEnvironmentOptions.DefaultExtensionFolder +
            @"\resources\windows\git-lfs-windows-386-2.0-pre-d9833cd.zip";

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Factory = new SubstituteFactory();
            NPathFileSystemProvider.Current = Factory.CreateFileSystem();
        }

        [Test]
        public void ShouldExtractGitIfNeeded()
        {
            var sharpZipLibHelper = Factory.CreateSharpZipLibHelper();

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    FilesThatExist = new[] { WindowsPortableGitZip },
                    DirectoriesThatExist =
                        new[] { @"c:\UserProfile\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db" },
                    RandomFileNames = new[] { "randomFile1", "randomFile2" },
                    ChildFiles =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(
                                    CreateFileSystemOptions.DefaultTemporaryPath + @"\randomFile1.deleteme", "*",
                                    SearchOption.AllDirectories),
                                new string[0]
                            }
                        }
                });
            
            NPathFileSystemProvider.Current = fileSystem;
            var created = 0;
            fileSystem.FileExists(Args.String).Returns(info =>
            {
                var path1 = (string)info[0];

                if (path1 == @"c:\UserProfile\GitHubUnityDebug\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe")
                    return false;
                else if (path1.StartsWith(@"c:\tmp"))
                {
                    created++;
                    var ret = created > 2;
                    return ret;
                }
                return true;
            });

            fileSystem.DirectoryExists(Args.String).Returns(info =>
            {
                var path1 = (string)info[0];

                if (path1.StartsWith(@"c:\tmp"))
                {
                    created++;
                    var ret = created > 2;
                    return ret;
                }
                return true;
            });
            var portableGitManager = new GitInstaller(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
            sharpZipLibHelper);
            var tempPath = NPath.CreateTempDirectory("integration-tests");
            portableGitManager.ExtractGitIfNeeded(tempPath);
            tempPath.Delete();

            sharpZipLibHelper.Received().Extract(WindowsPortableGitZip, Args.String);

            //TODO: Write code to make sure NPath was used to copy files
        }

        [Test]
        public void ShouldExtractGitLfsIfNeeded()
        {
            var sharpZipLibHelper = Factory.CreateSharpZipLibHelper();

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    FilesThatExist = new[] { WindowsGitLfsZip },
                    FileContents = new Dictionary<string, IList<string>>(),
                    RandomFileNames = new[] { "randomFile1", "randomFile2" }
                });

            var portableGitManager = new GitInstaller(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            var tempPath = NPath.CreateTempDirectory("integration-tests");
            portableGitManager.ExtractGitIfNeeded(tempPath);
            tempPath.Delete();

            const string shouldExtractTo = CreateFileSystemOptions.DefaultTemporaryPath + @"\randomFile1.deleteme";
            sharpZipLibHelper.Received().Extract(WindowsGitLfsZip, shouldExtractTo);

            //TODO: Write code to make sure files were copied
        }

        [Test]
        public void ShouldKnoowIfGitLfsIsExtracted()
        {
            var sharpZipLibHelper = Factory.CreateSharpZipLibHelper();

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    FilesThatExist =
                        new[] {
                            CreateEnvironmentOptions.DefaultUserProfilePath +
                            @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\mingw32\libexec\git-core\git-lfs.exe"
                        },
                    FileContents = new Dictionary<string, IList<string>>()
                });

            fileSystem.FileExists(Args.String).Returns(info => true);
            var portableGitManager = new GitInstaller(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            portableGitManager.IsGitLfsExtracted().Should().BeTrue();

            //TODO: Write code to make sure file was copied
        }

        [Test]
        public void ShouldKnowGitLfsDestinationDirectory()
        {
            var portableGitManager = new GitInstaller(Factory.CreateEnvironment(), Factory.CreateSharpZipLibHelper());

            portableGitManager.GitLfsDestinationPath.Should()
                              .Be(
                                  @"c:\UserProfile\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\mingw32\libexec\git-core\git-lfs.exe");
        }

        [Test]
        public void ShouldNotExtractGitIfAlreadyPresent()
        {
            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions() {
                    FilesThatExist =
                        new[] {
                            WindowsPortableGitZip,
                            CreateEnvironmentOptions.DefaultUserProfilePath +
                            @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe"
                        }
                });

            var sharpZipLibHelper = Factory.CreateSharpZipLibHelper();

            var portableGitManager = new GitInstaller(Factory.CreateEnvironment(), sharpZipLibHelper);
            var tempPath = NPath.CreateTempDirectory("integration-tests");
            portableGitManager.ExtractGitIfNeeded(tempPath);
            tempPath.Delete();

            sharpZipLibHelper.DidNotReceiveWithAnyArgs().Extract(Args.String, Args.String);
        }

        [Test]
        public void ShouldNotExtractGitLfsIfAlreadyPresent()
        {
            var sharpZipLibHelper = Factory.CreateSharpZipLibHelper();

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions() {
                    FilesThatExist =
                        new[] {
                            CreateEnvironmentOptions.DefaultUserProfilePath +
                            @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\mingw32\libexec\git-core\git-lfs.exe"
                        },
                    FileContents = new Dictionary<string, IList<string>>()
                });

            NPathFileSystemProvider.Current = fileSystem;

            var portableGitManager = new GitInstaller(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            var tempPath = NPath.CreateTempDirectory("integration-tests");
            portableGitManager.ExtractGitIfNeeded(tempPath);
            tempPath.Delete();

            sharpZipLibHelper.DidNotReceiveWithAnyArgs().Extract(Args.String, Args.String);
        }
    }
}
