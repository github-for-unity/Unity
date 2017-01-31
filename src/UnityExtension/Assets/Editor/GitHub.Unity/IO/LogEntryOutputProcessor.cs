using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class LogEntryOutputProcessor : BaseOutputProcessor
    {
        private static readonly ILogger logger = Logger.GetLogger<LogEntryOutputProcessor>();

        private readonly IGitStatusEntryFactory gitStatusEntryFactory;
        public event Action<GitLogEntry> OnLogEntry;

        private int phase;
        private string authorName;
        private string mergeA;
        private string mergeB;
        private List<GitStatusEntry> changes;
        private string authorEmail;
        private string summary;
        private List<string> descriptionLines;
        private string commitId;
        private DateTimeOffset? time;
        private int newlineCount;

        public LogEntryOutputProcessor(IGitStatusEntryFactory gitStatusEntryFactory)
        {
            this.gitStatusEntryFactory = gitStatusEntryFactory;
            Reset();
        }

        private void Reset()
        {
            phase = 0;
            authorName = null;
            mergeA = null;
            mergeB = null;
            changes = new List<GitStatusEntry>();
            authorEmail = null;
            summary = null;
            descriptionLines = new List<string>();
            commitId = null;
            time = null;
            newlineCount = 0;
        }

        public override void LineReceived(string line)
        {
            base.LineReceived(line);

            logger.Debug(@"LineReceived: ""{0}""", line);

            if (OnLogEntry == null)
                return;

            if (line == null)
            {
                ReturnGitLogEntry();
                return;
            }

            var proc = new LineParser(line);
            switch (phase)
            {
                case 0:
                    if (proc.Matches("commit"))
                    {
                        //commit ee1cd912a5728f8fe268130791fd61ab3e69d941

                        proc.MoveToAfter(' ');
                        commitId = proc.ReadToEnd();

                        phase++;
                    }
                    else if (proc.Matches("fatal"))
                    {
                        //fatal: your current branch 'master' does not have any commits yet
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                    break;

                case 1:
                    // optionally
                    // Merge: cf6c9c41 def9a702
                    if (proc.Matches("Merge:"))
                    {
                        proc.MoveToAfter(' ');
                        mergeA = proc.ReadUntilWhitespace();
                        proc.SkipWhitespace();
                        mergeB = proc.ReadToEnd();
                    }

                    //finally
                    //Author: Andreia Gaita <shana@users.noreply.github.com>
                    else if (proc.Matches("Author"))
                    {
                        proc.MoveToAfter(' ');
                        authorName = proc.ReadUntil('<');
                        proc.MoveNext();

                        authorName = authorName.Substring(0, authorName.Length - 1);
                        authorEmail = proc.ReadUntil('>');

                        phase++;
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                    break;

                case 2:
                    //Date:   Tue Jan 24 16:34:49 2017 + 0100
                    if (proc.Matches("Date:"))
                    {
                        proc.MoveToAfter(':');
                        proc.SkipWhitespace();

                        //Skipping day
                        proc.ReadUntilWhitespace();

                        var monthString = proc.ReadUntilWhitespace();
                        int month = DateTime.ParseExact(monthString, "MMM", CultureInfo.CurrentCulture).Month;

                        var date = int.Parse(proc.ReadUntilWhitespace());
                        var hour = int.Parse(proc.ReadUntil(':'));
                        proc.MoveNext();

                        var minute = int.Parse(proc.ReadUntil(':'));
                        proc.MoveNext();

                        var second = int.Parse(proc.ReadUntilWhitespace());
                        var year = int.Parse(proc.ReadUntilWhitespace());
                        proc.SkipWhitespace();

                        var timezoneOffset = int.Parse(proc.ReadToEnd());

                        var offset = TimeSpan.FromHours(timezoneOffset / 100.0);
                        time = new DateTimeOffset(year, month, date, hour, minute, second, offset);

                        phase++;
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                    break;

                case 3:
                    //blank line
                    if (proc.IsAtEnd)
                    {
                        phase++;
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                    break;

                case 4:
                    if (proc.IsAtWhitespace)
                    {
                        //summary
                        proc.SkipWhitespace();
                        summary = proc.ReadToEnd();
                        descriptionLines.Add(summary);
                        phase++;
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                    break;

                case 5:
                    //blank line
                    if (proc.IsAtEnd)
                    {
                        phase++;
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                    break;

                case 6:
                    if (proc.IsAtEnd)
                    {
                        if (changes.Count != 0)
                        {
                            //Tf we have recieved files, an empty string signifies the end of this entry
                            ReturnGitLogEntry();
                        }
                        else
                        {
                            //else, we have not recieved files, an empty string may preceede files or may be a newline in the description
                            newlineCount++;
                        }
                    }
                    else if (proc.IsAtWhitespace)
                    {
                        //If this line starts with whitespace it is description text

                        PopNewlines();
                        proc.SkipWhitespace();
                        descriptionLines.Add(proc.ReadToEnd());
                    }
                    else if (proc.Matches('M'))
                    {
                        //If this is the first file change decrement the newline count
                        if (changes.Count == 0 && newlineCount > 0)
                        {
                            newlineCount--;
                        }

                        proc.ReadUntilWhitespace();
                        proc.SkipWhitespace();

                        var path = proc.ReadToEnd();

                        var gitStatusEntry = gitStatusEntryFactory.Create(path, GitFileStatus.Modified);
                        changes.Add(gitStatusEntry);
                    }
                    else if (proc.Matches('R'))
                    {
                        //If this is the first file change decrement the newline count
                        if (changes.Count == 0 && newlineCount > 0)
                        {
                            newlineCount--;
                        }

                        proc.ReadUntilWhitespace();
                        proc.SkipWhitespace();

                        var originalPath = proc.ReadUntilWhitespace();
                        proc.SkipWhitespace();

                        var path = proc.ReadToEnd();

                        var gitStatusEntry = gitStatusEntryFactory.Create(path, GitFileStatus.Renamed, originalPath);
                        changes.Add(gitStatusEntry);
                    }
                    else if (proc.Matches('A'))
                    {
                        //If this is the first file change decrement the newline count
                        if (changes.Count == 0 && newlineCount > 0)
                        {
                            newlineCount--;
                        }

                        proc.ReadUntilWhitespace();
                        proc.SkipWhitespace();

                        var path = proc.ReadToEnd();

                        var gitStatusEntry = gitStatusEntryFactory.Create(path, GitFileStatus.Added);
                        changes.Add(gitStatusEntry);
                    }
                    else if (proc.Matches('D'))
                    {
                        //If this is the first file change decrement the newline count
                        if (changes.Count == 0 && newlineCount > 0)
                        {
                            newlineCount--;
                        }

                        proc.ReadUntilWhitespace();
                        proc.SkipWhitespace();

                        var path = proc.ReadToEnd();

                        var gitStatusEntry = gitStatusEntryFactory.Create(path, GitFileStatus.Deleted);
                        changes.Add(gitStatusEntry);
                    }
                    else if (proc.Matches("commit"))
                    {
                        ReturnGitLogEntry();
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }

                    break;

                default:
                    throw new Exception("Unexpected phase:" + phase);
            }
        }

        private void PopNewlines()
        {
            while (newlineCount > 0)
            {
                descriptionLines.Add(string.Empty);
                newlineCount--;
            }
        }

        private void HandleUnexpected(string line)
        {
            throw new Exception(string.Format(@"Unexpected input in phase: {0}{1}""{2}""", phase, Environment.NewLine,
                line));
        }

        private void ReturnGitLogEntry()
        {
            Debug.Assert(time != null, "time != null");

            logger.Debug("ReturnGitLogEntry commitId:" + commitId);

            PopNewlines();

            var description = string.Join(Environment.NewLine, descriptionLines.ToArray());

            OnLogEntry.SafeInvoke(new GitLogEntry()
            {
                AuthorName = authorName,
                MergeA = mergeA,
                MergeB = mergeB,
                Changes = changes,
                AuthorEmail = authorEmail,
                Summary = summary,
                Description = description,
                CommitID = commitId,
                Time = time.Value
            });

            Reset();
        }
    }
}