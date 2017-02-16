using System.Threading.Tasks;
using Octokit;

namespace GitHub.Unity
{
    class AppCredentialStore : ICredentialStore
    {
        private readonly string username;
        private readonly string token;

        public AppCredentialStore(UriString hostAddress, string username, string token)
        {
            this.username = username;
            this.token = token;
        }

        Task<Credentials> ICredentialStore.GetCredentials()
        {
            return TaskEx.FromResult(new Credentials(username, token));
        }
    }
}