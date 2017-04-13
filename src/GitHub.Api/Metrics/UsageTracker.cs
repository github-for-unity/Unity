using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class UsageTracker : IUsageTracker
    {
        private const string StoreFileName = "ghfunity.usage";

        private static readonly Calendar cal = CultureInfo.InvariantCulture.Calendar;
        private static readonly ILogging logger = Logging.GetLogger<UsageTracker>();

        private readonly IFileSystem fileSystem;
        private readonly NPath storePath;

        private IMetricsService client = null;
        private bool firstRun = true;
        private System.Timers.Timer timer;

        public UsageTracker(IFileSystem fileSystem, NPath storePath)
        {
            logger.Trace("Created");

            this.fileSystem = fileSystem;
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

        public async Task IncrementCloneCount()
        {
            logger.Trace("IncrementCloneCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfClones;
            SaveUsage(usage);
        }

        public async Task IncrementCommitCount()
        {
            logger.Trace("IncrementCommitCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfCommits;
            SaveUsage(usage);
        }

        public async Task IncrementFetchCount()
        {
            logger.Trace("IncrementFetchCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfFetches;
            SaveUsage(usage);
        }

        public async Task IncrementPullCount()
        {
            logger.Trace("IncrementPullCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfPulls;
            SaveUsage(usage);
        }

        public async Task IncrementPushCount()
        {
            logger.Trace("IncrementPushCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfPushes;
            SaveUsage(usage);
        }

        public async Task IncrementLockCount()
        {
            logger.Trace("IncrementLockCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfLocks;
            SaveUsage(usage);
        }

        public async Task IncrementUnlockCount()
        {
            logger.Trace("IncrementUnlockCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfUnlocks;
            SaveUsage(usage);
        }

        public async Task IncrementCreateCount()
        {
            logger.Trace("IncrementCreateCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfReposCreated;
            SaveUsage(usage);
        }

        public async Task IncrementPublishCount()
        {
            logger.Trace("IncrementPublishCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfReposPublished;
            SaveUsage(usage);
        }

        public async Task IncrementOpenInGitHubCount()
        {
            logger.Trace("IncrementOpenInGitHubCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfOpenInGitHub;
            SaveUsage(usage);
        }

        public async Task IncrementLinkToGitHubCount()
        {
            logger.Trace("IncrementLinkToGitHubCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfLinkToGitHub;
            SaveUsage(usage);
        }

        public async Task IncrementCreateGistCount()
        {
            logger.Trace("IncrementCreateGistCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfGists;
            SaveUsage(usage);
        }

        public async Task IncrementUpstreamPullRequestCount()
        {
            logger.Trace("IncrementUpstreamPullRequestCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfUpstreamPullRequests;
            SaveUsage(usage);
        }

        public async Task IncrementLoginCount()
        {
            logger.Trace("IncrementLoginCount");

            var usage = await LoadUsage();
            ++usage.Model.NumberOfLogins;
            SaveUsage(usage);
        }

        public async Task IncrementPullRequestCheckOutCount(bool fork)
        {
            logger.Trace("IncrementPullRequestCheckOutCount");

            var usage = await LoadUsage();

            if (fork)
            {
                ++usage.Model.NumberOfForkPullRequestsCheckedOut;
            }
            else
            {
                ++usage.Model.NumberOfLocalPullRequestsCheckedOut;
            }

            SaveUsage(usage);
        }

        public async Task IncrementPullRequestPushCount(bool fork)
        {
            logger.Trace("IncrementPullRequestPushCount");

            var usage = await LoadUsage();

            if (fork)
            {
                ++usage.Model.NumberOfForkPullRequestPushes;
            }
            else
            {
                ++usage.Model.NumberOfLocalPullRequestPushes;
            }

            SaveUsage(usage);
        }

        public async Task IncrementPullRequestPullCount(bool fork)
        {
            logger.Trace("IncrementPullRequestPullCount");

            var usage = await LoadUsage();

            if (fork)
            {
                ++usage.Model.NumberOfForkPullRequestPulls;
            }
            else
            {
                ++usage.Model.NumberOfLocalPullRequestPulls;
            }

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
            usage.NumberOfClones = 0;
            usage.NumberOfReposCreated = 0;
            usage.NumberOfReposPublished = 0;
            usage.NumberOfGists = 0;
            usage.NumberOfOpenInGitHub = 0;
            usage.NumberOfLinkToGitHub = 0;
            usage.NumberOfLogins = 0;
            usage.NumberOfUpstreamPullRequests = 0;
            usage.NumberOfPullRequestsOpened = 0;
            usage.NumberOfLocalPullRequestsCheckedOut = 0;
            usage.NumberOfLocalPullRequestPulls = 0;
            usage.NumberOfLocalPullRequestPushes = 0;
            usage.NumberOfForkPullRequestsCheckedOut = 0;
            usage.NumberOfForkPullRequestPulls = 0;
            usage.NumberOfForkPullRequestPushes = 0;

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
            timer = new System.Timers.Timer(TimeSpan.FromMinutes(3).TotalMilliseconds);
        }

        private void TimerTick()
        {
            TimerTickAsync().Forget();
        }

        private async Task TimerTickAsync()
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
