using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OctoRun
{
    static class StringExtensions
    {
        public static bool Contains(this string s, string expectedSubstring, StringComparison comparison)
        {
            return s.IndexOf(expectedSubstring, comparison) > -1;
        }

        public static bool ContainsAny(this string s, IEnumerable<char> characters)
        {
            return s.IndexOfAny(characters.ToArray()) > -1;
        }

        public static string ToNullIfEmpty(this string s)
        {
            return String.IsNullOrEmpty(s) ? null : s;
        }

        public static bool StartsWith(this string s, char c)
        {
            if (String.IsNullOrEmpty(s)) return false;
            return s.First() == c;
        }

        public static string RightAfter(this string s, string search)
        {
            if (s == null) return null;
            int lastIndex = s.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (lastIndex < 0)
                return null;

            return s.Substring(lastIndex + search.Length);
        }

        public static string RightAfterLast(this string s, string search)
        {
            if (s == null) return null;
            int lastIndex = s.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (lastIndex < 0)
                return null;

            return s.Substring(lastIndex + search.Length);
        }

        public static string LeftBeforeLast(this string s, string search)
        {
            if (s == null) return null;
            int lastIndex = s.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (lastIndex < 0)
                return null;

            return s.Substring(0, lastIndex);
        }

        public static string TrimEnd(this string s, string suffix)
        {
            if (s == null) return null;
            if (!s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return s;

            return s.Substring(0, s.Length - suffix.Length);
        }

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
