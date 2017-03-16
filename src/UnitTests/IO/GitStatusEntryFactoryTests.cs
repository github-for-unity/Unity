using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    class GitStatusEntryFactoryTests : TestBase
    {
        protected SubstituteFactory SubstituteFactory { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new SubstituteFactory();
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInGitRoot()
        {
            var repositoryRoot = "/Source".ToNPath();
            var projectRoot = repositoryRoot.Combine("UnityProject");

            var gitEnvironment = SubstituteFactory.CreateProcessEnvironment(repositoryRoot);
            var environment = SubstituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                UnityProjectPath = projectRoot
            });

            NPathFileSystemProvider.Current = SubstituteFactory.CreateFileSystem(currentDirectory: repositoryRoot);
            var root = repositoryRoot.ToString();
            environment.RepositoryPath.Returns(root);

            var filePath = "Something.sln";
            var fullPath = repositoryRoot.Combine(filePath);
            string projectPath = fullPath.RelativeTo(projectRoot);


            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(filePath, fullPath, projectPath, status);

            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(filePath, status);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInProjectRoot()
        {
            var repositoryRoot = "/Source".ToNPath();
            var projectRoot = repositoryRoot.Combine("UnityProject");

            var gitEnvironment = SubstituteFactory.CreateProcessEnvironment(repositoryRoot);
            var environment = SubstituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                UnityProjectPath = projectRoot
            });
            NPathFileSystemProvider.Current = SubstituteFactory.CreateFileSystem(currentDirectory: repositoryRoot);
            var root = repositoryRoot.ToString();
            environment.RepositoryPath.Returns(root);

            var filePath = "UnityProject/Something.sln".ToNPath();
            var fullPath = repositoryRoot.Combine(filePath);
            string projectPath = "Something.sln";

            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(filePath, fullPath, projectPath, status);

            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(filePath, status);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsSameAsGitRootAndFileInGitRoot()
        {
            var repositoryRoot = "/Source".ToNPath();
            var projectRoot = repositoryRoot;

            var gitEnvironment = SubstituteFactory.CreateProcessEnvironment(repositoryRoot);
            var environment = SubstituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                UnityProjectPath = projectRoot
            });
            NPathFileSystemProvider.Current = SubstituteFactory.CreateFileSystem(currentDirectory: repositoryRoot);
            var root = repositoryRoot.ToString();
            environment.RepositoryPath.Returns(root);

            var filePath = "Something.sln";
            var fullPath = repositoryRoot.Combine(filePath);

            string projectPath = filePath;
            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(filePath, fullPath, projectPath, status);

            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(filePath, status);

            result.Should().Be(expected);
        }
    }
}