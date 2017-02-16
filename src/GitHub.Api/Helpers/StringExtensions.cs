using GitHub.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    static class StringExtensions
    {
        /// <summary>
        /// Pretty much the same things as `String.Join` but used when appending to an already delimited string. If the values passed
        /// in are empty, it does not prepend the delimeter. Otherwise, it prepends with the delimiter.
        /// </summary>
        /// <param name="separator">The separator character</param>
        /// <param name="values">The set values to join</param>
        public static string JoinForAppending(string separator, IEnumerable<string> values)
        {
            return values.Any()
                ? separator + String.Join(separator, values.ToArray())
                : string.Empty;
        }

        public static string RemoveSurroundingQuotes(this string s)
        {
            Guard.ArgumentNotNull(s, "string");

            if (s.Length < 2)
                return s;

            var quoteCharacters = new[] { '"', '\'' };
            char firstCharacter = s[0];
            if (!quoteCharacters.Contains(firstCharacter))
                return s;

            if (firstCharacter != s[s.Length - 1])
                return s;

            return s.Substring(1, s.Length - 2);
        }

        public static string RightAfter(this string s, char search)
        {
            if (s == null) return null;
            int lastIndex = s.IndexOf(search);
            if (lastIndex < 0)
                return null;

            return s.Substring(lastIndex + 1);
        }

        public static string RightAfterLast(this string s, char search)
        {
            if (s == null) return null;
            int lastIndex = s.LastIndexOf(search);
            if (lastIndex < 0)
                return null;

            return s.Substring(lastIndex + 1);
        }

        public static string LeftBeforeLast(this string s, char search)
        {
            if (s == null) return null;
            int lastIndex = s.LastIndexOf(search);
            if (lastIndex < 0)
                return null;

            return s.Substring(0, lastIndex);
        }

        public static StringResult? NextChunk(this string s, int start, char search)
        {
            if (s == null) return null;
            int index = s.IndexOf(search, start);
            if (index < 0)
                return null;

            return new StringResult { Chunk = s.Substring(start, index - start), Start = start, End = index };
        }

        public static StringResult? NextChunk(this string s, int start, string search)
        {
            if (s == null) return null;
            int index = s.IndexOf(search, start);
            if (index < 0)
                return null;

            return new StringResult { Chunk = s.Substring(start, index - start), Start = start, End = index };
        }
    }

    public struct StringResult
    {
        public string Chunk;
        public int Start;
        public int End;
    }
}
