using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    public static class ListExtensions
    {
        public static string Join<T>(this IEnumerable<T> list, string separator)
        {
            if (list == null)
                return null;
            return String.Join(separator, list.Select(x => x?.ToString()).ToArray());
        }

        public static IEnumerable<IList<string>> Spool<T>(this IEnumerable<T> items, int spoolLength)
        {
            var currentSpoolLength = 0;
            var currentList= new List<string>();

            foreach (var item in items)
            {
                var itemValue = item.ToString();
                var itemValueLength = itemValue.Length;

                if (currentSpoolLength + itemValueLength > spoolLength)
                {
                    yield return currentList;

                    currentSpoolLength = 0;
                    currentList = new List<string>();
                }

                currentSpoolLength += itemValueLength;
                currentList.Add(itemValue);
            }

            if (currentList.Any())
            {
                yield return currentList;
            }
        }

        public static T[] Append<T>(this T[] array, T item)
        {
            if (array == null)
                return new T[] { item };
            var ret = new T[array.Length];
            array.CopyTo(ret, 0);
            ret[ret.Length - 1] = item;
            return ret;
        }
    }
}
