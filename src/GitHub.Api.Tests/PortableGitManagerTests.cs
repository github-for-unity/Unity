using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class PortableGitManagerTests
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
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);

            Factory = new SubstituteFactory();
            NPathFileSystemProvider.Current = Factory.CreateFileSystem(new CreateFileSystemOptions());
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
                    FolderContents =
                        new Dictionary<SubstituteFactory.FolderContentsKey, string[]> {
                            {
                                new SubstituteFactory.FolderContentsKey(
                                    CreateFileSystemOptions.DefaultTemporaryPath + @"\randomFile1.deleteme", "*",
                                    SearchOption.AllDirectories),
                                new string[0]
                            }
                        }
                });

            NPathFileSystemProvider.Current = fileSystem;

            var portableGitManager = new PortableGitManager(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            const string shouldExtractTo = CreateFileSystemOptions.DefaultTemporaryPath + @"\randomFile1.deleteme";
            sharpZipLibHelper.Received().ExtractZipFile(WindowsPortableGitZip, shouldExtractTo);

            //TODO: Write code to make sure NPath was used to copy files
        }

        [Test]
        public void ShouldExtractGitLfsIfNeeded()
        {
            var sharpZipLibHelper = Factory.CreateSharpZipLibHelper();

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    FilesThatExist = new[] { WindowsGitLfsZip },
                    FileContents = new Dictionary<string, string[]>(),
                    RandomFileNames = new[] { "randomFile1", "randomFile2" }
                });

            var portableGitManager = new PortableGitManager(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            portableGitManager.ExtractGitLfsIfNeeded();

            const string shouldExtractTo = CreateFileSystemOptions.DefaultTemporaryPath + @"\randomFile1.deleteme";
            sharpZipLibHelper.Received().ExtractZipFile(WindowsGitLfsZip, shouldExtractTo);

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
                    FileContents = new Dictionary<string, string[]>()
                });

            var portableGitManager = new PortableGitManager(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            portableGitManager.IsGitLfsExtracted().Should().BeTrue();

            //TODO: Write code to make sure file was copied
        }

        [Test]
        public void ShouldKnowGitLfsDestinationDirectory()
        {
            var portableGitManager = new PortableGitManager(Factory.CreateEnvironment(), Factory.CreateSharpZipLibHelper());

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

            var portableGitManager = new PortableGitManager(Factory.CreateEnvironment(), sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            sharpZipLibHelper.DidNotReceiveWithAnyArgs().ExtractZipFile(Arg.Any<string>(), Arg.Any<string>());
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
                    FileContents = new Dictionary<string, string[]>()
                });

            NPathFileSystemProvider.Current = fileSystem;

            var portableGitManager = new PortableGitManager(Factory.CreateEnvironment(new CreateEnvironmentOptions()),
                sharpZipLibHelper);
            portableGitManager.ExtractGitLfsIfNeeded();

            sharpZipLibHelper.DidNotReceiveWithAnyArgs().ExtractZipFile(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
