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
    [TestFixture, Isolated, Ignore]
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

            //TODO: Mock CacheContainer
            ICacheContainer cacheContainer = null;
            return new Repository(@"C:\Repo".ToNPath(), cacheContainer);
        }

        private RepositoryEvents repositoryEvents;
        private TimeSpan repositoryEventsTimeout;

        [SetUp]
        public void OnSetup()
        {
            repositoryEvents = new RepositoryEvents();
            repositoryEventsTimeout = TimeSpan.FromSeconds(0.5);
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

            var masterOriginBranch = new ConfigBranch { Name = "master", Remote = origin };

            var branches = new[] {
                masterOriginBranch,
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

            repositoryManager.OnLocalBranchListUpdated += Raise.Event<Action<IDictionary<string, ConfigBranch>>>(branchDictionary);

            repositoryManager.OnRemoteBranchListUpdated += Raise.Event<Action<IDictionary<string, ConfigRemote>, IDictionary<string, IDictionary<string, ConfigBranch>>>>(remoteDictionary, remoteBranchDictionary);

            repositoryManager.OnCurrentBranchUpdated += Raise.Event<Action<ConfigBranch?>>(masterOriginBranch);
            repositoryManager.OnCurrentRemoteUpdated += Raise.Event<Action<ConfigRemote?>>(origin);
        }
    }
}
