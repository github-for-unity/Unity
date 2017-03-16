using System.Collections.Generic;
using GitHub.Unity;

namespace TestUtils
{
    class CreateRepositoryProcessRunnerOptions
    {
        public Dictionary<GitConfigGetKey, string> GitConfigGetResults { get; set; }

        public CreateRepositoryProcessRunnerOptions(Dictionary<GitConfigGetKey, string> getConfigResults = null)
        {
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