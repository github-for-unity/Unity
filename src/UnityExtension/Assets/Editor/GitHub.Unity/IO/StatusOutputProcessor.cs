using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class StatusOutputProcessor : BaseOutputProcessor
    {
        private static readonly Regex branchTrackedAndDelta = new Regex(@"(.*)\.\.\.(.*)\s\[(.*)\]",
            RegexOptions.Compiled);

        private readonly IGitStatusEntryFactory gitStatusEntryFactory;
        private int ahead;
        private int behind;
        private List<GitStatusEntry> entries;
        private string localBranch;
        private string remoteBranch;

        public StatusOutputProcessor(IGitStatusEntryFactory gitStatusEntryFactory)
        {
            this.gitStatusEntryFactory = gitStatusEntryFactory;
            Reset();
        }

        public override void LineReceived(string line)
        {
            base.LineReceived(line);

            if (OnStatus == null)
            {
                return;
            }

            if (line == null)
            {
                ReturnStatus();
            }
            else
            {
                var proc = new LineParser(line);
                if (localBranch == null)
                {
                    if (proc.Matches("##"))
                    {
                        proc.MoveToAfter('#');
                        proc.SkipWhitespace();

                        string branchesString;
                        if (proc.Matches(branchTrackedAndDelta))
                        {
                            //master...origin/master [ahead 1]
                            //master...origin/master [behind 1]
                            //master...origin/master [ahead 1, behind 1]

                            branchesString = proc.ReadUntilWhitespace();
                            proc.MoveToAfter('[');

                            var deltaString = proc.ReadUntil(']');
                            var deltas = deltaString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var delta in deltas)
                            {
                                var deltaComponents = delta.Split(' ');
                                if (deltaComponents[0] == "ahead")
                                {
                                    ahead = Int32.Parse(deltaComponents[1]);
                                }
                                else if (deltaComponents[0] == "behind")
                                {
                                    behind = Int32.Parse(deltaComponents[1]);
                                }
                                else
                                {
                                    throw new Exception("Unexpected deltaComponent in o");
                                }
                            }
                        }
                        else
                        {
                            branchesString = proc.ReadToEnd();
                        }

                        var branches = branchesString.Split(new[] { "..." }, StringSplitOptions.RemoveEmptyEntries);
                        localBranch = branches[0];
                        if (branches.Length == 2)
                        {
                            remoteBranch = branches[1];
                        }
                    }
                    else
                    {
                        HandleUnexpected(line);
                    }
                }
                else
                {
                    // M GitHubVS.sln
                    //R  README.md -> README2.md
                    // D deploy.cmd
                    //A  something added.txt
                    //?? something.txt

                    string originalPath = null;
                    string path = null;
                    var status = GitFileStatus.Added;
                    var staged = false;

                    if (proc.Matches('?'))
                    {
                        //?? something.txt
                        proc.MoveToAfter('?');
                        proc.SkipWhitespace();

                        path = proc.ReadToEnd().Trim('"');
                        status = GitFileStatus.Untracked;
                    }
                    else
                    {
                        if (proc.IsAtWhitespace)
                        {
                            proc.SkipWhitespace();
                        }
                        else
                        {
                            staged = true;
                        }

                        if (proc.Matches('M'))
                        {
                            //M  GitHubVS.sln
                            proc.MoveNext();
                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                            status = GitFileStatus.Modified;
                        }
                        else if (proc.Matches('D'))
                        {
                            //D  deploy.cmd
                            proc.MoveNext();
                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                            status = GitFileStatus.Deleted;
                        }
                        else if (proc.Matches('R'))
                        {
                            //R  README.md -> README2.md
                            proc.MoveNext();
                            proc.SkipWhitespace();

                            var files =
                                proc.ReadToEnd()
                                    .Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Select(s => s.Trim('"'))
                                    .ToArray();

                            originalPath = files[0];
                            path = files[1];
                            status = GitFileStatus.Renamed;
                        }
                        else if (proc.Matches('A'))
                        {
                            //A  something added.txt
                            proc.MoveNext();
                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                            status = GitFileStatus.Added;
                        }
                        else
                        {
                            HandleUnexpected(line);
                        }
                    }

                    var gitStatusEntry = gitStatusEntryFactory.Create(path, status, originalPath, staged);
                    entries.Add(gitStatusEntry);
                }
            }
        }

        private void ReturnStatus()
        {
            var gitStatus = new GitStatus {
                LocalBranch = localBranch,
                RemoteBranch = remoteBranch,
                Ahead = ahead,
                Behind = behind
            };

            if (entries.Any())
            {
                gitStatus.Entries = entries;
            }

            OnStatus(gitStatus);

            Reset();
        }

        private void Reset()
        {
            localBranch = null;
            remoteBranch = null;
            ahead = 0;
            behind = 0;
            entries = new List<GitStatusEntry>();
        }

        private void HandleUnexpected(string line)
        {
            throw new Exception(string.Format(@"Unexpected input{0}""{1}""", Environment.NewLine, line));
        }

        public event Action<GitStatus> OnStatus;
    }
}
