using System;
using System.Collections.Generic;
using System.Text;

namespace GitHub.Unity
{
    public interface IOutputProcessor
    {
        void LineReceived(string line);
    }

    public interface IOutputProcessor<T> : IOutputProcessor
    {
        T Result { get; }
        event Action<T> OnEntry;
    }

    public interface IOutputProcessor<TData, T> : IOutputProcessor<T>
    {
        new event Action<TData> OnEntry;
    }

    public abstract class BaseOutputProcessor<T> : IOutputProcessor<T>
    {
        public event Action<T> OnEntry;

        public abstract void LineReceived(string line);
        protected void RaiseOnEntry(T entry)
        {
            Result = entry;
            OnEntry?.Invoke(entry);
        }
        public virtual T Result { get; protected set; }

        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? Logging.GetLogger(GetType()); } }
    }

    public abstract class BaseOutputProcessor<TData, T> : BaseOutputProcessor<T>, IOutputProcessor<TData, T>
    {
        public new event Action<TData> OnEntry;

        protected virtual void RaiseOnEntry(TData entry)
        {
            OnEntry?.Invoke(entry);
        }
    }

    public abstract class BaseOutputListProcessor<T> : BaseOutputProcessor<T, List<T>>
    {
        protected override void RaiseOnEntry(T entry)
        {
            if (Result == null)
            {
                Result = new List<T>();
            }
            Result.Add(entry);
            base.RaiseOnEntry(entry);
        }
    }

    class SimpleOutputProcessor : BaseOutputProcessor<string>
    {
        private readonly StringBuilder sb = new StringBuilder();
        public override void LineReceived(string line)
        {
            if (line == null)
                return;
            sb.AppendLine(line);
            RaiseOnEntry(line);
        }
        public override string Result { get { return sb.ToString(); } }
    }

    class SimpleListOutputProcessor : BaseOutputListProcessor<string>
    {
        public override void LineReceived(string line)
        {
            if (line == null)
                return;
            RaiseOnEntry(line);
        }
    }

    abstract class FirstResultOutputProcessor<T> : BaseOutputProcessor<T>
    {
        private bool isSet = false;
        public override void LineReceived(string line)
        {
            if (!isSet)
            {
                T res;
                if (ProcessLine(line, out res))
                {
                    Result = res;
                    isSet = true;
                    RaiseOnEntry(res);
                }
            }
        }

        protected abstract bool ProcessLine(string line, out T result);
    }

    class FirstNonNullLineOutputProcessor : FirstResultOutputProcessor<string>
    {
        protected override bool ProcessLine(string line, out string result)
        {
            result = null;
            if (String.IsNullOrEmpty(line))
                return false;
            result = line;
            return true;
        }
    }

    class FirstLineIsPathOutputProcessor : FirstResultOutputProcessor<NPath>
    {
        protected override bool ProcessLine(string line, out NPath result)
        {
            result = null;
            if (String.IsNullOrEmpty(line))
                return false;
            result = line.ToNPath();
            return true;
        }
    }
}