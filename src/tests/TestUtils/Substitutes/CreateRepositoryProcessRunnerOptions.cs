using System.Collections.Generic;
using GitHub.Unity;

namespace TestUtils
{
    class CreateRepositoryProcessRunnerOptions
    {
        public Dictionary<GitConfigGetKey, string> GitConfigGetResults { get; set; }

        public GitStatus GitStatusResults { get; set; }

        public List<GitLock> GitListLocksResults { get; set; }

        public CreateRepositoryProcessRunnerOptions(Dictionary<GitConfigGetKey, string> getConfigResults = null,
            GitStatus gitStatusResults = new GitStatus(),
            List<GitLock> gitListLocksResults = null)
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