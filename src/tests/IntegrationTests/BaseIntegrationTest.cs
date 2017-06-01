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
        protected NPath TestBasePath { get; private set; }
        protected ILogging Logger { get; private set; }
        public IEnvironment Environment { get; set; }

        protected TestUtils.SubstituteFactory Factory { get; set; }
        protected static NPath SolutionDirectory => TestContext.CurrentContext.TestDirectory.ToNPath();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Logger = Logging.GetLogger(GetType());
            Factory = new TestUtils.SubstituteFactory();
            System.Environment.SetEnvironmentVariable("GHFU", "TESTING");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            System.Environment.SetEnvironmentVariable("GHFU", null);
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

            NPath.FileSystem = null;
        }
    }
}