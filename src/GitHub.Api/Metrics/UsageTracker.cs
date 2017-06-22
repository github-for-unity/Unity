using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Rackspace.Threading;
using System.Globalization;

namespace GitHub.Unity
{
    class UsageTracker : IUsageTracker
    {
        private static ILogging Logger { get; } = Logging.GetLogger<UsageTracker>();
        private static IMetricsService metricsService;

        private readonly NPath storePath;
        private readonly string guid;
        private readonly string unityVersion;

        private bool firstRun = true;
        private Timer timer;

        public UsageTracker(NPath storePath, string guid, string unityVersion)
        {
            this.guid = guid;
            this.storePath = storePath;
            this.unityVersion = unityVersion;

            Logger.Trace("guid:{0}", guid);
            RunTimer();
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

        private void RunTimer()
        {
            Logger.Trace("Scheduling timer for 3 minutes from now");
            // The timer first ticks after 3 minutes to allow things to settle down after startup.
            // This will be changed to 8 hours after the first tick by the TimerTick method.
            timer = new Timer(TimeSpan.FromMinutes(3).TotalMilliseconds);
            timer.Elapsed += TimerTick;
            timer.Start();
        }
            
        private void TimerTick(object sender, ElapsedEventArgs e)
        {
            TimerTick().Catch((Action<Task, Exception>)((task, exception) => {
                Logger.Error(exception, "TimerTicker Error: {0}", exception.Message);
            })).Forget();
        }

        private async Task TimerTick()
        {
            Logger.Trace("TimerTick");

            var usageStore = LoadUsage();

            if (firstRun)
            {
                timer.Interval = TimeSpan.FromHours(8).TotalMilliseconds;
                firstRun = false;
                Logger.Trace("Scheduling timer for 8 hours from now");

                if (!Enabled)
                {
                    Logger.Warning("Metrics Disabled");
                    return;
                }
            }

            if (metricsService == null)
            {
                Logger.Warning("MetricsClient is null; stopping timer");
                if (timer != null)
                {
                    timer.Enabled = false;
                    timer = null;
                }
                return;
            }

            if (!Enabled)
            {
                Logger.Warning("Metrics Disabled");
                return;
            }

            if (usageStore.LastUpdated.Date != DateTimeOffset.UtcNow.Date)
            {
                await SendUsage(usageStore);
            }
        }

        private async Task SendUsage(UsageStore usage)
        {
            Logger.Trace("Sending Usage");

            var currentTimeOffset = DateTimeOffset.UtcNow;
            var beforeDate = currentTimeOffset.Date;

            var success = false;
            var extractReports = usage.Model.SelectReports(beforeDate);
            if (!extractReports.Any())
            {
                Logger.Trace("No items to send");
                success = true;
            }
            else
            {
                try
                {
                    await metricsService.PostUsage(extractReports);
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Error Sending Usage");
                }

            }

            if (success)
            {
                usage.Model.RemoveReports(beforeDate);
                usage.LastUpdated = currentTimeOffset;
                SaveUsage(usage);
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
            Logger.Trace("SetMetricsService instance:{1}", instance?.ToString() ?? "Null");
            metricsService = instance;
        }

        public bool Enabled { get; set; }
    }
}
