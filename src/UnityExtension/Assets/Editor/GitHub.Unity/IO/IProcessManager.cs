namespace GitHub.Unity
{
    interface IProcessManager
    {
        IProcess Configure(string processName, string processArguments, string gitRoot);
        IProcess Reconnect(int i);
    }
}