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
        private readonly ILogging logger = Logging.GetLogger<UsageTracker>();
        private readonly NPath storePath;
        private readonly string userTrackingId;

        private IMetricsService client;
        private bool firstRun = true;
        private Timer timer;

        public UsageTracker(NPath storePath, string userTrackingId)
        {
            this.userTrackingId = userTrackingId;
            logger.Trace("Tracking Id:{0}", userTrackingId);

            this.storePath = storePath;

            RunTimer();
        }

        private void Initialize()
        {
#if HAS_METRICS_SERVICE
            if (client == null)
            {
                client = new MetricsService($"GitHub4Unity{AssemblyVersionInformation.Version}");
            }
#endif
        }

        private UsageStore LoadUsage()
        {
            Initialize();

            string json = null;
            if (storePath.FileExists())
            {
                logger.Trace("LoadUsage: \"{0}\"", storePath);

                try
                {
                    json = storePath.ReadAllText(Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "LoadUsage Error {0}", ex.Message);
                }
            }

            UsageStore result;
            try
            {
                if (json != null)
                {
                    result = SimpleJson.DeserializeObject<UsageStore>(json);
                    if (result.Model == null)
                    {
                        logger.Warning("Model is Null");
                    }

                    if (result.Model.Reports == null)
                    {
                        logger.Warning("Reports is Null");
                    }
                }
                else
                {
                    result = new UsageStore();
                }
            }
            catch (Exception ex)
            {
                result = new UsageStore();

                logger.Warning(ex, "Error Loading Usage: {0}; Deleting File", storePath);

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
            logger.Trace("SaveUsage: \"{0}\"", pathString);

            try
            {
                var json = SimpleJson.SerializeObject(store);
                storePath.WriteAllText(json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "SaveUsage Error: \"{0}\"", pathString);
            }
        }

        private void RunTimer()
        {
            logger.Trace("Scheduling timer for 3 minutes from now");
            // The timer first ticks after 3 minutes to allow things to settle down after startup.
            // This will be changed to 8 hours after the first tick by the TimerTick method.
            timer = new Timer(TimeSpan.FromMinutes(3).TotalMilliseconds);
            timer.Elapsed += TimerTick;
            timer.Start();
        }
            
        private void TimerTick(object sender, ElapsedEventArgs e)
        {
            TimerTick().Catch((Action<Task, Exception>)((task, exception) => {
                logger.Error(exception, "TimerTicker Error: {0}", exception.Message);
            })).Forget();
        }

        private async Task TimerTick()
        {
            logger.Trace("TimerTick");

            Initialize();
            var usageStore = LoadUsage();

            if (firstRun)
            {
                timer.Interval = TimeSpan.FromHours(8).TotalMilliseconds;
                firstRun = false;
                Logging.Trace("Scheduling timer for 8 hours from now");

                if (!Enabled)
                {
                    logger.Warning("Tracking Disabled");
                    return;
                }
            }

            if (client == null)
            {
                logger.Warning("MetricsClient is null; stopping timer");
                if (timer != null)
                {
                    timer.Enabled = false;
                    timer = null;
                }
                return;
            }

            if (!Enabled)
            {
                logger.Warning("Tracking Disabled");
                return;
            }

            if (usageStore.LastUpdated.Date != DateTimeOffset.UtcNow.Date)
            {
                await SendUsage(usageStore);
            }
        }

        private async Task SendUsage(UsageStore usage)
        {
            logger.Trace("Sending Usage");

            var currentTimeOffset = DateTimeOffset.UtcNow;
            var beforeDate = currentTimeOffset.Date;

            var extractReports = usage.Model.SelectReports(beforeDate);
            if (!extractReports.Any())
            {
                logger.Trace("No items to send");
            }
            else
            {
                var success = false;
                try
                {
                    await client.PostUsage(userTrackingId, extractReports);
                    success = true;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Error Sending Usage");
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

            logger.Trace("IncrementLaunchCount Date:{0} NumberOfStartups:{1}", usage.Date, usage.NumberOfStartups);

            SaveUsage(usageStore);
        }

        public bool Enabled { get; set; }
    }
}
