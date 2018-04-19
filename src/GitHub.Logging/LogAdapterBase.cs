namespace GitHub.Logging
{
    public abstract class LogAdapterBase
    {
        public abstract void Info(string context, string message);

        public abstract void Debug(string context, string message);

        public abstract void Trace(string context, string message);

        public abstract void Warning(string context, string message);

        public abstract void Error(string context, string message);
    }
}