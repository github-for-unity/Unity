using System;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseIntegrationTest
    {
        protected NPath TestBasePath { get; private set; }
        protected ILogging Logger { get; private set; }
        protected IEnvironment Environment { get; set; }
        protected IFileSystem FileSystem { get; set; }

        [SetUp]
        public void SetUp()
        {
            Logger = Logging.GetLogger(GetType());
            Environment = new DefaultEnvironment();

            FileSystem = new FileSystem(TestBasePath);

            NPathFileSystemProvider.Current.Should().BeNull("Test should run in isolation");
            NPathFileSystemProvider.Current = FileSystem;

            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            FileSystem.SetCurrentDirectory(TestBasePath);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                //TestBasePath.Delete();
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Error deleting TestBasePath: {0}", TestBasePath.ToString());
            }

            NPathFileSystemProvider.Current = null;
        }

        protected void CreateDirStructure(NPath[] dirs)
        {
            foreach (var file in dirs)
            {
                TestBasePath.Combine(file).CreateFile();
            }
        }
    }
}