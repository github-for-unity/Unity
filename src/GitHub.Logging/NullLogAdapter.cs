namespace GitHub.Logging
{
    public class NullLogAdapter : LogAdapterBase
    {
        public override void Info(string context, string message)
        {
        }

        public override void Debug(string context, string message)
        {
        }

        public override void Trace(string context, string message)
        {
        }

        public override void Warning(string context, string message)
        {
        }

        public override void Error(string context, string message)
        {
        }
    }
}
