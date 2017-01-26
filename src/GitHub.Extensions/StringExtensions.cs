using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GitHub.Extensions
{
    public static class StringExtensions
    {
        public static bool Contains(this string s, string expectedSubstring, StringComparison comparison)
        {
            return s.IndexOf(expectedSubstring, comparison) > -1;
        }

        public static bool ContainsAny(this string s, IEnumerable<char> characters)
        {
            return s.IndexOfAny(characters.ToArray()) > -1;
        }

        public static string DebugRepresentation(this string s)
        {
            s = s ?? "(null)";
            return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", s);
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

        // Returns a file name even if the path is FUBAR.

        public static string ParseFileName(this string path)
        {
            if (path == null) return null;
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).RightAfterLast(Path.DirectorySeparatorChar + "");
        }

        // Returns the parent directory even if the path is FUBAR.

        public static string ParseParentDirectory(this string path)
        {
            if (path == null) return null;
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).LeftBeforeLast(Path.DirectorySeparatorChar + "");
        }


        public static string EnsureStartsWith(this string s, char c)
        {
            if (s == null) return null;
            return c + s.TrimStart(c);
        }

        // Ensures the string ends with the specified character.

        public static string EnsureEndsWith(this string s, char c)
        {
            if (s == null) return null;
            return s.TrimEnd(c) + c;
        }


        public static string NormalizePath(this string path)
        {
            if (String.IsNullOrEmpty(path)) return null;

            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }


        public static string TrimEnd(this string s, string suffix)
        {
            if (s == null) return null;
            if (!s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return s;

            return s.Substring(0, s.Length - suffix.Length);
        }

        public static string RemoveSurroundingQuotes(this string s)
        {
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

        public static Int32 ToInt32(this string s)
        {
            Int32 val;
            return Int32.TryParse(s, out val) ? val : 0;
        }

        /// <summary>
        /// Wrap a string to the specified length.
        /// </summary>
        /// <param name="text">The text string to wrap</param>
        /// <param name="maxLength">The character length to wrap at</param>
        /// <returns>A wrapped string using the platform's default newline character. This string will end in a newline.</returns>
        public static string Wrap(this string text, int maxLength = 72)
        {
            if (text.Length == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var unwrappedLine in text.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                var line = new StringBuilder();
                foreach (var word in unwrappedLine.Split(' '))
                {
                    var needsLeadingSpace = line.Length > 0;

                    var extraLength = (needsLeadingSpace ? 1 : 0) + word.Length;
                    if (line.Length + extraLength > maxLength)
                    {
                        sb.AppendLine(line.ToString());
                        line.Clear();
                        needsLeadingSpace = false;
                    }

                    if (needsLeadingSpace)
                        line.Append(" ");

                    line.Append(word);
                }

                sb.AppendLine(line.ToString());
            }

            return sb.ToString();
        }

        public static Uri ToUriSafe(this string url)
        {
            Uri uri;
            Uri.TryCreate(url, UriKind.Absolute, out uri);
            return uri;
        }
    }

    public static class StringBuilderExtensions
    {
        public static void Clear(this StringBuilder sb)
        {
            sb.Length = 0;
        }
    }
}
