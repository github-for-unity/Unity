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

            while (!Char.IsWhiteSpace(line[current]) && current < line.Length)
                current++;
            while (Char.IsWhiteSpace(line[current]) && current < line.Length)
                current++;
        }

        public string ReadUntil(char separator)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            if (line[current] == separator)
                current++;
            var end = line.IndexOf(separator, current);
            if (end == -1)
                return null;
            LastSubstring = line.Substring(current, end - current);
            current = end;
            return LastSubstring;
        }

        public string ReadUntilWhitespace()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of line");

            if (Char.IsWhiteSpace(line[current]))
                SkipWhitespace();

            int end = line.Length;
            for (var i = current; i < end; i++)
            {
                if (Char.IsWhiteSpace(line[i]))
                {
                    end = i;
                    break;
                }
            }
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

        public bool IsAtEnd { get { return line != null ? line.Length == current : true; } }
        public bool IsAtWhitespace { get { return line != null && Char.IsWhiteSpace(line[current]); } }
        public bool IsAtDigit { get { return line != null && Char.IsDigit(line[current]); } }
        public bool IsAtLetter { get { return line != null && Char.IsLetter(line[current]); } }
        public string LastSubstring { get; private set; }
    }
}