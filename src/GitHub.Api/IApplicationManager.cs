using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IApplicationManager : IDisposable
    {
        CancellationToken CancellationToken { get; }
        IEnvironment Environment { get; }
        IPlatform Platform { get; }
        IProcessEnvironment GitEnvironment { get; }
        IProcessManager ProcessManager { get; }
        ITaskResultDispatcher MainThreadResultDispatcher { get; }
        ISettings SystemSettings { get; }
        ISettings LocalSettings { get; }
        ISettings UserSettings { get; }
        AppConfiguration AppConfiguration { get; }
        Task RestartRepository();
    }
}