using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace IntegrationTests
{
    [TestFixture]
    class MetricsTests : BaseIntegrationTest
    {
        [TestCase(nameof(Measures.UnityProjectViewContextLfsLock))]
        [TestCase(nameof(Measures.UnityProjectViewContextLfsUnlock))]
        [TestCase(nameof(Measures.AuthenticationViewButtonAuthentication))]
        [TestCase(nameof(Measures.BranchesViewButtonCheckoutLocalBranch))]
        [TestCase(nameof(Measures.BranchesViewButtonCheckoutRemoteBranch))]
        [TestCase(nameof(Measures.BranchesViewButtonCreateBranch))]
        [TestCase(nameof(Measures.BranchesViewButtonDeleteBranch))]
        [TestCase(nameof(Measures.ChangesViewButtonCommit))]
        [TestCase(nameof(Measures.HistoryViewToolbarFetch))]
        [TestCase(nameof(Measures.HistoryViewToolbarPull))]
        [TestCase(nameof(Measures.HistoryViewToolbarPush))]
        [TestCase(nameof(Measures.NumberOfStartups))]
        [TestCase(nameof(Measures.ProjectsInitialized))]
        [TestCase(nameof(Measures.SettingsViewButtonLfsUnlock))]
        public void IncrementMetricsWorks(string measureName)
        {
            var userId = Guid.NewGuid().ToString();
            var appVersion = ApplicationInfo.Version;
            var unityVersion = "2017.3f1";
            var instanceId = Guid.NewGuid().ToString();
            var usageLoader = Substitute.For<IUsageLoader>();
            var usageStore = new UsageStore();
            var settings = Substitute.For<ISettings>();
            settings.Exists(Arg.Is<string>(Constants.GuidKey)).Returns(true);
            settings.Get(Arg.Is<string>(Constants.GuidKey)).Returns(userId);

            usageStore.Model.Guid = userId;
            usageLoader.Load(Arg.Is<string>(userId)).Returns(usageStore);

            var usageTracker = new UsageTracker(settings, usageLoader, unityVersion, instanceId);

            var currentUsage = usageStore.GetCurrentMeasures(appVersion, unityVersion, instanceId);
            var prop = currentUsage.GetType().GetProperty(measureName);
            Assert.AreEqual(0, prop.GetValue(currentUsage, null));
            var meth = usageTracker.GetType().GetMethod("Increment" + measureName);
            meth.Invoke(usageTracker, null);
            currentUsage = usageStore.GetCurrentMeasures(appVersion, unityVersion, instanceId);
            Assert.AreEqual(1, prop.GetValue(currentUsage, null));
        }

        [Test]
        public void LoadingWorks()
        {
            InitializeEnvironment(TestBasePath, false, false);
            var userId = Guid.NewGuid().ToString();
            var appVersion = ApplicationInfo.Version;
            var unityVersion = "2017.3f1";
            var instanceId = Guid.NewGuid().ToString();
            var usageStore = new UsageStore();
            usageStore.Model.Guid = userId;
            var storePath = Environment.UserCachePath.Combine(Constants.UsageFile);
            var usageLoader = new UsageLoader(storePath);

            var settings = Substitute.For<ISettings>();
            settings.Exists(Arg.Is<string>(Constants.GuidKey)).Returns(true);
            settings.Get(Arg.Is<string>(Constants.GuidKey)).Returns(userId);
            var usageTracker = new UsageTracker(settings, usageLoader, unityVersion, instanceId);

            usageTracker.IncrementNumberOfStartups();
            usageTracker.IncrementNumberOfStartups();

            Assert.IsTrue(storePath.FileExists());
            var json = storePath.ReadAllText(Encoding.UTF8);
            var savedStore = json.FromJson<UsageStore>(lowerCase: true);
            Assert.AreEqual(2, savedStore.GetCurrentMeasures(appVersion, unityVersion, instanceId).NumberOfStartups);
        }
    }
}
