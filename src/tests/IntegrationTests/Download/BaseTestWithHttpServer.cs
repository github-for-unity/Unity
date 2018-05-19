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
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            var filesToServePath = SolutionDirectory.Combine("files");

            ApplicationConfiguration.WebTimeout = 50000;

            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"), 50000);
            Task.Factory.StartNew(server.Start);
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }

    class BaseGitTestWithHttpServer : BaseGitEnvironmentTest
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            var filesToServePath = SolutionDirectory.Combine("files");

            ApplicationConfiguration.WebTimeout = 50000;

            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"), 50000);
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