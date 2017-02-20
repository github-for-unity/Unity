namespace GitHub.Unity
{
    class ProcessOutputManager
    {
        public ProcessOutputManager(IProcess process, IOutputProcessor processor)
        {
            process.OnOutputData += processor.LineReceived;
            process.OnErrorData += ProcessError;
        }

        private void ProcessError(string data)
        {
        }
    }
}