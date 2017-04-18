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

        [Test]
        public void GetRepositoryPathShouldReturnWhenProjectRepositoryRootsAreEqual() => AssertGetRepositoryPath(@"c:\UnityProject", @"c:\UnityProject", "test.txt", "test.txt");

        [Test]
        public void GetRepositoryPathShouldReturnWhenProjectRootIsChild() => AssertGetRepositoryPath(@"c:\Projects", @"c:\Projects\UnityProject", "test.txt", @"UnityProject\test.txt");

        [Test]
        public void GetRepositoryPathShouldThrowWhenRepositoryRootIsChild() => AssertGetRepositoryPathThrows<Exception>(@"c:\Projects\UnityProject\Assets", @"c:\Projects\UnityProject\", "test.txt");

        [Test]
        public void GetAssetPathShouldReturnWhenProjectRepositoryRootsAreEqual() => AssertGetAssetPath(@"c:\UnityProject", @"c:\UnityProject", "test.txt", "test.txt");

        [Test]
        public void GetAssetPathShouldReturnWhenProjectRootIsChild() => AssertGetAssetPath(@"c:\Projects", @"c:\Projects\UnityProject", @"UnityProject\test.txt", "test.txt");

        [Test]
        public void GetAssetPathShouldThrowWhenRepositoryRootIsChild() => AssertGetAssetPathThrows<Exception>(@"c:\Projects\UnityProject\Assets", @"c:\Projects\UnityProject\", "test.txt");

        private void AssertGetRepositoryPath(string repositoryPath, string projectPath, string path, string expected)
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());

            NPathFileSystemProvider.Current.Should().BeNull();
            NPathFileSystemProvider.Current = fileSystem;

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            var repositoryFilePath = environment.GetRepositoryPath(path);
            repositoryFilePath.Should().Be(expected);

            NPathFileSystemProvider.Current.Should().NotBeNull();
            NPathFileSystemProvider.Current = null;
        }

        private void AssertGetRepositoryPathThrows<T>(string repositoryPath, string projectPath, string path) where T : Exception
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());

            NPathFileSystemProvider.Current.Should().BeNull();
            NPathFileSystemProvider.Current = fileSystem;

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            Action action = () => { environment.GetRepositoryPath(path); };
            action.ShouldThrow<T>();

            NPathFileSystemProvider.Current.Should().NotBeNull();
            NPathFileSystemProvider.Current = null;
        }

        private void AssertGetAssetPath(string repositoryPath, string projectPath, string path, string expected)
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());

            NPathFileSystemProvider.Current.Should().BeNull();
            NPathFileSystemProvider.Current = fileSystem;

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            var repositoryFilePath = environment.GetAssetPath(path);
            repositoryFilePath.Should().Be(expected);

            NPathFileSystemProvider.Current.Should().NotBeNull();
            NPathFileSystemProvider.Current = null;
        }

        private void AssertGetAssetPathThrows<T>(string repositoryPath, string projectPath, string path) where T : Exception
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());

            NPathFileSystemProvider.Current.Should().BeNull();
            NPathFileSystemProvider.Current = fileSystem;

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(projectPath);

            Action action = () => { environment.GetAssetPath(path); };
            action.ShouldThrow<T>();

            NPathFileSystemProvider.Current.Should().NotBeNull();
            NPathFileSystemProvider.Current = null;
        }
    }
}