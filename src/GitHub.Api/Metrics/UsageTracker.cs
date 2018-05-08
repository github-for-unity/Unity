using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Globalization;
using System.Threading;
using Timer = System.Threading.Timer;
using GitHub.Logging;

namespace GitHub.Unity
{
    class UsageTracker : IUsageTracker
    {
        private static ILogging Logger { get; } = LogHelper.GetLogger<UsageTracker>();

        private readonly ISettings userSettings;
        private readonly IUsageLoader usageLoader;
        private readonly IMetricsService metricsService;
        private readonly string userId;
        private readonly string appVersion;
        private readonly string unityVersion;
        private readonly string instanceId;
        private Timer timer;

        public UsageTracker(IMetricsService metricsService, ISettings userSettings,
            IEnvironment environment, string userId, string unityVersion, string instanceId)
                : this(metricsService, userSettings, 
                      new UsageLoader(environment.UserCachePath.Combine(Constants.UsageFile)),
                      userId, unityVersion, instanceId)
        {
        }

        public UsageTracker(IMetricsService metricsService, ISettings userSettings,
            IUsageLoader usageLoader,
            string userId, string unityVersion, string instanceId)
        {
            this.userSettings = userSettings;
            this.usageLoader = usageLoader;
            this.metricsService = metricsService;
            this.userId = userId;
            this.appVersion = ApplicationConfiguration.AssemblyName.Version.ToString();
            this.unityVersion = unityVersion;
            this.instanceId = instanceId;

            Logger.Trace("userId:{0} instanceId:{1}", userId, instanceId);
            if (Enabled)
                RunTimer(3*60);
        }

        private void RunTimer(int seconds)
        {
            Logger.Trace($"Scheduling timer for {seconds} seconds from now");
            timer = new Timer(async _ =>
            {
                try
                {
                    timer.Dispose();
                    await SendUsage();
                }
                catch {}
            }, null, seconds * 1000, Timeout.Infinite);
        }

        private async Task SendUsage()
        {
            var usageStore = usageLoader.Load(userId);

            if (metricsService == null)
            {
                Logger.Warning("No service, not sending usage");
                return;
            }

            if (usageStore.LastUpdated.Date != DateTimeOffset.UtcNow.Date)
            {
                Logger.Trace("Sending Usage");

                var currentTimeOffset = DateTimeOffset.UtcNow;
                var beforeDate = currentTimeOffset.Date;

                var success = false;
                var extractReports = usageStore.Model.SelectReports(beforeDate);
                if (!extractReports.Any())
                {
                    Logger.Trace("No items to send");
                }
                else
                {
                    if (!Enabled)
                    {
                        Logger.Trace("Metrics disabled");
                        return;
                    }

                    try
                    {
                        await metricsService.PostUsage(extractReports);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(@"Error Sending Usage Exception Type:""{0}"" Message:""{1}""", ex.GetType().ToString(), ex.Message);
                    }
                }

                if (success)
                {
                    usageStore.Model.RemoveReports(beforeDate);
                    usageStore.LastUpdated = currentTimeOffset;
                    usageLoader.Save(usageStore);
                }
            }
        }

        public void IncrementNumberOfStartups()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .NumberOfStartups++;
            usageLoader.Save(usage);
        }

        public void IncrementChangesViewButtonCommit()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .ChangesViewButtonCommit++;
            usageLoader.Save(usage);
        }

        public void IncrementHistoryViewToolbarButtonFetch()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .HistoryViewToolbarButtonFetch++;
            usageLoader.Save(usage);
        }

        public void IncrementHistoryViewToolbarButtonPush()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .HistoryViewToolbarButtonPush++;
            usageLoader.Save(usage);
        }

        public void IncrementProjectsInitialized()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .ProjectsInitialized++;
            usageLoader.Save(usage);
        }

        public void IncrementBranchesViewButtonCreateBranch()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .BranchesViewButtonCreateBranch++;
            usageLoader.Save(usage);
        }

        public void IncrementBranchesViewButtonDeleteBranch()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .BranchesViewButtonDeleteBranch++;
            usageLoader.Save(usage);
        }

        public void IncrementBranchesViewButtonCheckoutLocalBranch()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .BranchesViewButtonCheckoutLocalBranch++;
            usageLoader.Save(usage);
        }

        public void IncrementBranchesViewButtonCheckoutRemoteBranch()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .BranchesViewButtonCheckoutRemoteBranch++;
            usageLoader.Save(usage);
        }

        public void IncrementSettingsViewUnlockButtonLfsUnlock()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .SettingsViewUnlockButtonLfsUnlock++;
            usageLoader.Save(usage);
        }

        public void IncrementAssetExplorerContextMenuLfsLock()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .AssetExplorerContextMenuLfsLock++;
            usageLoader.Save(usage);
        }

        public void IncrementAssetExplorerContextMenuLfsUnlock()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .AssetExplorerContextMenuLfsUnlock++;
            usageLoader.Save(usage);
        }

        public void IncrementHistoryViewToolbarButtonPull()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .HistoryViewToolbarButtonPull++;
            usageLoader.Save(usage);
        }

        public void IncrementAuthenticationViewButtonAuthentication()
        {
            var usage = usageLoader.Load(userId);
            usage.GetCurrentMeasures(appVersion, unityVersion, instanceId)
                .AuthenticationViewButtonAuthentication++;
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
                LogHelper.Instance.Trace("LoadUsage: \"{0}\"", path);

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
                    catch {}
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
