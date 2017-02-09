using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }

        public static IEnumerable<TSource> Except<TSource>(
            this IEnumerable<TSource> enumerable,
            IEnumerable<TSource> second,
            Func<TSource, TSource, int> comparer)
        {
            return enumerable.Except(second, new Collections.LambdaComparer<TSource>(comparer));
        }

    }
}
