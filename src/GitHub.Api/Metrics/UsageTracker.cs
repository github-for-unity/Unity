using System;
using System.Linq;
using System.Text;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    class UsageTracker : IUsageTracker
    {
        private static ILogging Logger { get; } = LogHelper.GetLogger<UsageTracker>();

        private static object _lock = new object();

        private readonly ISettings userSettings;
        private readonly IUsageLoader usageLoader;
        private readonly string userId;
        private readonly string appVersion;
        private readonly string unityVersion;
        private readonly string instanceId;
        private Timer timer;

        public IMetricsService MetricsService { get; set; }

        public UsageTracker(ISettings userSettings,
            IEnvironment environment, string instanceId)
                : this(userSettings, 
                    new UsageLoader(environment.UserCachePath.Combine(Constants.UsageFile)),
                    environment.UnityVersion, instanceId)
        {
        }

        public UsageTracker(ISettings userSettings,
            IUsageLoader usageLoader,
            string unityVersion, string instanceId)
        {
            this.userSettings = userSettings;
            this.usageLoader = usageLoader;
            this.appVersion = ApplicationInfo.Version;
            this.unityVersion = unityVersion;
            this.instanceId = instanceId;

            if (userSettings.Exists(Constants.GuidKey))
            {
                userId = userSettings.Get(Constants.GuidKey);
            }

            if (String.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
                userSettings.Set(Constants.GuidKey, userId);
            }

            Logger.Trace("userId:{0} instanceId:{1}", userId, instanceId);
            if (Enabled)
                RunTimer(3 * 60);
        }

        private void RunTimer(int seconds)
        {
            timer = new System.Threading.Timer(_ =>
            {
                try
                {
                    timer.Dispose();
                    SendUsage();
                }
                catch { }
            }, null, seconds * 1000, Timeout.Infinite);
        }

        private void SendUsage()
        {
            if (MetricsService == null)
            {
                Logger.Warning("Metrics disabled: no service");
                return;
            }

            if (!Enabled)
            {
                Logger.Trace("Metrics disabled");
                return;
            }

            UsageStore usageStore = null;
            lock (_lock)
            {
                usageStore = usageLoader.Load(userId);
            }

            var currentTimeOffset = DateTimeOffset.UtcNow;
            if (usageStore.LastSubmissionDate.Date == currentTimeOffset.Date)
            {
                Logger.Trace("Already sent today");
                return;
            }

            var extractReports = usageStore.Model.SelectReports(currentTimeOffset.Date);
            if (!extractReports.Any())
            {
                Logger.Trace("No items to send");
                return;
            }

            try
            {
                MetricsService.PostUsage(extractReports);
            }
            catch (Exception ex)
            {
                Logger.Warning(@"Error sending usage:""{0}"" Message:""{1}""", ex.GetType(), ex.GetExceptionMessageShort());
                return;
            }

            // if we're here, success!
            lock(_lock)
            {
                usageStore = usageLoader.Load(userId);
                usageStore.LastSubmissionDate = currentTimeOffset;
                usageStore.Model.RemoveReports(currentTimeOffset.Date);
                usageLoader.Save(usageStore);
            }
        }

        public void IncrementNumberOfStartups()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .NumberOfStartups++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementProjectsInitialized()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .ProjectsInitialized++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementChangesViewButtonCommit()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .ChangesViewButtonCommit++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementHistoryViewToolbarFetch()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .HistoryViewToolbarFetch++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementHistoryViewToolbarPush()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .HistoryViewToolbarPush++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementHistoryViewToolbarPull()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .HistoryViewToolbarPull++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementBranchesViewButtonCreateBranch()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .BranchesViewButtonCreateBranch++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementBranchesViewButtonDeleteBranch()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .BranchesViewButtonDeleteBranch++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementBranchesViewButtonCheckoutLocalBranch()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .BranchesViewButtonCheckoutLocalBranch++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementBranchesViewButtonCheckoutRemoteBranch()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .BranchesViewButtonCheckoutRemoteBranch++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementSettingsViewButtonLfsUnlock()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .SettingsViewButtonLfsUnlock++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementAuthenticationViewButtonAuthentication()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .AuthenticationViewButtonAuthentication++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementUnityProjectViewContextLfsLock()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .UnityProjectViewContextLfsLock++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementUnityProjectViewContextLfsUnlock()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                    .UnityProjectViewContextLfsUnlock++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementPublishViewButtonPublish()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                     .PublishViewButtonPublish++;
                usageLoader.Save(usage);
            }
        }

        public void IncrementApplicationMenuMenuItemCommandLine()
        {
            lock (_lock)
            {
                var usage = usageLoader.Load(userId);
                usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                     .ApplicationMenuMenuItemCommandLine++;
                usageLoader.Save(usage);
            }
        }

        public void UpdateRepoSize(int kilobytes)
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId).GitRepoSize = kilobytes;
            usageLoader.Save(usage);
        }

        public void UpdateLfsDiskUsage(int kilobytes)
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId).LfsDiskUsage = kilobytes;
            usageLoader.Save(usage);
        }

        public bool Enabled
        {
            get
            {
                return userSettings.Get(Constants.MetricsKey, true);
            }
            set
            {
                if (value == Enabled)
                    return;
                userSettings.Set(Constants.MetricsKey, value);
                if (value)
                {
                    RunTimer(5);
                }
                else
                {
                    timer.Dispose();
                    timer = null;
                }
            }
        }
    }

    interface IUsageLoader
    {
        UsageStore Load(string userId);
        void Save(UsageStore store);
    }

    class UsageLoader : IUsageLoader
    {
        private readonly NPath path;

        public UsageLoader(NPath path)
        {
            this.path = path;
        }

        public UsageStore Load(string userId)
        {
            UsageStore result = null;
            string json = null;
            if (path.FileExists())
            {
                try
                {
                    json = path.ReadAllText(Encoding.UTF8);
                    result = json?.FromJson<UsageStore>(lowerCase: true);
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Warning(ex, "Error Loading Usage: {0}; Deleting File", path);
                    try
                    {
                        path.DeleteIfExists();
                    }
                    catch { }
                }
            }

            if (result == null)
                result = new UsageStore();

            if (String.IsNullOrEmpty(result.Model.Guid))
                result.Model.Guid = userId;

            return result;
        }

        public void Save(UsageStore store)
        {
            try
            {
                var json = store.ToJson(lowerCase: true);
                path.WriteAllText(json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, "SaveUsage Error: \"{0}\"", path);
            }
        }
    }
}
