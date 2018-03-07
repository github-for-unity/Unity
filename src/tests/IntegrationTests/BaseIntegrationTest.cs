using System;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;
using NCrunch.Framework;
using System.Threading;
using NSubstitute;
using GitHub.Logging;

namespace IntegrationTests
{
    [Isolated]
    class BaseIntegrationTest
    {
        protected NPath TestBasePath { get; private set; }
        protected ILogging Logger { get; private set; }
        public IEnvironment Environment { get; set; }
        public IRepository Repository => Environment.Repository;
        public ICacheContainer CacheContainer { get;  set; }

        protected TestUtils.SubstituteFactory Factory { get; set; }
        protected static NPath SolutionDirectory => TestContext.CurrentContext.TestDirectory.ToNPath();

        protected void InitializeEnvironment(NPath repoPath,
            NPath? environmentPath = null,
            bool enableEnvironmentTrace = false,
            bool initializeRepository = true
            )
        {
            CacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(CacheContainer,
                repoPath,
                SolutionDirectory,
                environmentPath,
                enableEnvironmentTrace,
                initializeRepository);
        }

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            Logger = LogHelper.GetLogger(GetType());
            Factory = new TestUtils.SubstituteFactory();
            GitHub.Unity.Guard.InUnitTestRunner = true;
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
        }

        [SetUp]
        public virtual void OnSetup()
        {
            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            NPath.FileSystem.SetCurrentDirectory(TestBasePath);
        }

        [TearDown]
        public virtual void OnTearDown()
        {
            TaskManager.Instance?.Dispose();
            Logger.Debug("Deleting TestBasePath: {0}", TestBasePath.ToString());
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    TestBasePath.Delete();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }
            if (TestBasePath.Exists())
                Logger.Warning("Error deleting TestBasePath: {0}", TestBasePath.ToString());

            NPath.FileSystem = null;
        }
    }
}