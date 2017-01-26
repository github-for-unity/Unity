using System;

namespace GitHub.Unity
{
    interface IProcess
    {
        event Action<string> OnOutputData;
        event Action<string> OnErrorData;
        void Run();
        bool WaitForExit(int milliseconds);
        void WaitForExit();
        void Close();
        void Kill();
        int Id { get; }
        bool HasExited { get; }
        event Action<IProcess> OnExit;
    }
}