using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IApplicationManager : IDisposable
    {
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
        IOAuthCallbackManager OAuthCallbackManager { get; }
        bool IsBusy { get; }
        void Run();
        void InitializeRepository();
        void GenerateGitignore(NPath path);
        event Action<IProgress> OnProgress;
        void SetupGit(GitInstaller.GitInstallationState state);
        void RestartRepository();
    }
}
