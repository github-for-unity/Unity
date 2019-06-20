using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class LineParser
    {
        readonly private string line;
        private int current;

        public LineParser(string line)
        {
            this.line = line;
            current = 0;
        }

        public bool Matches(string search)
        {
            return line.Substring(current).StartsWith(search);
        }

        public bool Matches(char search)
        {
            return line[current] == search;
        }

        public bool Matches(Regex regex)
        {
            return regex.IsMatch(line, current);
        }

        public void MoveNext()
        {
            current++;
        }

        public void MoveToAfter(char c)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            while (line[current] != c && current < line.Length)
                current++;
            while (line[current] == c && current < line.Length)
                current++;
        }

        public void MoveToAfter(string str)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            var skip = line.Substring(current).IndexOf(str);
            if (skip < 0)
                return;
            current += skip + str.Length;
        }

        public void SkipWhitespace()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            while (current < line.Length && char.IsWhiteSpace(line[current]))
                current++;
        }

        /// <summary>
        /// Reads until it finds the separator and returns what it read.
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="skipCurrentIfMatch">If the current character matches the
        /// separator and you actually want to read the next match, set this to true (if you're tokenizing, for instance)</param>
        /// <returns></returns>
        public string ReadUntil(char separator, bool skipCurrentIfMatch = false)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            if (Matches(separator))
            {
                if (skipCurrentIfMatch)
                    MoveNext();
                else
                    return null;
            }

            var end = line.IndexOf(separator, current);
            if (end == -1)
                return null;
            LastSubstring = line.Substring(current, end - current);
            current = end;
            return LastSubstring;
        }


        public string ReadUntilWhitespaceTrim()
        {
            SkipWhitespace();
            if (IsAtEnd)
                return null;
            return ReadUntilWhitespace();
        }

        public string ReadUntilWhitespace()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            if (char.IsWhiteSpace(line[current]))
                return null;

            var end = current;
            while (end < line.Length && !char.IsWhiteSpace(line[end]))
                end++;

            if (end == current) // current character is a whitespace, read nothing
                return null;

            LastSubstring = line.Substring(current, end - current);
            current = end;
            return LastSubstring;
        }

        public string ReadChunk(char startChar, char endChar, bool excludeTerminators = true)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            var start = line.IndexOf(startChar);
            var end = line.IndexOf(endChar, start);
            LastSubstring = line.Substring(start + (excludeTerminators ? 1 : 0), end - start + (excludeTerminators ? -1 : 1));
            current = end;
            return LastSubstring;
        }

        public string ReadToEnd()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Already at end");
            LastSubstring = line.Substring(current);
            current = line.Length;
            return LastSubstring;
        }

        public string Read(int howMany)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            if (current + howMany > line.Length)
                return null;

            LastSubstring = line.Substring(current, howMany);
            current += howMany;
            return LastSubstring;
        }

        public char ReadChar()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            var ret = line[current];
            LastSubstring = ret.ToString();
            MoveNext();
            return ret;
        }

        public string ReadUntilLast(string str)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Already at end");
            var substr = line.Substring(current);
            var idx = substr.LastIndexOf(str);
            if (idx < 0)
                return ReadToEnd();
            LastSubstring = substr.Substring(0, idx);
            current += idx;
            return LastSubstring;
        }

        public bool IsAtEnd => line == null || line.Length == current;
        public bool IsAtWhitespace => line != null && Char.IsWhiteSpace(line[current]);
        public bool IsAtDigit => line != null && Char.IsDigit(line[current]);
        public bool IsAtLetter => line != null && Char.IsLetter(line[current]);
        public string LastSubstring { get; private set; }
    }
}
