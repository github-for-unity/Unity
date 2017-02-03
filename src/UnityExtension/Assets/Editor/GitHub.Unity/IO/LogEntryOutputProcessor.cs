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

        public LogEntryOutputProcessor(IGitStatusEntryFactory gitStatusEntryFactory)
        {
            this.gitStatusEntryFactory = gitStatusEntryFactory;
            Reset();
        }

        private void Reset()
        {
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
                    time = DateTimeOffset.Parse(line);
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
                    summary = line;
                    descriptionLines.Add(line);
                    phase++;
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
                    }
                    else
                    {
                        var substring = line.Substring(0, indexOf);
                        descriptionLines.Add(substring);
                        phase++;
                    }
                    break;

                case ProcessingPhase.Files:
                    if (line == string.Empty)
                    {
                        ReturnGitLogEntry();
                        return;
                    }

                    var proc = new LineParser(line);

                    string file = null;
                    GitFileStatus status;
                    string originalPath = null;

                    if (proc.Matches('M'))
                    {
                        status = GitFileStatus.Modified;
                    }
                    else if (proc.Matches('A'))
                    {
                        status = GitFileStatus.Added;
                    }
                    else if (proc.Matches('D'))
                    {
                        status = GitFileStatus.Deleted;
                    }
                    else if (proc.Matches('R'))
                    {
                        status = GitFileStatus.Renamed;
                    }
                    else
                    {
                        HandleUnexpected(line);
                        return;
                    }

                    switch (status)
                    {
                        case GitFileStatus.Modified:
                        case GitFileStatus.Added:
                        case GitFileStatus.Deleted:
                            proc.SkipWhitespace();

                            file = proc.Matches('"') 
                                ? proc.ReadUntil('"')
                                : proc.ReadToEnd();

                            break;
                        case GitFileStatus.Renamed:

                            proc.SkipWhitespace();

                            originalPath = 
                                proc.Matches('"') 
                                ? proc.ReadUntil('"') 
                                : proc.ReadUntilWhitespace();

                            proc.SkipWhitespace();

                            file = proc.Matches('"') 
                                ? proc.ReadUntil('"') 
                                : proc.ReadToEnd();

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    changes.Add(gitStatusEntryFactory.Create(file, status, originalPath));

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
            logger.Error(@"Unexpected input in phase: {0}{1}""{2}""", phase, Environment.NewLine, line);
            Reset();
        }

        private void ReturnGitLogEntry()
        {
            logger.Debug("ReturnGitLogEntry commitId:" + commitId);

            PopNewlines();

            var description = string.Join(Environment.NewLine, descriptionLines.ToArray());

            OnLogEntry.SafeInvoke(new GitLogEntry()
            {
                AuthorName = authorName,
                CommitName = committerName,
                MergeA = mergeA,
                MergeB = mergeB,
                Changes = changes,
                AuthorEmail = authorEmail,
                CommitEmail = committerEmail,
                Summary = summary,
                Description = description,
                CommitID = commitId,
                Time = time.Value,
                CommitTime = committerTime.Value
            });

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