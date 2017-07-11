using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NUnit.Framework;
using TestUtils;

namespace UnitTests.UI
{

    [TestFixture, Isolated]
    public class TreeBuilderTests
    {
        private IEnvironment environment;
        private GitObjectFactory gitObjectFactory;

        [Test]
        public void CanBuildTreeForSingleItem()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry>() {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            Action<FileTreeNode> stateChangeCallback = node => { };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallback);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            gitCommitTargets.Count.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

        }

        [Test]
        public void CanBuildTreeForSingleItemWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry>() {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            Action<FileTreeNode> stateChangeCallback = node => { };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallback);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            gitCommitTargets.Count.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForMultipleSiblings()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry>() {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            Action<FileTreeNode> stateChangeCallback = node => { };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallback);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            gitCommitTargets.Count.Should().Be(2);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Label.Should().Be("file2.txt");
            children[1].Open.Should().BeTrue();
            children[1].Path.Should().Be("file2.txt");
            children[1].RepositoryPath.Should().Be("file2.txt");
            children[1].State.Should().Be(CommitState.None);
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().Be(gitCommitTargets[1]);

            children[1].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForMultipleSiblingsWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry>() {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            Action<FileTreeNode> stateChangeCallback = node => { };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallback);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            gitCommitTargets.Count.Should().Be(2);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Label.Should().Be("file2.txt");
            children[1].Open.Should().BeTrue();
            children[1].Path.Should().Be("file2.txt");
            children[1].RepositoryPath.Should().Be(@"Project\file2.txt");
            children[1].State.Should().Be(CommitState.None);
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().Be(gitCommitTargets[1]);

            children[1].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForHierarchy()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry>() {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder\file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            Action<FileTreeNode> stateChangeCallback = node => { };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallback);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            gitCommitTargets.Count.Should().Be(2);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Label.Should().Be("folder");
            children[1].Open.Should().BeTrue();
            children[1].Path.Should().Be("folder");
            children[1].RepositoryPath.Should().Be("folder");
            children[1].State.Should().Be(CommitState.None);
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().BeNull();

            var folderChildren = children[1].Children.ToArray();
            folderChildren.Length.Should().Be(1);

            folderChildren[0].Label.Should().Be("file2.txt");
            folderChildren[0].Open.Should().BeTrue();
            folderChildren[0].Path.Should().Be(@"folder\file2.txt");
            folderChildren[0].RepositoryPath.Should().Be(@"folder\file2.txt");
            folderChildren[0].State.Should().Be(CommitState.None);
            folderChildren[0].State.Should().Be(CommitState.None);
            folderChildren[0].Target.Should().Be(gitCommitTargets[1]);

            folderChildren[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForHierarchyWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry>() {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder\file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            Action<FileTreeNode> stateChangeCallback = node => { };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallback);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            gitCommitTargets.Count.Should().Be(2);

            children[0].Path.Should().Be("file1.txt");
            children[0].Label.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].State.Should().Be(CommitState.None);
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Path.Should().Be("folder");
            children[1].Label.Should().Be("folder");
            children[1].RepositoryPath.Should().Be(@"Project\folder");
            children[1].Open.Should().BeTrue();
            children[1].State.Should().Be(CommitState.None);
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().BeNull();

            var folderChildren = children[1].Children.ToArray();
            folderChildren.Length.Should().Be(1);

            folderChildren[0].Label.Should().Be("file2.txt");
            folderChildren[0].Open.Should().BeTrue();
            folderChildren[0].Path.Should().Be(@"folder\file2.txt");
            folderChildren[0].RepositoryPath.Should().Be(@"Project\folder\file2.txt");
            folderChildren[0].State.Should().Be(CommitState.None);
            folderChildren[0].State.Should().Be(CommitState.None);
            folderChildren[0].Target.Should().Be(gitCommitTargets[1]);

            folderChildren[0].Children.Should().BeEmpty();
        }

        private void InitializeEnvironment(string repositoryPath, string projectPath)
        {
            var substituteFactory = new SubstituteFactory();

            var fileSystem =
                substituteFactory.CreateFileSystem(new CreateFileSystemOptions { CurrentDirectory = projectPath });

            NPath.FileSystem = fileSystem;

            environment = substituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                RepositoryPath = repositoryPath,
                UnityProjectPath = projectPath.ToNPath(),
                Extensionfolder = projectPath.ToNPath().Combine("Assets", "Editor", "GitHub")
            });

            gitObjectFactory = new GitObjectFactory(environment);
        }
    }
}
