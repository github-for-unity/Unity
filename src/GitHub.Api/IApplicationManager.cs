using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IApplicationManager
    {
        CancellationToken CancellationToken { get; }
        IEnvironment Environment { get; }
        IFileSystem FileSystem { get; }
        IPlatform Platform { get; }
        IProcessEnvironment GitEnvironment { get; }
        IProcessManager ProcessManager { get; }
        ITaskResultDispatcher MainThreadResultDispatcher { get; }
        ISettings SystemSettings { get; }
        ISettings LocalSettings { get; }
        ISettings UserSettings { get; }
        Task RestartRepository();
    }
}