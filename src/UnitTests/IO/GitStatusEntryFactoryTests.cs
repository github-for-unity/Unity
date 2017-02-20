using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    class GitStatusEntryFactoryTests : TestBase
    {
        private static IProcessEnvironment CreateGitEnvironment(string repositoryRoot)
        {
            var gitEnvironment = Substitute.For<IProcessEnvironment>();
            gitEnvironment.FindRoot(Arg.Any<string>()).Returns(repositoryRoot);
            return gitEnvironment;
        }

        private static IEnvironment CreateEnvironment(string projectRoot)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.UnityProjectPath.Returns(projectRoot);
            return environment;
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInGitRoot()
        {
            var repositoryRoot = "/Source".ToNPath();
            var projectRoot = repositoryRoot.Combine("UnityProject");

            var gitEnvironment = CreateGitEnvironment(repositoryRoot);
            var environment = CreateEnvironment(projectRoot);
            NPathFileSystemProvider.Current = CreateFileSystem(currentDirectory: repositoryRoot);
            var root = repositoryRoot.ToString();
            environment.RepositoryPath.Returns(root);

            var filePath = "Something.sln";
            var fullPath = repositoryRoot.Combine(filePath);
            string projectPath = fullPath.RelativeTo(projectRoot);


            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(filePath, fullPath, projectPath, status);

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(filePath, status);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInProjectRoot()
        {
            var repositoryRoot = "/Source".ToNPath();
            var projectRoot = repositoryRoot.Combine("UnityProject");

            var gitEnvironment = CreateGitEnvironment(repositoryRoot);
            var environment = CreateEnvironment(projectRoot);
            NPathFileSystemProvider.Current = CreateFileSystem(currentDirectory: repositoryRoot);
            var root = repositoryRoot.ToString();
            environment.RepositoryPath.Returns(root);

            var filePath = "UnityProject/Something.sln".ToNPath();
            var fullPath = repositoryRoot.Combine(filePath);
            string projectPath = "Something.sln";

            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(filePath, fullPath, projectPath, status);

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(filePath, status);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsSameAsGitRootAndFileInGitRoot()
        {
            var repositoryRoot = "/Source".ToNPath();
            var projectRoot = repositoryRoot;

            var gitEnvironment = CreateGitEnvironment(repositoryRoot);
            var environment = CreateEnvironment(projectRoot);
            NPathFileSystemProvider.Current = CreateFileSystem(currentDirectory: repositoryRoot);
            var root = repositoryRoot.ToString();
            environment.RepositoryPath.Returns(root);

            var filePath = "Something.sln";
            var fullPath = repositoryRoot.Combine(filePath);

            string projectPath = filePath;
            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(filePath, fullPath, projectPath, status);

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(filePath, status);

            result.Should().Be(expected);
        }
    }
}