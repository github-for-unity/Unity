﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Globalization;
using System.Threading;
using Timer = System.Threading.Timer;

namespace GitHub.Unity
{
    class UsageTracker : IUsageTracker
    {
        private static ILogging Logger { get; } = Logging.GetLogger<UsageTracker>();
        private static IMetricsService metricsService;

        private readonly NPath storePath;
        private readonly ISettings userSettings;
        private readonly string guid;
        private readonly string unityVersion;
        private Timer timer;

        public UsageTracker(ISettings userSettings, NPath storePath, string guid, string unityVersion)
        {
            this.userSettings = userSettings;
            this.guid = guid;
            this.storePath = storePath;
            this.unityVersion = unityVersion;

            Logger.Trace("guid:{0}", guid);
            if (Enabled)
                RunTimer(3*60);
        }

        private UsageStore LoadUsage()
        {
            UsageStore result = null;
            string json = null;
            if (storePath.FileExists())
            {
                Logger.Trace("LoadUsage: \"{0}\"", storePath);

                try
                {
                    json = storePath.ReadAllText(Encoding.UTF8);
                    if (json != null)
                    {
                        result = SimpleJson.DeserializeObject<UsageStore>(json);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Error Loading Usage: {0}; Deleting File", storePath);

                    try
                    {
                        storePath.DeleteIfExists();
                    }
                    catch {}
                }
            }

            if (result == null)
                result = new UsageStore();

            if (String.IsNullOrEmpty(result.Model.Guid))
                result.Model.Guid = guid;

            return result;
        }

        private void SaveUsage(UsageStore store)
        {
            if (!Enabled)
            {
                return;
            }

            var pathString = storePath.ToString();
            Logger.Trace("SaveUsage: \"{0}\"", pathString);

            try
            {
                var json = SimpleJson.SerializeObject(store);
                storePath.WriteAllText(json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "SaveUsage Error: \"{0}\"", pathString);
            }
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
            Logger.Trace("SendUsage");

            var usage = LoadUsage();

            if (metricsService == null)
            {
                Logger.Warning("No service, not sending usage");
                return;
            }

            if (usage.LastUpdated.Date != DateTimeOffset.UtcNow.Date)
            {
                Logger.Trace("Sending Usage");

                var currentTimeOffset = DateTimeOffset.UtcNow;
                var beforeDate = currentTimeOffset.Date;

                var success = false;
                var extractReports = usage.Model.SelectReports(beforeDate);
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
                    usage.Model.RemoveReports(beforeDate);
                    usage.LastUpdated = currentTimeOffset;
                    SaveUsage(usage);
                }
            }
        }

        public void IncrementLaunchCount()
        {
            var usageStore = LoadUsage();

            var usage = usageStore.Model.GetCurrentUsage();
            usage.NumberOfStartups++;
            usage.UnityVersion = unityVersion;
            usage.Lang = CultureInfo.InstalledUICulture.IetfLanguageTag;
            usage.AppVersion = AppConfiguration.AssemblyName.Version.ToString();

            Logger.Trace("IncrementLaunchCount Date:{0} NumberOfStartups:{1}", usage.Date, usage.NumberOfStartups);

            SaveUsage(usageStore);
        }

        public static void SetMetricsService(IMetricsService instance)
        {
            Logger.Trace("SetMetricsService instance:{0}", instance?.ToString() ?? "Null");
            metricsService = instance;
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
}
