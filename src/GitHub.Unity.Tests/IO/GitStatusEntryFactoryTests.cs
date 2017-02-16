using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class GitStatusEntryFactoryTests
    {
        private static IFileSystem CreateFileSystem()
        {
            var filesystem = Substitute.For<IFileSystem>();
            filesystem.Combine(Arg.Any<string>(), Arg.Any<string>())
                .Returns(info => Path.Combine((string)info[0], (string)info[1]));

            filesystem.GetFullPath(Arg.Any<string>())
                .Returns(info => Path.GetFullPath((string)info[0]));
            return filesystem;
        }

        private static IGitEnvironment CreateGitEnvironment(string gitRoot)
        {
            var gitEnvironment = Substitute.For<IGitEnvironment>();
            gitEnvironment.FindRoot(Arg.Any<string>()).Returns(gitRoot);
            return gitEnvironment;
        }

        private static IEnvironment CreateEnvironment(string projectRoot)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.UnityProjectPath.Returns(projectRoot);
            return environment;
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootFileInGitRoot()
        {
            var gitEnvironment = CreateGitEnvironment(@"c:\Source\");

            var environment = CreateEnvironment(@"c:\Source\UnityProject");

            var path = @"Something.sln";
            var fullPath = @"c:\Source\Something.sln";
            string projectPath = null;
            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(path, fullPath, projectPath, status);

            var filesystem = CreateFileSystem();
            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(path, status);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInProjectRoot()
        {
            var gitEnvironment = CreateGitEnvironment(@"c:\Source\");

            var environment = CreateEnvironment(@"c:\Source\UnityProject");

            var path = @"UnityProject\Something.sln";
            var fullPath = @"c:\Source\UnityProject\Something.sln";
            string projectPath = null;
            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(path, fullPath, projectPath, status);

            var filesystem = CreateFileSystem();

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(path, status);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsSameAsGitRootAndFileInGitRoot()
        {
            var gitEnvironment = CreateGitEnvironment(@"c:\UnityProject\");

            var environment = CreateEnvironment(@"c:\UnityProject\");

            var path = @"Something.sln";
            var fullPath = @"c:\UnityProject\Something.sln";
            string projectPath = null;
            var status = GitFileStatus.Added;

            var expected = new GitStatusEntry(path, fullPath, projectPath, status);

            var filesystem = CreateFileSystem();

            var gitStatusEntryFactory = new GitObjectFactory(environment, gitEnvironment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(path, status);

            result.Should().Be(expected);
        }
    }
}