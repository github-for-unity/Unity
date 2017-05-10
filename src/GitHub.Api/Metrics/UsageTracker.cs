using System;
using System.Globalization;
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

        private IMetricsService client;
        private bool firstRun = true;
        private Timer timer;

        public UsageTracker(NPath storePath)
        {
            logger.Trace("Created");

            this.storePath = storePath;

            RunTimer();
        }

        private static void ClearCounters(UsageModel model)
        {
            model.Clear();
        }

        private async Task Initialize()
        {
            // The services needed by the usage tracker are loaded when they are first needed to
            // improve the startup time of the extension.
            await ThreadingHelper.SwitchToMainThreadAsync();
            
#if HAS_METRICS_SERVICE
            if (client == null)
            {
                client = new MetricsService($"GitHub4Unity{AssemblyVersionInformation.Version}");
            }
#endif
        }

        private async Task<UsageStore> LoadUsage()
        {
            await Initialize();

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
                    //storePath.DeleteIfExists();
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
            //TODO: Check User Settings

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

            //TODO: Check User Settings

            await Initialize();
            var usage = await LoadUsage();

            if (firstRun)
            {
                timer.Interval = TimeSpan.FromHours(8).TotalMilliseconds;
                firstRun = false;

                logger.Trace("IncrementLaunchCount");
                ++usage.Model.GetCurrentUsage().NumberOfStartups;
                SaveUsage(usage);
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

            var currentTimeOffset = DateTimeOffset.UtcNow;

            if (usage.LastUpdated.Date != currentTimeOffset.Date)
            {
                await SendUsage(usage.Model);
                ClearCounters(usage.Model);
                usage.LastUpdated = currentTimeOffset;
                SaveUsage(usage);
            }
        }

        private async Task SendUsage(UsageModel usage)
        {
            //TODO: Be sure there shouldn't be a race condition here
            //var model = usage.Clone();

            await client.PostUsage(usage);
        }
    }
}
