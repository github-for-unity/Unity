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

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);
            
            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(1);

            gitCommitTargets.Count.Should().Be(1);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForSingleItemWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(1);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be(@"Project\file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForSingleItemInFolder()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry(@"folder\file1.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(1);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be(@"folder\file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForSingleItemInFolderWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry(@"folder\file1.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(1);
            gitCommitTargets.Count.Should().Be(1);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(1);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be(@"Project\folder\file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForMultipleSiblings()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(2);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();

            var file2 = treeRootChidren[1];

            file2.Label.Should().Be("file2.txt");
            file2.Open.Should().BeTrue();
            file2.Path.Should().Be("file2.txt");
            file2.RepositoryPath.Should().Be("file2.txt");
            file2.State.Should().Be(CommitState.None);
            file2.Target.Should().Be(gitCommitTargets[1]);

            file2.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForMultipleSiblingsWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(2);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be(@"Project\file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();

            var file2 = treeRootChidren[1];

            file2.Label.Should().Be("file2.txt");
            file2.Open.Should().BeTrue();
            file2.Path.Should().Be("file2.txt");
            file2.RepositoryPath.Should().Be(@"Project\file2.txt");
            file2.State.Should().Be(CommitState.None);
            file2.Target.Should().Be(gitCommitTargets[1]);

            file2.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForHierarchy()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder1\file2.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder1\file3.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder2\file4.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder2\file5.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(5);
            gitCommitTargets.Count.Should().Be(5);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(3);

            var file1 = treeRootChidren[0];
            var folder1 = treeRootChidren[1];
            var folder2 = treeRootChidren[2];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();

            folder1.Label.Should().Be("folder1");
            folder1.Open.Should().BeTrue();
            folder1.Path.Should().Be("folder1");
            folder1.RepositoryPath.Should().Be("folder1");
            folder1.State.Should().Be(CommitState.None);
            folder1.Target.Should().BeNull();

            folder2.Label.Should().Be("folder2");
            folder2.Open.Should().BeTrue();
            folder2.Path.Should().Be("folder2");
            folder2.RepositoryPath.Should().Be("folder2");
            folder2.State.Should().Be(CommitState.None);
            folder2.Target.Should().BeNull();

            var folder1Children = folder1.Children.ToArray();
            folder1Children.Length.Should().Be(2);

            var file2 = folder1Children[0];
            var file3 = folder1Children[1];

            file2.Label.Should().Be("file2.txt");
            file2.Open.Should().BeTrue();
            file2.Path.Should().Be(@"folder1\file2.txt");
            file2.RepositoryPath.Should().Be(@"folder1\file2.txt");
            file2.State.Should().Be(CommitState.None);
            file2.Target.Should().Be(gitCommitTargets[1]);

            file2.Children.Should().BeEmpty();

            file3.Label.Should().Be("file3.txt");
            file3.Open.Should().BeTrue();
            file3.Path.Should().Be(@"folder1\file3.txt");
            file3.RepositoryPath.Should().Be(@"folder1\file3.txt");
            file3.State.Should().Be(CommitState.None);
            file3.Target.Should().Be(gitCommitTargets[2]);

            file3.Children.Should().BeEmpty();

            var folder2Children = folder2.Children.ToArray();
            folder2Children.Length.Should().Be(2);

            var file4 = folder2Children[0];
            var file5 = folder2Children[1];

            file4.Label.Should().Be("file4.txt");
            file4.Open.Should().BeTrue();
            file4.Path.Should().Be(@"folder2\file4.txt");
            file4.RepositoryPath.Should().Be(@"folder2\file4.txt");
            file4.State.Should().Be(CommitState.None);
            file4.Target.Should().Be(gitCommitTargets[3]);

            file4.Children.Should().BeEmpty();

            file5.Label.Should().Be("file5.txt");
            file5.Open.Should().BeTrue();
            file5.Path.Should().Be(@"folder2\file5.txt");
            file5.RepositoryPath.Should().Be(@"folder2\file5.txt");
            file5.State.Should().Be(CommitState.None);
            file5.Target.Should().Be(gitCommitTargets[4]);

            file5.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForHierarchyWhenProjectNestedInRepo()
        {
            InitializeEnvironment(@"c:\Repo", @"c:\Repo\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder\file2.txt", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(2);

            var file1 = treeRootChidren[0];
            var folder = treeRootChidren[1];

            file1.Path.Should().Be("file1.txt");
            file1.Label.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be(@"Project\file1.txt");
            file1.Open.Should().BeTrue();
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();

            folder.Path.Should().Be("folder");
            folder.Label.Should().Be("folder");
            folder.RepositoryPath.Should().Be(@"Project\folder");
            folder.Open.Should().BeTrue();
            folder.State.Should().Be(CommitState.None);
            folder.Target.Should().BeNull();

            var folderChildren = folder.Children.ToArray();
            folderChildren.Length.Should().Be(1);

            var file2 = folderChildren[0];

            file2.Label.Should().Be("file2.txt");
            file2.Open.Should().BeTrue();
            file2.Path.Should().Be(@"folder\file2.txt");
            file2.RepositoryPath.Should().Be(@"Project\folder\file2.txt");
            file2.State.Should().Be(CommitState.None);
            file2.Target.Should().Be(gitCommitTargets[1]);

            file2.Children.Should().BeEmpty();
        }

        [Test]
        public void CanBuildTreeForItemAndMetafile()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();
            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file1.txt.meta", GitFileStatus.Modified)
            };

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(1);

            var file1 = treeRootChidren[0];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            var fileChildren = file1.Children.ToArray();
            fileChildren.Length.Should().Be(1);

            var file1Meta = fileChildren[0];

            file1Meta.Label.Should().Be("file1.txt.meta");
            file1Meta.Open.Should().BeTrue();
            file1Meta.Path.Should().Be("file1.txt.meta");

            //TODO: Understand this as this is unexpected
            file1Meta.RepositoryPath.Should().Be(@"file1.txt\file1.txt.meta");

            file1Meta.State.Should().Be(CommitState.None);
            file1Meta.State.Should().Be(CommitState.None);
            file1Meta.Target.Should().Be(gitCommitTargets[1]);

            file1Meta.Children.Should().BeEmpty();
        }

        [Test]
        public void CanUpdateTreeAndMaintainSelectedState()
        {
            InitializeEnvironment(@"c:\Project", @"c:\Project");

            var gitStatusEntries = new List<GitStatusEntry>();
            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries = new List<string>();

            var newGitStatusEntries1 = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file2.txt", GitFileStatus.Modified)
            };

            var stateChangeCallbackListener = Substitute.For<IStateChangeCallbackListener>();

            var treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries1, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            var treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(2);

            var file1 = treeRootChidren[0];
            var file2 = treeRootChidren[1];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);
            file1.Children.Should().BeEmpty();

            file2.Label.Should().Be("file2.txt");
            file2.Open.Should().BeTrue();
            file2.Path.Should().Be("file2.txt");
            file2.RepositoryPath.Should().Be("file2.txt");
            file2.State.Should().Be(CommitState.None);
            file2.Target.Should().Be(gitCommitTargets[1]);
            file2.Children.Should().BeEmpty();

            file1.State = CommitState.All;
            file2.State = CommitState.All;

            stateChangeCallbackListener.ClearReceivedCalls();

            var newGitStatusEntries2 = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry("file1.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry("file3.txt", GitFileStatus.Modified)
            };

            treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries2, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(2);

            var file3 = treeRootChidren[1];

            gitStatusEntries.Count.Should().Be(2);
            gitCommitTargets.Count.Should().Be(2);
            foldedTreeEntries.Count.Should().Be(0);

            treeRootChidren = treeRoot.Children.ToArray();
            treeRootChidren.Length.Should().Be(2);

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.All);
            file1.Target.Should().Be(gitCommitTargets[0]);
            file1.Children.Should().BeEmpty();

            file3.Label.Should().Be("file3.txt");
            file3.Open.Should().BeTrue();
            file3.Path.Should().Be("file3.txt");
            file3.RepositoryPath.Should().Be("file3.txt");
            file3.State.Should().Be(CommitState.None);
            file3.Target.Should().Be(gitCommitTargets[1]);
            file3.Children.Should().BeEmpty();
        }

        [Test]
        public void CanUpdateTreeAndMaintainFoldedState()
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
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(5);
            gitCommitTargets.Count.Should().Be(5);
            foldedTreeEntries.Count.Should().Be(0);

            var children = treeRoot.Children.ToArray();
            children.Length.Should().Be(3);

            var file1 = children[0];
            var folder1 = children[1];
            var folder2 = children[2];

            file1.Label.Should().Be("file1.txt");
            file1.Open.Should().BeTrue();
            file1.Path.Should().Be("file1.txt");
            file1.RepositoryPath.Should().Be("file1.txt");
            file1.State.Should().Be(CommitState.None);
            file1.Target.Should().Be(gitCommitTargets[0]);

            file1.Children.Should().BeEmpty();

            folder1.Label.Should().Be("folder1");
            folder1.Open.Should().BeTrue();
            folder1.Path.Should().Be("folder1");
            folder1.RepositoryPath.Should().Be("folder1");
            folder1.State.Should().Be(CommitState.None);
            folder1.Target.Should().BeNull();

            folder2.Label.Should().Be("folder2");
            folder2.Open.Should().BeTrue();
            folder2.Path.Should().Be("folder2");
            folder2.RepositoryPath.Should().Be("folder2");
            folder2.State.Should().Be(CommitState.None);
            folder2.Target.Should().BeNull();

            var folder1Children = folder1.Children.ToArray();
            folder1Children.Length.Should().Be(2);

            var file2 = folder1Children[0];
            var file3 = folder1Children[1];

            file2.Label.Should().Be("file2.txt");
            file2.Open.Should().BeTrue();
            file2.Path.Should().Be(@"folder1\file2.txt");
            file2.RepositoryPath.Should().Be(@"folder1\file2.txt");
            file2.State.Should().Be(CommitState.None);
            file2.Target.Should().Be(gitCommitTargets[1]);

            file2.Children.Should().BeEmpty();

            file3.Label.Should().Be("file3.txt");
            file3.Open.Should().BeTrue();
            file3.Path.Should().Be(@"folder1\file3.txt");
            file3.RepositoryPath.Should().Be(@"folder1\file3.txt");
            file3.State.Should().Be(CommitState.None);
            file3.Target.Should().Be(gitCommitTargets[2]);

            file3.Children.Should().BeEmpty();

            var folder2Children = folder2.Children.ToArray();
            folder2Children.Length.Should().Be(2);

            var file4 = folder2Children[0];
            var file5 = folder2Children[1];

            file4.Label.Should().Be("file4.txt");
            file4.Open.Should().BeTrue();
            file4.Path.Should().Be(@"folder2\file4.txt");
            file4.RepositoryPath.Should().Be(@"folder2\file4.txt");
            file4.State.Should().Be(CommitState.None);
            file4.Target.Should().Be(gitCommitTargets[3]);

            file4.Children.Should().BeEmpty();

            file5.Label.Should().Be("file5.txt");
            file5.Open.Should().BeTrue();
            file5.Path.Should().Be(@"folder2\file5.txt");
            file5.RepositoryPath.Should().Be(@"folder2\file5.txt");
            file5.State.Should().Be(CommitState.None);
            file5.Target.Should().Be(gitCommitTargets[4]);

            file5.Children.Should().BeEmpty();

            newGitStatusEntries = new List<GitStatusEntry> {
                gitObjectFactory.CreateGitStatusEntry(@"folder1\file2.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder1\file3.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder3\file6.txt", GitFileStatus.Modified),
                gitObjectFactory.CreateGitStatusEntry(@"folder3\file7.txt", GitFileStatus.Modified)
            };

            foldedTreeEntries.Add(folder1.RepositoryPath);
            foldedTreeEntries.Add(folder2.RepositoryPath);

            foldedTreeEntries.Count.Should().Be(2);

            treeRoot = TreeBuilder.BuildTreeRoot(newGitStatusEntries, gitStatusEntries, gitCommitTargets,
                foldedTreeEntries);

            gitStatusEntries.Count.Should().Be(4);
            gitCommitTargets.Count.Should().Be(4);
            foldedTreeEntries.Count.Should().Be(1);
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
