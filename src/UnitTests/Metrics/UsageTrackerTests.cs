using GitHub.Unity;
using NCrunch.Framework;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture, Isolated]
    class UsageTrackerTests: BaseTest
    {
        public IFileSystem FileSystem1 { get; private set; }

        protected override void OnTestFixtureSetup()
        {
            base.OnTestFixtureSetup();
            FileSystem1 = Factory.CreateFileSystem(new CreateFileSystemOptions());
            NPathFileSystemProvider.Current = FileSystem1;
        }

        protected override void OnTestFixtureTearDown()
        {
            base.OnTestFixtureTearDown();
            NPathFileSystemProvider.Current = null;
        }

        [Test]
        public void Blah()
        {
            var userTrackingId = System.Guid.NewGuid().ToString();
            IUsageTracker usageTracker = new UsageTracker(@"c:\Setting.txt", userTrackingId);
            //usageTracker.IncrementLaunchCount().Wait();
        }
    }
}
