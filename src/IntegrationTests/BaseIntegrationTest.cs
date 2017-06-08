using System;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;
using NCrunch.Framework;
using TestUtils;

namespace IntegrationTests
{
    [Isolated]
    abstract class BaseIntegrationTest : BaseTest
    {
        private IFileSystem fs;
        private IEnvironment env;

        protected NPath TestBasePath { get; private set; }
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
        protected static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;

        protected override void OnSetup()
        {
            base.OnSetup();

            FileSystem = new FileSystem(TestBasePath);

            //NPathFileSystemProvider.Current.Should().BeNull("Test should run in isolation");
            NPathFileSystemProvider.Current = FileSystem;

            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            FileSystem.SetCurrentDirectory(TestBasePath);
        }

        protected override void OnTearDown()
        {
            base.OnTearDown();

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