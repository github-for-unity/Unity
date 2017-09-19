using System.Threading.Tasks;
using Octokit;

namespace GitHub.Unity
{
    class KeychainAdapter : IKeychainAdapter
    {
        public Credentials OctokitCredentials { get; private set; } = Credentials.Anonymous;
        public ICredential Credential { get; private set; }

        public void Set(ICredential credential)
        {
            Credential = credential;
            OctokitCredentials = new Credentials(credential.Username, credential.Token);
        }

        public void UpdateToken(string token)
        {
            Credential.UpdateToken(token);
            OctokitCredentials = new Credentials(OctokitCredentials.Login, token);
        }

        public void Clear()
        {
            OctokitCredentials = Credentials.Anonymous;
            Credential = null;
        }

        /// <summary>
        /// Implementation for Octokit
        /// </summary>
        /// <returns>Octokit credentials</returns>
        Task<Credentials> ICredentialStore.GetCredentials()
        {
            return TaskEx.FromResult(OctokitCredentials);
        }
    }

    public interface IKeychainAdapter: ICredentialStore
    {
        Credentials OctokitCredentials { get; }
        ICredential Credential { get; }
    }
}
