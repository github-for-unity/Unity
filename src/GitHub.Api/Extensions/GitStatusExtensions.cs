using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    static class GitStatusExtensions
    {
        public static IEnumerable<GitStatusEntry> GetEntriesExcludingIgnoredAndUntracked(this GitStatus gitStatus)
        {
            return gitStatus.Entries.Where(entry => entry.Status != GitFileStatus.Ignored && !entry.Staged);
        }
    }
}