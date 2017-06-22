using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Rackspace.Threading;

namespace GitHub.Unity
{
    class UsageTracker : IUsageTracker
    {
        private static ILogging Logger { get; } = Logging.GetLogger<UsageTracker>();
        private static IMetricsService metricsService;

        private readonly NPath storePath;
        private readonly string userTrackingId;

        private bool firstRun = true;
        private Timer timer;

        public UsageTracker(NPath storePath, string userTrackingId)
        {
            this.userTrackingId = userTrackingId;
            Logger.Trace("Tracking Id:{0}", userTrackingId);

            this.storePath = storePath;

            RunTimer();
        }

        private UsageStore LoadUsage()
        {
            string json = null;
            if (storePath.FileExists())
            {
                Logger.Trace("LoadUsage: \"{0}\"", storePath);

                try
                {
                    json = storePath.ReadAllText(Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "LoadUsage Error {0}", ex.Message);
                }
            }

            UsageStore result;
            try
            {
                if (json != null)
                {
                    result = SimpleJson.DeserializeObject<UsageStore>(json);
                }
                else
                {
                    result = new UsageStore();
                }
            }
            catch (Exception ex)
            {
                result = new UsageStore();

                Logger.Warning(ex, "Error Loading Usage: {0}; Deleting File", storePath);

                try
                {
                    storePath.DeleteIfExists();
                }
                catch{}
            }

            //TODO: Figure out these values
            //result.Model.Lang = CultureInfo.InstalledUICulture.IetfLanguageTag;
            //result.Model.AppVersion = AssemblyVersionInformation.Version;

            //TODO: Get Unity Version
            //result.Model.UnityVersion
            //result.Model.VSVersion = vsservices.VSVersion;

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
                    Logger.Warning("Tracking Disabled");
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
                Logger.Warning("Tracking Disabled");
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

            var extractReports = usage.Model.SelectReports(beforeDate);
            if (!extractReports.Any())
            {
                Logger.Trace("No items to send");
            }
            else
            {
                var success = false;
                try
                {
                    await metricsService.PostUsage(userTrackingId, extractReports);
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Error Sending Usage");
                }

                if (success)
                {
                    usage.Model.RemoveReports(beforeDate);
                }
            }

            usage.LastUpdated = currentTimeOffset;
            SaveUsage(usage);
        }

        public void IncrementLaunchCount()
        {
            var usageStore = LoadUsage();

            var usage = usageStore.Model.GetCurrentUsage();
            usage.NumberOfStartups++;

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
