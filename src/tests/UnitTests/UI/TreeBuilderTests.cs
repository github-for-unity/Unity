using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace UnitTests.UI
{

    [TestFixture, Isolated]
    public class TreeBuilderTests
    {
        private ILogging logger = Logging.GetLogger<TreeBuilderTests>();

        private IEnvironment environment;
        private GitObjectFactory gitObjectFactory;

        [Test]
        public void CanBuildTreeForSingleItem()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);
            
            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            gitCommitTargets.Count.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForSingleItemWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForSingleItemInFolder()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry(@"folder\file1.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"folder\file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForSingleItemInFolderWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry(@"folder\file1.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\folder\file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForMultipleSiblings()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Label.Should().Be("file2.txt");
            children[1].Open.Should().BeTrue();
            children[1].Path.Should().Be("file2.txt");
            children[1].RepositoryPath.Should().Be("file2.txt");
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().Be(gitCommitTargets[1]);

            children[1].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForMultipleSiblingsWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Label.Should().Be("file2.txt");
            children[1].Open.Should().BeTrue();
            children[1].Path.Should().Be("file2.txt");
            children[1].RepositoryPath.Should().Be(@"Project\file2.txt");
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().Be(gitCommitTargets[1]);

            children[1].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForHierarchy()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder1\file2.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder1\file3.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder2\file4.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder2\file5.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(5);
            gitCommitTargets.Count.Should().Be(5);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(3);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Label.Should().Be("folder1");
            children[1].Open.Should().BeTrue();
            children[1].Path.Should().Be("folder1");
            children[1].RepositoryPath.Should().Be("folder1");
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().BeNull();

            children[2].Label.Should().Be("folder2");
            children[2].Open.Should().BeTrue();
            children[2].Path.Should().Be("folder2");
            children[2].RepositoryPath.Should().Be("folder2");
            children[2].State.Should().Be(CommitState.None);
            children[2].Target.Should().BeNull();

            var folder1Children = children[1].Children.ToArray();
            folder1Children.Length.Should().Be(2);

            folder1Children[0].Label.Should().Be("file2.txt");
            folder1Children[0].Open.Should().BeTrue();
            folder1Children[0].Path.Should().Be(@"folder1\file2.txt");
            folder1Children[0].RepositoryPath.Should().Be(@"folder1\file2.txt");
            folder1Children[0].State.Should().Be(CommitState.None);
            folder1Children[0].Target.Should().Be(gitCommitTargets[1]);

            folder1Children[0].Children.Should().BeEmpty();

            folder1Children[1].Label.Should().Be("file3.txt");
            folder1Children[1].Open.Should().BeTrue();
            folder1Children[1].Path.Should().Be(@"folder1\file3.txt");
            folder1Children[1].RepositoryPath.Should().Be(@"folder1\file3.txt");
            folder1Children[1].State.Should().Be(CommitState.None);
            folder1Children[1].Target.Should().Be(gitCommitTargets[2]);

            folder1Children[1].Children.Should().BeEmpty();

            var folder2Children = children[2].Children.ToArray();
            folder2Children.Length.Should().Be(2);

            folder2Children[0].Label.Should().Be("file4.txt");
            folder2Children[0].Open.Should().BeTrue();
            folder2Children[0].Path.Should().Be(@"folder2\file4.txt");
            folder2Children[0].RepositoryPath.Should().Be(@"folder2\file4.txt");
            folder2Children[0].State.Should().Be(CommitState.None);
            folder2Children[0].Target.Should().Be(gitCommitTargets[3]);

            folder2Children[0].Children.Should().BeEmpty();

            folder2Children[1].Label.Should().Be("file5.txt");
            folder2Children[1].Open.Should().BeTrue();
            folder2Children[1].Path.Should().Be(@"folder2\file5.txt");
            folder2Children[1].RepositoryPath.Should().Be(@"folder2\file5.txt");
            folder2Children[1].State.Should().Be(CommitState.None);
            folder2Children[1].Target.Should().Be(gitCommitTargets[4]);

            folder2Children[1].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForHierarchyWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder\file2.txt", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(2);

            children[0].Path.Should().Be("file1.txt");
            children[0].Label.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be(@"Project\file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            children[0].Children.Should().BeEmpty();

            children[1].Path.Should().Be("folder");
            children[1].Label.Should().Be("folder");
            children[1].RepositoryPath.Should().Be(@"Project\folder");
            children[1].Open.Should().BeTrue();
            children[1].State.Should().Be(CommitState.None);
            children[1].Target.Should().BeNull();

            var folderChildren = children[1].Children.ToArray();
            folderChildren.Length.Should().Be(1);

            folderChildren[0].Label.Should().Be("file2.txt");
            folderChildren[0].Open.Should().BeTrue();
            folderChildren[0].Path.Should().Be(@"folder\file2.txt");
            folderChildren[0].RepositoryPath.Should().Be(@"Project\folder\file2.txt");
            folderChildren[0].State.Should().Be(CommitState.None);
            folderChildren[0].Target.Should().Be(gitCommitTargets[1]);

            folderChildren[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForItemAndMetafile()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file1.txt.meta", GitFileStatus.Modified)
            };
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(1);

            children[0].Label.Should().Be("file1.txt");
            children[0].Open.Should().BeTrue();
            children[0].Path.Should().Be("file1.txt");
            children[0].RepositoryPath.Should().Be("file1.txt");
            children[0].State.Should().Be(CommitState.None);
            children[0].Target.Should().Be(gitCommitTargets[0]);

            var fileChildren = children[0].Children.ToArray();
            fileChildren.Length.Should().Be(1);

            fileChildren[0].Label.Should().Be("file1.txt.meta");
            fileChildren[0].Open.Should().BeTrue();
            fileChildren[0].Path.Should().Be("file1.txt.meta");

            //TODO: Understand this as this is unexpected
            fileChildren[0].RepositoryPath.Should().Be(@"file1.txt\file1.txt.meta");

            fileChildren[0].State.Should().Be(CommitState.None);
            fileChildren[0].State.Should().Be(CommitState.None);
            fileChildren[0].Target.Should().Be(gitCommitTargets[1]);

            fileChildren[0].Children.Should().BeEmpty();
        }

        [Test]
        public void CanUpdateTreeForSingleItem()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var gitStatusEntries = new List<GitStatusEntry>();

            var gitStatusEntriesGen1 = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRootGen1 = TreeBuilder.BuildTreeRoot(gitStatusEntriesGen1, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var fileTreeNodesGen1 = treeRootGen1.Children.ToArray();

            fileTreeNodesGen1.Length.Should().Be(2);

            fileTreeNodesGen1[0].Label.Should().Be("file1.txt");
            fileTreeNodesGen1[0].Open.Should().BeTrue();
            fileTreeNodesGen1[0].Path.Should().Be("file1.txt");
            fileTreeNodesGen1[0].RepositoryPath.Should().Be("file1.txt");
            fileTreeNodesGen1[0].State.Should().Be(CommitState.None);
            fileTreeNodesGen1[0].Target.Should().Be(gitCommitTargets[0]);
            fileTreeNodesGen1[0].Children.Should().BeEmpty();

            fileTreeNodesGen1[1].Label.Should().Be("file2.txt");
            fileTreeNodesGen1[1].Open.Should().BeTrue();
            fileTreeNodesGen1[1].Path.Should().Be("file2.txt");
            fileTreeNodesGen1[1].RepositoryPath.Should().Be("file2.txt");
            fileTreeNodesGen1[1].State.Should().Be(CommitState.None);
            fileTreeNodesGen1[1].Target.Should().Be(gitCommitTargets[1]);
            fileTreeNodesGen1[1].Children.Should().BeEmpty();

            fileTreeNodesGen1[0].State = CommitState.All;

            stateChangeCallbackListener.ReceivedWithAnyArgs(1).StateChangeCallback(Arg.Any<FileTreeNode>());
            stateChangeCallbackListener.ClearReceivedCalls();

            var gitStatusEntriesGen2 = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file3.txt", GitFileStatus.Modified)
            };

            var treeRootGen2 = TreeBuilder.BuildTreeRoot(gitStatusEntriesGen2, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries, stateChangeCallbackListener.StateChangeCallback);

            gitStatusEntries.Count.Should().Be(2);

            var fileTreeNodesGen2 = treeRootGen2.Children.ToArray();

            fileTreeNodesGen1.Length.Should().Be(2);

            fileTreeNodesGen2[0].Label.Should().Be("file1.txt");
            fileTreeNodesGen2[0].Open.Should().BeTrue();
            fileTreeNodesGen2[0].Path.Should().Be("file1.txt");
            fileTreeNodesGen2[0].RepositoryPath.Should().Be("file1.txt");
            fileTreeNodesGen2[0].State.Should().Be(CommitState.All);
            fileTreeNodesGen2[0].Target.Should().Be(gitCommitTargets[0]);
            fileTreeNodesGen2[0].Children.Should().BeEmpty();

            fileTreeNodesGen2[1].Label.Should().Be("file3.txt");
            fileTreeNodesGen2[1].Open.Should().BeTrue();
            fileTreeNodesGen2[1].Path.Should().Be("file3.txt");
            fileTreeNodesGen2[1].RepositoryPath.Should().Be("file3.txt");
            fileTreeNodesGen2[1].State.Should().Be(CommitState.None);
            fileTreeNodesGen2[1].Target.Should().Be(gitCommitTargets[1]);
            fileTreeNodesGen2[1].Children.Should().BeEmpty();
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

    interface IStateChangeCallbackListener
    {
        void StateChangeCallback(FileTreeNode fileTreeNode);
    }
}
