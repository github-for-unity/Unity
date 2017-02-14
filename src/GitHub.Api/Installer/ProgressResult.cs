using System;

namespace GitHub.Api
{
    public class ProgressResult
    {
        public ProgressResult()
        {}

        public ProgressResult(int value)
        {
            ProgressValue = value;
        }

        public ProgressResult(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }

        public int ProgressValue { get; private set; }
    }
}
