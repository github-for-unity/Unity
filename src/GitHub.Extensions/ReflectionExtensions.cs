using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GitHub.Extensions
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static bool HasInterface(this Type type, Type targetInterface)
        {
            if (targetInterface.IsAssignableFrom(type))
                return true;
            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == targetInterface);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static string GetCustomAttributeValue<T>(this Assembly assembly, string propertyName) where T : Attribute
        {
            if (assembly == null || string.IsNullOrEmpty(propertyName)) return string.Empty;

            object[] attributes = assembly.GetCustomAttributes(typeof(T), false);
            if (attributes.Length == 0) return string.Empty;

            var attribute = attributes[0] as T;
            if (attribute == null) return string.Empty;

            var propertyInfo = attribute.GetType().GetProperty(propertyName);
            if (propertyInfo == null) return string.Empty;

            var value = propertyInfo.GetValue(attribute, null);
            return value.ToString();
        }

        public static object GetValueForProperty(this Type type, object instance, string propName)
        {
            var prop = type.GetProperty(propName);
            Debug.Assert(prop != null, string.Format(CultureInfo.InvariantCulture, "'{0}' {1} not found in assembly '{2}'. Check if it's been moved or mistyped.",
                propName, "property", type.Assembly.GetCustomAttributeValue<AssemblyFileVersionAttribute>("Version")));
            if (prop == null)
                return null;
            var getm = prop.GetGetMethod();
            Debug.Assert(prop != null, string.Format(CultureInfo.InvariantCulture, "'{0}' {1} not found in assembly '{2}'. Check if it's been moved or mistyped.",
                propName, "getter", type.Assembly.GetCustomAttributeValue<AssemblyFileVersionAttribute>("Version")));
            if (getm == null)
                return null;
            try {
                return getm.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' {1} in assembly '{2}' threw an exception. {3}.",
                    propName, "getter", type.Assembly.GetCustomAttributeValue<AssemblyFileVersionAttribute>("Version"), ex));
            }
        }
    }
}
