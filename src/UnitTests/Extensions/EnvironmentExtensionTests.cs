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
    public class GetRepositoryPathTests
    {
        private SubstituteFactory SubstituteFactory { get; } = new SubstituteFactory();

        [Test]
        public void Should_When_Project_Repository_Roots_AreEqual() => Assert(@"c:\UnityProject", @"c:\UnityProject", "test.txt", "test.txt");

        [Test]
        public void Should_When_Project_Root_IsChild() => Assert(@"c:\Projects", @"c:\Projects\UnityProject", "test.txt", @"UnityProject\test.txt");

        [Test]
        public void Should_Not_When_Repository_Root_IsChild() => AssertThrows<Exception>(@"c:\Projects\UnityProject\Assets", @"c:\Projects\UnityProject\", "test.txt");

        private void Assert(string repositoryPath, string unityProjectPath, string assetPath, string expectedPath)
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());

            NPathFileSystemProvider.Current.Should().BeNull();
            NPathFileSystemProvider.Current = fileSystem;

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(unityProjectPath);

            var repositoryFilePath = environment.GetRepositoryPath(assetPath);
            repositoryFilePath.Should().Be(expectedPath);

            NPathFileSystemProvider.Current.Should().NotBeNull();
            NPathFileSystemProvider.Current = null;
        }

        private void AssertThrows<T>(string repositoryPath, string unityProjectPath, string assetPath) where T : Exception
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());

            NPathFileSystemProvider.Current.Should().BeNull();
            NPathFileSystemProvider.Current = fileSystem;

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath);
            environment.UnityProjectPath.Returns(unityProjectPath);

            Action action = () => { environment.GetRepositoryPath(assetPath); };
            action.ShouldThrow<T>();

            NPathFileSystemProvider.Current.Should().NotBeNull();
            NPathFileSystemProvider.Current = null;
        }
    }
}