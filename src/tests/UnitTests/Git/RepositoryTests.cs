using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using TestUtils.Events;

namespace UnitTests
{
    [TestFixture, Isolated]
    public class RepositoryTests
    {
        private static readonly SubstituteFactory SubstituteFactory = new SubstituteFactory();

        private static Repository LoadRepository()
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(
                new CreateFileSystemOptions
                {

                });

            NPath.FileSystem = fileSystem;

            return new Repository("TestRepo", @"C:\Repo".ToNPath());
        }

        private RepositoryEvents repositoryEvents;

        [SetUp]
        public void OnSetup()
        {
            repositoryEvents = new RepositoryEvents();
        }

        [Test]
        public void Repository()
        {
            var repository = LoadRepository();
            var repositoryManager = Substitute.For<IRepositoryManager>();

            var repositoryListener = Substitute.For<IRepositoryListener>();
            repositoryListener.AttachListener(repository, repositoryEvents);

            var origin = new ConfigRemote
            {
                Name = "origin",
                Url = "https://github.com/someUser/someRepo.git"
            };

            var remotes = new[] { origin };

            var remoteDictionary = remotes.ToDictionary(remote => remote.Name);

            var branches = new[] {
                new ConfigBranch { Name = "master", Remote = origin },
                new ConfigBranch { Name = "features/feature-1", Remote = origin }
            };

            var branchDictionary = branches.ToDictionary(branch => branch.Name);

            var remoteBranches = new[] {
                new ConfigBranch { Name = "master", Remote = origin },
                new ConfigBranch { Name = "features/feature-1", Remote = origin },
                new ConfigBranch { Name = "features/feature-2", Remote = origin }
            };

            var remoteBranchDictionary = remoteBranches
                .GroupBy(branch => branch.Remote.Value.Name)
                .ToDictionary(grouping => grouping.Key,
                    grouping => grouping.ToDictionary(branch => branch.Name));

            repository.Initialize(repositoryManager);

            string expectedBranch = null;
            repository.OnCurrentBranchChanged += branch => {
                expectedBranch = branch;
            };

            string expectedRemote = null;
            repository.OnCurrentRemoteChanged += remote => {
                expectedRemote = remote;
            };

            repositoryManager.OnLocalBranchListUpdated += Raise.Event<Action<Dictionary<string, ConfigBranch>>>(branchDictionary);

            repositoryEvents.OnLocalBranchListChanged.WaitOne().Should().BeTrue();

            repositoryManager.OnRemoteBranchListUpdated += Raise.Event<Action<Dictionary<string, Dictionary<string, ConfigBranch>>>>(remoteBranchDictionary);

            repositoryEvents.OnRemoteBranchListChanged.WaitOne().Should().BeTrue();

            repositoryManager.OnHeadUpdated += Raise.Event<Action<string>>("ref:refs/heads/master");

            repositoryEvents.OnCurrentBranchChanged.WaitOne().Should().BeTrue();
            repositoryEvents.OnCurrentRemoteChanged.WaitOne().Should().BeTrue();

            expectedBranch.Should().Be("master");
            expectedRemote.Should().Be("origin");
        }
    }
}
