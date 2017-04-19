using System;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture, Isolated]
    public class EnvironmentExtensionTests
    {
        private SubstituteFactory SubstituteFactory { get; } = new SubstituteFactory();

        [SetUp]
        public void TestSetup()
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());
            NPathFileSystemProvider.Current = fileSystem;
        }

        [TearDown]
        public void TestTearDown()
        {
            NPathFileSystemProvider.Current = null;
        }

        [Test, Sequential]
        public void GetRepositoryPathReturnsRelativePathToRepository(
            [Values(@"c:\UnityProject", "/Projects")] string repositoryPath,
            [Values(@"c:\UnityProject", "/Projects/UnityProject")]string projectPath,
            [Values(@"test.txt", "test.txt")]string path,
            [Values(@"test.txt", "UnityProject/test.txt")]string expected)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            NPath nExpected = expected;
            var repositoryFilePath = environment.GetRepositoryPath(path);
            repositoryFilePath.Should().Be(nExpected);
        }

        [Test, Sequential]
        [ExpectedException(ExpectedException = typeof(InvalidOperationException))]
        public void GetRepositoryPathThrowsWhenRepositoryIsChildOfProject(
            [Values(@"c:\UnityProject\UnityProject\Assets")] string repositoryPath,
            [Values(@"c:\UnityProject\UnityProject")]string projectPath,
            [Values(@"test.txt")]string path)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            var repositoryFilePath = environment.GetRepositoryPath(path);
        }

        [Test, Sequential]
        public void GetAssetPathReturnsRelativePathToProject(
            [Values(@"c:\Projects", "/Projects", "/UnityProject")] string repositoryPath,
            [Values(@"c:\Projects\UnityProject", "/Projects/Unity/UnityProject", "/UnityProject")] string projectPath,
            [Values(@"UnityProject\test.txt", "Unity/UnityProject/Assets/test.txt", "test.txt")] string path,
            [Values("test.txt", "Assets/test.txt", "test.txt")] string expected)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            NPath nExpected = expected;
            var repositoryFilePath = environment.GetAssetPath(path);
            repositoryFilePath.Should().Be(nExpected);
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(InvalidOperationException))]
        public void GetAssetPathShouldThrowWhenRepositoryRootIsChild(
            [Values(@"c:\Projects\UnityProject\Assets")] string repositoryPath,
            [Values(@"c:\Projects\UnityProject")] string projectPath,
            [Values("test.txt")] string path)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            var repositoryFilePath = environment.GetAssetPath(path);
        }
    }
}