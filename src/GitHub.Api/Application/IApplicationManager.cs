using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IApplicationManager : IDisposable
    {
        CancellationToken CancellationToken { get; }
        IEnvironment Environment { get; }
        IPlatform Platform { get; }
        IProcessEnvironment GitEnvironment { get; }
        IProcessManager ProcessManager { get; }
        ISettings SystemSettings { get; }
        ISettings LocalSettings { get; }
        ISettings UserSettings { get; }
        ITaskManager TaskManager { get; }
        IGitClient GitClient { get; }
        IUsageTracker UsageTracker { get; }

        void Run(bool firstRun);
        void RestartRepository();
        ITask InitializeRepository();
    }
}