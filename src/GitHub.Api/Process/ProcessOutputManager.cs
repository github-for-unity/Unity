namespace GitHub.Unity
{
    class ProcessOutputManager
    {
        public ProcessOutputManager(IProcess process, IOutputProcessor processor)
        {
            process.OnOutputData += processor.LineReceived;
        }
    }
}