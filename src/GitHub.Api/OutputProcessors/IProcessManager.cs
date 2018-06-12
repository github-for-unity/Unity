using System;
using System.Threading;

namespace GitHub.Unity
{
    public interface IProcessManager : IDisposable
    {
        T Configure<T>(T processTask, NPath? executable = null, string arguments = null, NPath? workingDirectory = null,
        	bool withInput = false, bool dontSetupGit = false)
            where T : IProcess;
        IProcess Reconnect(IProcess processTask, int i);
        void RunCommandLineWindow(NPath workingDirectory);
        void Stop();
    }
}
