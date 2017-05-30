using System;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;
using NCrunch.Framework;
using System.Threading;

namespace IntegrationTests
{
    [Isolated]
    class BaseIntegrationTest
    {
        private IFileSystem fs;
        private IEnvironment env;

        protected NPath TestBasePath { get; private set; }
        protected ILogging Logger { get; private set; }
        public IEnvironment Environment
        {
            get
            {
                return env;
            }
            set
            {
                env = value;
                if (fs != null)
                {
                    env.FileSystem = fs;
                }
            }
        }
        protected IFileSystem FileSystem
        {
            get
            {
                return fs;
            }
            set
            {
                fs = value;
                if (env != null)
                    env.FileSystem = value;
            }
        }
        protected TestUtils.SubstituteFactory Factory { get; set; }
        protected static NPath SolutionDirectory => TestContext.CurrentContext.TestDirectory.ToNPath();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Logger = Logging.GetLogger(GetType());
            Factory = new TestUtils.SubstituteFactory();
        }

        [SetUp]
        public void SetUp()
        {
            OnSetup();
        }

        protected virtual void OnSetup()
        {
            FileSystem = new FileSystem();
            NPath.FileSystem = FileSystem;
            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            FileSystem.SetCurrentDirectory(TestBasePath);
        }

        [TearDown]
        public void TearDown()
        {
            OnTearDown();
        }

        protected virtual void OnTearDown()
        {
            TaskManager.Instance?.Stop();
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

            FileSystem = null;
            NPath.FileSystem = null;
        }
    }
}