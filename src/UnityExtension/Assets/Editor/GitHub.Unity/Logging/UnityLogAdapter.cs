namespace GitHub.Unity
{
    class UnityLogAdapter : LogAdapterBase
    {
        public UnityLogAdapter(string context) : base(context)
        {}

        public override void Info(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public override void Debug(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public override void Trace(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public override void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public override void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}