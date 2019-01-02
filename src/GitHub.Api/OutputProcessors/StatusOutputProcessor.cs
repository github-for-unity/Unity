using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class GitStatusOutputProcessor : BaseOutputProcessor<GitStatus>
    {
        private static readonly Regex branchTrackedAndDelta = new Regex(@"(.*)\.\.\.(.*)\s\[(.*)\]",
            RegexOptions.Compiled);

        private readonly IGitObjectFactory gitObjectFactory;
        GitStatus gitStatus;
        
        public GitStatusOutputProcessor(IGitObjectFactory gitObjectFactory)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitObjectFactory");
            this.gitObjectFactory = gitObjectFactory;
        }

        public override void LineReceived(string line)
        {
            if (line == null)
            {
                ReturnStatus();
            }
            else
            {
                Prepare();

                var proc = new LineParser(line);
                if (gitStatus.LocalBranch == null)
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
                                    gitStatus.Ahead = Int32.Parse(deltaComponents[1]);
                                }
                                else if (deltaComponents[0] == "behind")
                                {
                                    gitStatus.Behind = Int32.Parse(deltaComponents[1]);
                                }
                                else if (deltaComponents[0] == "gone")
                                {
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected deltaComponent in o");
                                }
                            }
                        }
                        else
                        {
                            branchesString = proc.ReadToEnd();
                        }

                        var branches = branchesString.Split(new[] { "..." }, StringSplitOptions.RemoveEmptyEntries);
                        gitStatus.LocalBranch = branches[0];
                        if (branches.Length == 2)
                        {
                            gitStatus.RemoteBranch = branches[1];
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
                    else if (proc.Matches('!'))
                    {
                        //?? something.txt
                        proc.MoveToAfter('!');
                        proc.SkipWhitespace();

                        path = proc.ReadToEnd().Trim('"');
                        status = GitFileStatus.Ignored;
                    }
                    else
                    {
                        var secondPosition = false;
                        if (proc.IsAtWhitespace)
                        {
                            proc.SkipWhitespace();
                            secondPosition = true;
                        }

                        if (proc.Matches('M'))
                        {
                            //M  GitHubVS.sln
                            proc.MoveNext();
                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                            status = GitFileStatus.Modified;
                            staged = !secondPosition;
                        }
                        else if (proc.Matches('D'))
                        {
                            proc.MoveNext();

                            if (proc.Matches('D') || proc.Matches('U'))
                            {
                                //DD  deploy.cmd - unmerged, both deleted
                                //DU  deploy.cmd - unmerged, deleted by us

                                status = GitFileStatus.Unmerged;
                            }
                            else if(proc.IsAtWhitespace)
                            {
                                //D  deploy.cmd
                                // D deploy.cmd

                                status = GitFileStatus.Deleted;
                                staged = !secondPosition;
                            }
                            else
                            {
                                HandleUnexpected(line);
                                return;
                            }

                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                        }
                        else if (proc.Matches('R'))
                        {
                            //R  README.md -> README2.md
                            // R README.md -> README2.md

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
                            staged = !secondPosition;
                        }
                        else if (proc.Matches('A'))
                        {
                            proc.MoveNext();
                            if (proc.Matches('A') || proc.Matches('U'))
                            {
                                //AA  deploy.cmd - unmerged, both added
                                //AU  deploy.cmd - unmerged, added by us

                                status = GitFileStatus.Unmerged;
                            }
                            else if (proc.IsAtWhitespace)
                            {
                                //A  something added.txt
                                // A something added.txt

                                status = GitFileStatus.Added;
                                staged = !secondPosition;
                            }
                            else
                            {
                                HandleUnexpected(line);
                                return;
                            }

                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                        }
                        else if (proc.Matches('U'))
                        {
                            proc.MoveNext();
                            if (proc.Matches('D') || proc.Matches('A') || proc.Matches('U'))
                            {
                                //UD  deploy.cmd - unmerged, deleted by them
                                //UA  deploy.cmd - unmerged, added by them
                                //UU  deploy.cmd - unmerged, both modified

                                status = GitFileStatus.Unmerged;
                            }
                            else
                            {
                                HandleUnexpected(line);
                                return;
                            }

                            proc.SkipWhitespace();

                            path = proc.ReadToEnd().Trim('"');
                        }
                        else
                        {
                            HandleUnexpected(line);
                            return;
                        }
                    }

                    var gitStatusEntry = gitObjectFactory.CreateGitStatusEntry(path, status, originalPath, staged);
                    gitStatus.Entries.Add(gitStatusEntry);
                }
            }
        }

        private void ReturnStatus()
        {
            if (gitStatus.Entries == null)
                return;

            gitStatus.Entries = gitStatus.Entries
                                         .OrderBy(entry => entry.Path, StatusOutputPathComparer.Instance)
                                         .ToList();

            RaiseOnEntry(gitStatus);

            gitStatus = new GitStatus();
        }

        private void Prepare()
        {
            if (gitStatus.Entries == null)
            {
                gitStatus = new GitStatus
                {
                    Entries = new List<GitStatusEntry>()
                };
            }
        }

        private void HandleUnexpected(string line)
        {
            Logger.Error("Unexpected Input:\"{0}\"", line);
        }

        class StatusOutputPathComparer : IComparer<string>
        {
            internal static StatusOutputPathComparer Instance => new StatusOutputPathComparer();

            public int Compare(string x, string y)
            {
                Guard.ArgumentNotNull(x, nameof(x));
                Guard.ArgumentNotNull(y, nameof(y));

                var meta = ".meta";
                var xHasMeta = x.EndsWith(meta);
                var yHasMeta = y.EndsWith(meta);

                if(!xHasMeta && !yHasMeta) return StringComparer.InvariantCulture.Compare(x, y);

                var xPure = xHasMeta ? x.Substring(0, x.Length - meta.Length) : x;
                var yPure = yHasMeta ? y.Substring(0, y.Length - meta.Length) : y;

                if (xHasMeta)
                {
                    return xPure.Equals(y) ? 1 : StringComparer.InvariantCulture.Compare(xPure, yPure);
                }

                return yPure.Equals(x) ? -1 : StringComparer.InvariantCulture.Compare(xPure, yPure);
            }
        }
    }
}
