using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GitHub.Unity
{
    class PropertyOrFieldNullException : Exception
    {
        public PropertyOrFieldNullException(string message) : base(message)
        { }
    }

    static class Guard
    {
        public static void PropertyOrFieldNotNull(object value, string name)
        {
            if (value != null) return;
            string message = String.Format(CultureInfo.InvariantCulture, $"Property or Field {name} is null");
            throw new PropertyOrFieldNullException(message);
        }

        public static void ArgumentNotNull(object value, string name)
        {
            if (value != null) return;
            string message = String.Format(CultureInfo.InvariantCulture, "Failed Null Check on '{0}'", name);
            throw new ArgumentNullException(name, message);
        }

        public static void ArgumentNotNullOrEmpty<T>(IList<T> value, string name)
        {
            if (value == null)
            {
                string message = String.Format(CultureInfo.InvariantCulture, "Failed Null Check on '{0}'", name);
                throw new ArgumentNullException(name, message);
            }

            if (!value.Any())
            {
                string message = String.Format(CultureInfo.InvariantCulture, "Failed Empty Check on '{0}'", name);
                throw new ArgumentNullException(name, message);
            }
        }

        public static void ArgumentNonNegative(int value, string name)
        {
            if (value > -1) return;

            var message = String.Format(CultureInfo.InvariantCulture, "The value for '{0}' must be non-negative", name);
            throw new ArgumentException(message, name);
        }

        /// <summary>
        ///   Checks a string argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name = "value">The argument value to check.</param>
        /// <param name = "name">The name of the argument.</param>
        public static void ArgumentNotNullOrWhiteSpace(string value, string name)
        {
            if (value != null && value.Trim().Length > 0)
                return;
            string message = String.Format(CultureInfo.InvariantCulture, "The value for '{0}' must not be empty", name);
            throw new ArgumentException(message, name);
        }

        public static void ArgumentInRange(int value, int minValue, string name)
        {
            if (value >= minValue) return;
            string message = String.Format(CultureInfo.InvariantCulture,
                "The value '{0}' for '{1}' must be greater than or equal to '{2}'",
                value,
                name,
                minValue);
            throw new ArgumentOutOfRangeException(name, message);
        }

        public static void ArgumentInRange(int value, int minValue, int maxValue, string name)
        {
            if (value >= minValue && value <= maxValue) return;
            string message = String.Format(CultureInfo.InvariantCulture,
                "The value '{0}' for '{1}' must be greater than or equal to '{2}' and less than or equal to '{3}'",
                value,
                name,
                minValue,
                maxValue);
            throw new ArgumentOutOfRangeException(name, message);
        }

        public static bool InUnitTestRunner { get; set; }
    }
}