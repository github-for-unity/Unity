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
        private static readonly Calendar cal = CultureInfo.InvariantCulture.Calendar;
        private static readonly ILogging logger = Logging.GetLogger<UsageTracker>();

        private readonly NPath storePath;

        private IMetricsService client = null;
        private bool firstRun = true;
        private Timer timer;

        public UsageTracker(NPath storePath)
        {
            logger.Trace("Created");

            this.storePath = storePath;

            RunTimer();
        }

        public async Task IncrementLaunchCount()
        {
            logger.Trace("IncrementLaunchCount");
            
            var usage = await LoadUsage();
            ++usage.Model.NumberOfStartups;
            ++usage.Model.NumberOfStartupsWeek;
            ++usage.Model.NumberOfStartupsMonth;
            SaveUsage(usage);
        }

        // http://blogs.msdn.com/b/shawnste/archive/2006/01/24/iso-8601-week-of-year-format-in-microsoft-net.aspx
        private static int GetIso8601WeekOfYear(DateTimeOffset time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            var day = cal.GetDayOfWeek(time.UtcDateTime);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return cal.GetWeekOfYear(time.UtcDateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private static void ClearCounters(UsageModel usage, bool weekly, bool monthly)
        {
            usage.NumberOfStartups = 0;

            if (weekly)
            {
                usage.NumberOfStartupsWeek = 0;
            }

            if (monthly)
            {
                usage.NumberOfStartupsMonth = 0;
            }
        }

        private async Task Initialize()
        {
            // The services needed by the usage tracker are loaded when they are first needed to
            // improve the startup time of the extension.
            await ThreadingHelper.SwitchToMainThreadAsync();
#if HAS_METRICS_SERVICE
            client = new MetricsService($"GitHub4Unity{AssemblyVersionInformation.Version}");
#endif
        }

        private async Task<UsageStore> LoadUsage()
        {
            await Initialize();

            string json = null;
            if (storePath.FileExists())
            {
                logger.Trace("ReadAllText: \"{0}\"", storePath);

                try
                {
                    json = storePath.ReadAllText(Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error {0}", ex.Message);
                }
            }

            UsageStore result = null;
            try
            {
                result = json != null
                    ? SimpleJson.DeserializeObject<UsageStore>(json)
                    : new UsageStore { Model = new UsageModel() };
            }
            catch
            {
                result = new UsageStore { Model = new UsageModel() };
            }

            result.Model.Lang = CultureInfo.InstalledUICulture.IetfLanguageTag;
            result.Model.AppVersion = AssemblyVersionInformation.Version;

            //TODO: Get Unity Version
            //result.Model.UnityVersion
            //result.Model.VSVersion = vsservices.VSVersion;

            return result;
        }

        private void SaveUsage(UsageStore store)
        {
            var json = SimpleJson.SerializeObject(store);

            var pathString = storePath.ToString();
            logger.Trace("WriteAllText: \"{0}\"", pathString);

            try
            {
                storePath.WriteAllText(json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                logger.Error("Error writing to \"{0}\"", pathString);
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

            await Initialize();

            if (firstRun)
            {
                await IncrementLaunchCount();

                timer.Interval = TimeSpan.FromHours(8).TotalMilliseconds;
                firstRun = false;
            }

            //TODO: Check User Settings
            //if (client == null || !userSettings.CollectMetrics)
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

            // Every time we increment the launch count we increment both daily and weekly
            // launch count but we only submit (and clear) the weekly launch count when we've
            // transitioned into a new week. We've defined a week by the ISO8601 definition,
            // i.e. week starting on Monday and ending on Sunday.
            var usage = await LoadUsage();
            var lastDate = usage.LastUpdated;
            var currentDate = DateTimeOffset.Now;
            var includeWeekly = GetIso8601WeekOfYear(lastDate) != GetIso8601WeekOfYear(currentDate);
            var includeMonthly = lastDate.Month != currentDate.Month;

            // Only send stats once a day.
            if (lastDate.Date != currentDate.Date)
            {
                await SendUsage(usage.Model, includeWeekly, includeMonthly);
                ClearCounters(usage.Model, includeWeekly, includeMonthly);
                usage.LastUpdated = DateTimeOffset.Now.UtcDateTime;
                SaveUsage(usage);
            }
        }

        private async Task SendUsage(UsageModel usage, bool weekly, bool monthly)
        {
            //Debug.Assert(client != null, "SendUsage should not be called when there is no IMetricsService");

            //            if (connectionManager.Connections.Any(x => x.HostAddress.IsGitHubDotCom()))
            //            {
            //                usage.IsGitHubUser = true;
            //            }
            //
            //            if (connectionManager.Connections.Any(x => !x.HostAddress.IsGitHubDotCom()))
            //            {
            //                usage.IsEnterpriseUser = true;
            //            }

            var model = usage.Clone(weekly, monthly);
            await client.PostUsage(model);
        }

        private class UsageStore
        {
            public DateTimeOffset LastUpdated { get; set; }
            public UsageModel Model { get; set; }
        }
    }
}
