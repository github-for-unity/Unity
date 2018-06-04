using System.Threading;

namespace GitHub.Unity
{
    public interface IProcessManager
    {
        T Configure<T>(T processTask, NPath? executable = null, string arguments = null, NPath? workingDirectory = null,
        	bool withInput = false, bool dontSetupGit = false)
            where T : IProcess;
        IProcess Reconnect(IProcess processTask, int i);
        CancellationToken CancellationToken { get; }
        void RunCommandLineWindow(NPath workingDirectory);
        void Stop();
    }
}
