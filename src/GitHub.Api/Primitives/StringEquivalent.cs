using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace GitHub.Unity
{
    [Serializable]
    public abstract class StringEquivalent<T> : ISerializable where T : StringEquivalent<T>
    {
        protected string Value;

        protected StringEquivalent(string value)
        {
            Value = value;
        }

        protected StringEquivalent()
        {
        }

        public abstract T Combine(string addition);

        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Add doesn't make sense in the case of a string equivalent")]
        public static T operator +(StringEquivalent<T> a, string b)
        {
            return a.Combine(b);
        }

        public static bool operator ==(StringEquivalent<T> a, StringEquivalent<T> b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Value.Equals(b.Value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(StringEquivalent<T> a, StringEquivalent<T> b)
        {
            return !(a == b);
        }

        public override bool Equals(Object obj)
        {
            return obj != null && Equals(obj as T) || Equals(obj as string);
        }

        public virtual bool Equals(T stringEquivalent)
        {
            return this == stringEquivalent;
        }

        public override int GetHashCode()
        {
            return (Value ?? "").GetHashCode();
        }

        public virtual bool Equals(string other)
        {
            return other != null && Value == other;
        }

        public override string ToString()
        {
            return Value;
        }

        protected StringEquivalent(SerializationInfo info) : this(info.GetValue("Value", typeof(string)) as string)
        {
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Value", Value);
        }

        public int Length
        {
            get { return Value != null ? Value.Length : 0; }
        }
    }
}
