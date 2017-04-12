using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using GitHub.Unity;

namespace GitHub.Services
{
    public class UsageTracker : IUsageTracker
    {
        const string StoreFileName = "ghfunity.usage";
        static readonly Calendar cal = CultureInfo.InvariantCulture.Calendar;

        //readonly DispatcherTimer timer;
        //IMetricsService client;

        string storePath;
        bool firstRun = true;

        Func<string, bool> fileExists;
        Func<string, Encoding, string> readAllText;
        Action<string, string, Encoding> writeAllText;
        Action<string> dirCreate;

        public UsageTracker()
        {
            fileExists = (path) => System.IO.File.Exists(path);
            readAllText = (path, encoding) =>
            {
                try
                {
                    return System.IO.File.ReadAllText(path, encoding);
                }
                catch
                {
                    return null;
                }
            };
            writeAllText = (path, content, encoding) =>
            {
                try
                {
                    System.IO.File.WriteAllText(path, content, encoding);
                }
                catch {}
            };
            dirCreate = (path) => System.IO.Directory.CreateDirectory(path);

//            this.timer = new DispatcherTimer(
//                TimeSpan.FromMinutes(3),
//                DispatcherPriority.Background,
//                TimerTick,
//                ThreadingHelper.MainThreadDispatcher);

            RunTimer();
        }

        public async Task IncrementLaunchCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfStartups;
            ++usage.Model.NumberOfStartupsWeek;
            ++usage.Model.NumberOfStartupsMonth;
            SaveUsage(usage);
        }

        public async Task IncrementCloneCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfClones;
            SaveUsage(usage);
        }

        public async Task IncrementCreateCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfReposCreated;
            SaveUsage(usage);
        }

        public async Task IncrementPublishCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfReposPublished;
            SaveUsage(usage);
        }

        public async Task IncrementOpenInGitHubCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfOpenInGitHub;
            SaveUsage(usage);
        }

        public async Task IncrementLinkToGitHubCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfLinkToGitHub;
            SaveUsage(usage);
        }

        public async Task IncrementCreateGistCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfGists;
            SaveUsage(usage);
        }

        public async Task IncrementUpstreamPullRequestCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfUpstreamPullRequests;
            SaveUsage(usage);
        }

        public async Task IncrementLoginCount()
        {
            var usage = await LoadUsage();
            ++usage.Model.NumberOfLogins;
            SaveUsage(usage);
        }

        public async Task IncrementPullRequestCheckOutCount(bool fork)
        {
            var usage = await LoadUsage();

            if (fork)
                ++usage.Model.NumberOfForkPullRequestsCheckedOut;
            else
                ++usage.Model.NumberOfLocalPullRequestsCheckedOut;

            SaveUsage(usage);
        }

        public async Task IncrementPullRequestPushCount(bool fork)
        {
            var usage = await LoadUsage();

            if (fork)
                ++usage.Model.NumberOfForkPullRequestPushes;
            else
                ++usage.Model.NumberOfLocalPullRequestPushes;

            SaveUsage(usage);
        }

        public async Task IncrementPullRequestPullCount(bool fork)
        {
            var usage = await LoadUsage();

            if (fork)
                ++usage.Model.NumberOfForkPullRequestPulls;
            else
                ++usage.Model.NumberOfLocalPullRequestPulls;

            SaveUsage(usage);
        }

        async Task Initialize()
        {
            // The services needed by the usage tracker are loaded when they are first needed to
            // improve the startup time of the extension.
//            if (userSettings == null)
//            {
//                await ThreadingHelper.SwitchToMainThreadAsync();
//
//                client = gitHubServiceProvider.GetService<IMetricsService>();
//                connectionManager = gitHubServiceProvider.GetService<IConnectionManager>();
//                userSettings = gitHubServiceProvider.GetService<IPackageSettings>();
//                vsservices = gitHubServiceProvider.GetService<IVSServices>();
//
//                var program = gitHubServiceProvider.GetService<IProgram>();
//                storePath = System.IO.Path.Combine(
//                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//                    program.ApplicationName,
//                    StoreFileName);
//            }
        }

        async Task<UsageStore> LoadUsage()
        {
            await Initialize();

            var json = fileExists(storePath) ? readAllText(storePath, Encoding.UTF8) : null;
            UsageStore result = null;
            try
            {
                result = json != null ?
                    SimpleJson.DeserializeObject<UsageStore>(json) :
                    new UsageStore { Model = new UsageModel() };
            }
            catch
            {
                result = new UsageStore { Model = new UsageModel() };
            }

            result.Model.Lang = CultureInfo.InstalledUICulture.IetfLanguageTag;
            result.Model.AppVersion = AssemblyVersionInformation.Version;
            //result.Model.VSVersion = vsservices.VSVersion;

            return result;
        }

        void SaveUsage(UsageStore store)
        {
            dirCreate(System.IO.Path.GetDirectoryName(storePath));
            var json = SimpleJson.SerializeObject(store);
            writeAllText(storePath, json, Encoding.UTF8);
        }

        void RunTimer()
        {
            // The timer first ticks after 3 minutes to allow things to settle down after startup.
            // This will be changed to 8 hours after the first tick by the TimerTick method.
            //timer.Start();
        }

        void TimerTick(object sender, EventArgs e)
        {
//            TimerTick()
//                .Catch(ex =>
//                {
//                    //log.Warn("Failed submitting usage data", ex);
//                })
//                .Forget();
        }

        async Task TimerTick()
        {
            await Initialize();

//            if (firstRun)
//            {
//                await IncrementLaunchCount();
//                timer.Interval = TimeSpan.FromHours(8);
//                firstRun = false;
//            }
//
//            if (client == null || !userSettings.CollectMetrics)
//            {
//                timer.Stop();
//                return;
//            }

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

        async Task SendUsage(UsageModel usage, bool weekly, bool monthly)
        {
//            Debug.Assert(client != null, "SendUsage should not be called when there is no IMetricsService");

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
//            await client.PostUsage(model);
            throw new NotImplementedException();
        }

        // http://blogs.msdn.com/b/shawnste/archive/2006/01/24/iso-8601-week-of-year-format-in-microsoft-net.aspx
        static int GetIso8601WeekOfYear(DateTimeOffset time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = cal.GetDayOfWeek(time.UtcDateTime);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return cal.GetWeekOfYear(time.UtcDateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        static void ClearCounters(UsageModel usage, bool weekly, bool monthly)
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
                usage.NumberOfStartupsWeek = 0;

            if (monthly)
                usage.NumberOfStartupsMonth = 0;
        }

        class UsageStore
        {
            public DateTimeOffset LastUpdated { get; set; }
            public UsageModel Model { get; set; }
        }
    }
}
