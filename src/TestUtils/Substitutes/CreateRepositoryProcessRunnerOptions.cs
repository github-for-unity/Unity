using System.Collections.Generic;
using GitHub.Unity;

namespace TestUtils
{
    class CreateRepositoryProcessRunnerOptions
    {
        public Dictionary<GitConfigGetKey, string> GitConfigGetResults { get; set; }

        public IList<GitStatus> GitStatusResults { get; set; }

        public IList<IEnumerable<GitLock>> GitListLocksResults { get; set; }

        public CreateRepositoryProcessRunnerOptions(Dictionary<GitConfigGetKey, string> getConfigResults = null,
            IList<GitStatus> gitStatusResults = null,
            IList<IEnumerable<GitLock>> gitListLocksResults = null)
        {
            GitListLocksResults = gitListLocksResults;
            GitStatusResults = gitStatusResults;
            GitConfigGetResults = getConfigResults ?? new Dictionary<GitConfigGetKey, string>();
        }

        public struct GitConfigGetKey
        {
            public string Key;
            public GitConfigSource GitConfigSource;

            public GitConfigGetKey(string key, GitConfigSource gitConfigSource)
            {
                Key = key;
                GitConfigSource = gitConfigSource;
            }
        }
    }
}