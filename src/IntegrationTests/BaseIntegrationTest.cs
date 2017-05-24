using System;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;
using NCrunch.Framework;

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
        protected static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;

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
            FileSystem = new FileSystem(TestBasePath);

            //NPathFileSystemProvider.Current.Should().BeNull("Test should run in isolation");
            NPathFileSystemProvider.Current = FileSystem;

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
            try
            {
                Logger.Debug("Deleting TestBasePath: {0}", TestBasePath.ToString());
                TestBasePath.Delete();
            }
            catch (Exception)
            {
                Logger.Warning("Error deleting TestBasePath: {0}", TestBasePath.ToString());
            }

            FileSystem = null;
            NPathFileSystemProvider.Current = null;
        }
    }
}