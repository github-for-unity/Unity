namespace GitHub.Logging
{
    public class MultipleLogAdapter : LogAdapterBase
    {
        private readonly LogAdapterBase[] logAdapters;

        public MultipleLogAdapter(params LogAdapterBase[] logAdapters)
        {
            this.logAdapters = logAdapters ?? new LogAdapterBase[0];
        }

        public override void Info(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Info(context, message);
            }
        }

        public override void Debug(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Debug(context, message);
            }
        }

        public override void Trace(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Trace(context, message);
            }
        }

        public override void Warning(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Warning(context, message);
            }
        }

        public override void Error(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Error(context, message);
            }
        }
    }
}
