using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class LogEntryOutputProcessor : BaseOutputListProcessor<GitLogEntry>
    {
        private readonly IGitObjectFactory gitObjectFactory;
        private ProcessingPhase phase;
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
        private string committerName;
        private string committerEmail;
        private DateTimeOffset? committerTime;
        private bool seenBodyEnd = false;
        private Regex hashRegex = new Regex("[0-9a-fA-F]{40}");

        private StringBuilder sb;

        public LogEntryOutputProcessor(IGitObjectFactory gitObjectFactory)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitObjectFactory");
            this.gitObjectFactory = gitObjectFactory;
            Reset();
        }

        private void Reset()
        {
            sb = new StringBuilder();
            phase = ProcessingPhase.CommitHash;
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
            committerName = null;
            committerEmail = null;
            committerTime = null;
            seenBodyEnd = false;
        }

        public override void LineReceived(string line)
        {
            if (line == null)
            {
                ReturnGitLogEntry();
                return;
            }
            sb.AppendLine(line);

            if (phase == ProcessingPhase.Files && seenBodyEnd)
            {
                seenBodyEnd = false;
                var proc = new LineParser(line);
                if (proc.Matches(hashRegex))
                {
                    // there's no files on this commit, it's a new one!
                    ReturnGitLogEntry();
                }
            }

            switch (phase)
            {
                case ProcessingPhase.CommitHash:
                    commitId = line;
                    phase++;
                    break;

                case ProcessingPhase.ParentHash:
                    var parentHashes = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    if (parentHashes.Length > 1)
                    {
                        mergeA = parentHashes[0];
                        mergeB = parentHashes[1];
                    }

                    phase++;
                    break;

                case ProcessingPhase.AuthorName:
                    authorName = line;
                    phase++;
                    break;

                case ProcessingPhase.AuthorEmail:
                    authorEmail = line;
                    phase++;
                    break;

                case ProcessingPhase.AuthorDate:
                    DateTimeOffset t;
                    if (DateTimeOffset.TryParse(line, out t))
                    {
                        time = t;
                    }
                    else
                    {
                        Logger.Error("ERROR {0}", sb.ToString());
                        throw new FormatException("Invalid line");
                    }
                    phase++;
                    break;

                case ProcessingPhase.CommitterName:
                    committerName = line;
                    phase++;
                    break;

                case ProcessingPhase.CommitterEmail:
                    committerEmail = line;
                    phase++;
                    break;

                case ProcessingPhase.CommitterDate:
                    committerTime = DateTimeOffset.Parse(line);
                    phase++;
                    break;

                case ProcessingPhase.Summary:
                    {
                        var idx = line.IndexOf("---GHUBODYEND---", StringComparison.Ordinal);
                        var oneliner = idx >= 0;
                        if (oneliner)
                        {
                            line = line.Substring(0, idx);
                        }

                        summary = line;
                        phase++;
                        // there's no description so skip it
                        if (oneliner)
                        {
                            phase++;
                            seenBodyEnd = true;
                        }
                    }
                    break;

                case ProcessingPhase.Description:
                    var indexOf = line.IndexOf("---GHUBODYEND---", StringComparison.InvariantCulture);
                    if (indexOf == -1)
                    {
                        descriptionLines.Add(line);
                    }
                    else if (indexOf == 0)
                    {
                        phase++;
                        seenBodyEnd = true;
                    }
                    else
                    {
                        var substring = line.Substring(0, indexOf);
                        descriptionLines.Add(substring);
                        phase++;
                        seenBodyEnd = true;
                    }
                    break;

                case ProcessingPhase.Files:
                    if (string.IsNullOrEmpty(line))
                    {
                        ReturnGitLogEntry();
                        return;
                    }

                    if (line.IndexOf("---GHUBODYEND---", StringComparison.InvariantCulture) >= 0)
                    {
                        seenBodyEnd = true;
                        return;
                    }

                    var proc = new LineParser(line);

                    string file = null;
                    GitFileStatus status = GitFileStatus.None;
                    string originalPath = null;

                    if (proc.IsAtEnd)
                    {
                        // there's no files on this commit, it's a new one!
                        ReturnGitLogEntry();
                        return;
                    }
                    else
                    {
                        status = GitStatusEntry.ParseStatusMarker(proc.ReadChar());
                    }

                    if (status == GitFileStatus.None)
                    {
                        HandleUnexpected(line);
                        return;
                    }

                    proc.ReadUntilWhitespace();
                    if (status == GitFileStatus.Copied || status == GitFileStatus.Renamed)
                    {
                        var files =
                            proc.ReadToEnd().Trim()
                                .Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Select(s => s.Trim('"'))
                                .ToArray();

                        originalPath = files[0];
                        file = files[1];
                    }
                    else
                    {
                        file = proc.ReadToEnd().Trim().Trim('"');
                    }

                    changes.Add(gitObjectFactory.CreateGitStatusEntry(file, status, GitFileStatus.None, originalPath));

                    break;

                default:
                    HandleUnexpected(line);
                    break;
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
            Logger.Error("Unexpected Input:\"{0}\" Phase:{1}", line, phase);
            Reset();
        }

        private void ReturnGitLogEntry()
        {
            PopNewlines();

            var filteredDescriptionLines = (descriptionLines.Any() && string.IsNullOrEmpty(descriptionLines.First()) ? descriptionLines.Skip(1) : descriptionLines).ToArray();
            var description = string.Join(Environment.NewLine, filteredDescriptionLines);

            if (time.HasValue)
            {
                var gitLogEntry = new GitLogEntry(commitId, 
                    authorName, authorEmail, 
                    committerName, committerEmail, 
                    summary,
                    description,
                    time.Value,  committerTime.Value,
                    changes,
                    mergeA, mergeB);

                RaiseOnEntry(gitLogEntry);
            }

            Reset();
        }

        private enum ProcessingPhase
        {
            CommitHash = 0,
            ParentHash = 1,
            AuthorName = 2,
            AuthorEmail = 3,
            AuthorDate = 4,
            CommitterName = 5,
            CommitterEmail = 6,
            CommitterDate = 7,
            Summary = 8,
            Description = 9,
            Files = 10,
        }
    }
}
