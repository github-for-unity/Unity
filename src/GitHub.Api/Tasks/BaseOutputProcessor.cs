using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitHub.Unity
{
    public interface IOutputProcessor
    {
        bool LineReceived(string line);
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

        public abstract bool LineReceived(string line);
        protected void RaiseOnEntry(T entry)
        {
            Result = entry;
            OnEntry?.Invoke(entry);
        }
        public virtual T Result { get; protected set; }

        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
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
        public override bool LineReceived(string line)
        {
            if (line == null)
                return false;
            sb.AppendLine(line);
            RaiseOnEntry(line);
            return false;
        }
        public override string Result { get { return sb.ToString(); } }
    }

    class SimpleListOutputProcessor : BaseOutputListProcessor<string>
    {
        public override bool LineReceived(string line)
        {
            if (line == null)
                return false;
            RaiseOnEntry(line);
            return false;
        }
    }

    abstract class FirstResultOutputProcessor<T> : BaseOutputProcessor<T>
    {
        private bool isSet = false;
        public override bool LineReceived(string line)
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
            return false;
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
            result = NPath.Default;
            if (String.IsNullOrEmpty(line))
                return false;
            result = line.ToNPath();
            return true;
        }
    }

    class GitNetworkOperationOutputProcessor : BaseOutputProcessor<string>
    {
        private readonly StringBuilder sb = new StringBuilder();
        public override bool LineReceived(string line)
        {
            if (line == null)
                return false;
            sb.AppendLine(line);
            RaiseOnEntry(line);
            return line.StartsWith("Enter");
        }
        public override string Result { get { return sb.ToString(); } }
    }

}