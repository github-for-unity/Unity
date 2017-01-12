using System;

namespace GitHub.Unity
{
    struct GitStatusEntry
    {
        private const string UnknownStatusKeyError = "Unknown file status key: '{0}'";

        // NOTE: Has to stay in sync with GitFileStatus enum for FileStatusFromKey to function as intended
        private static readonly string[] GitFileStatusKeys = { "??", "M", "A", "D", "R", "C" };

        public static bool TryParse(string line, out GitStatusEntry entry)
        {
            var match = Utility.StatusStartRegex.Match(line);
            string statusKey = match.Groups["status"].Value, path = match.Groups["path"].Value;

            if (!string.IsNullOrEmpty(statusKey) && !string.IsNullOrEmpty(path))
            {
                var status = FileStatusFromKey(statusKey);
                var renameIndex = line.IndexOf(Utility.StatusRenameDivider);

                if (renameIndex >= 0)
                {
                    match = Utility.StatusEndRegex.Match(line.Substring(renameIndex));
                    entry = new GitStatusEntry(match.Groups["path"].Value, status, path.Substring(0, path.Length - 1));
                }
                else
                {
                    entry = new GitStatusEntry(path, status);
                }

                return true;
            }

            entry = new GitStatusEntry();

            return false;
        }

        private static GitFileStatus FileStatusFromKey(string key)
        {
            for (var index = 0; index < GitFileStatusKeys.Length; ++index)
            {
                if (key.Equals(GitFileStatusKeys[index]))
                {
                    return (GitFileStatus)index;
                }
            }

            throw new ArgumentException(String.Format(UnknownStatusKeyError, key));
        }

        public readonly string Path, FullPath, ProjectPath, OriginalPath;
        public readonly GitFileStatus Status;

        public GitStatusEntry(string path, GitFileStatus status, string originalPath = "")
        {
            Path = path;
            FullPath = Utility.RepositoryPathToAbsolute(Path);
            ProjectPath = Utility.RepositoryPathToAsset(Path);
            Status = status;
            OriginalPath = originalPath;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("'{0}': {1}", Path, Status);
        }
    }
}