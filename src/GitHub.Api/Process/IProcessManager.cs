using System.Threading;

namespace GitHub.Unity
{
    interface IProcessManager
    {
        T ConfigureGitProcess<T>(T processTask, bool withInput = false) where T : IProcess;
        T Configure<T>(T processTask, string executableFileName, string arguments, string workingDirectory = null, bool withInput = false)
            where T : IProcess;
        IProcess Reconnect(IProcess processTask, int i);
        CancellationToken CancellationToken { get; }
        IProcess RunCommandLineWindow(string workingDirectory);
    }
}