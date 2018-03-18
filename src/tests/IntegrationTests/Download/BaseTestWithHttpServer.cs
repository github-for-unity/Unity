using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitHub.Logging;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseTestWithHttpServer : BaseIntegrationTest
    {
        protected const int Timeout = 30000;
        protected TestWebServer.HttpServer server;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            var filesToServePath = SolutionDirectory.Combine("files");

            ApplicationConfiguration.WebTimeout = 50000;

            AssemblyResources.ToFile(ResourceType.Platform, "git.zip", filesToServePath, new DefaultEnvironment());
            AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", filesToServePath, new DefaultEnvironment());
            AssemblyResources.ToFile(ResourceType.Platform, "git.zip.md5", filesToServePath, new DefaultEnvironment());
            AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip.md5", filesToServePath, new DefaultEnvironment());

            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"));
            Task.Factory.StartNew(server.Start);
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }

    }
}