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
                    var gitStatusMarker = proc.Read(2);
                    if (gitStatusMarker == null)
                    {
                        HandleUnexpected(line);
                        return;
                    }


                    /*
                     X          Y     Meaning
                    -------------------------------------------------
	                         [AMD]   not updated
                    M        [ MD]   updated in index
                    A        [ MD]   added to index
                    D                deleted from index
                    R        [ MD]   renamed in index
                    C        [ MD]   copied in index
                    [MARC]           index and work tree matches
                    [ MARC]     M    work tree changed since index
                    [ MARC]     D    deleted in work tree
                    [ D]        R    renamed in work tree
                    [ D]        C    copied in work tree
                    -------------------------------------------------
                    D           D    unmerged, both deleted
                    A           A    unmerged, both added
                    A           U    unmerged, added by us
                    D           U    unmerged, deleted by us
                    U           A    unmerged, added by them
                    U           D    unmerged, deleted by them
                    U           U    unmerged, both modified
                    -------------------------------------------------
                    ?           ?    untracked
                    !           !    ignored
                    -------------------------------------------------
                     */

                    string originalPath = null;
                    string path = null;

                    var indexStatusMarker = gitStatusMarker[0];
                    var workTreeStatusMarker = gitStatusMarker[1];

                    GitFileStatus indexStatus = GitStatusEntry.ParseStatusMarker(indexStatusMarker);
                    GitFileStatus workTreeStatus = GitStatusEntry.ParseStatusMarker(workTreeStatusMarker);
                    GitFileStatus status = workTreeStatus != GitFileStatus.None ? workTreeStatus : indexStatus;

                    if (status == GitFileStatus.None)
                    {
                        HandleUnexpected(line);
                        return;
                    }

                    if (status == GitFileStatus.Copied || status == GitFileStatus.Renamed)
                    {
                        var files =
                            proc.ReadToEnd()
                                .Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Select(s => s.Trim('"'))
                                .ToArray();

                        originalPath = files[0];
                        path = files[1];
                    }
                    else
                    {
                        path = proc.ReadToEnd().Trim().Trim('"');
                    }

                    var gitStatusEntry = gitObjectFactory.CreateGitStatusEntry(path, indexStatus, workTreeStatus, originalPath);
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

        public class StatusOutputPathComparer : IComparer<string>
        {
            public static StatusOutputPathComparer Instance => new StatusOutputPathComparer();

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
