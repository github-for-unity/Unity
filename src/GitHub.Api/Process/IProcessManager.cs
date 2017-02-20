using System.Threading;

namespace GitHub.Unity
{
    interface IProcessManager
    {
        IProcess Configure(string processName, string processArguments, string gitRoot);
        IProcess Reconnect(int i);
        CancellationToken CancellationToken { get; }
    }
}