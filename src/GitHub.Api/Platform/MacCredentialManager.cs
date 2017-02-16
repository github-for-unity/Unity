using System.Threading.Tasks;

namespace GitHub.Api
{
    class MacCredentialManager : ICredentialManager
    {
        private ICredential credential;

        public Task Delete(UriString host)
        {
            // TODO: implement credential deletion
            credential = null;
            return TaskEx.FromResult(true);
        }

        public Task<ICredential> Load(UriString host)
        {
            // TODO: implement credential loading
            return TaskEx.FromResult<ICredential>(new Credential(host, credential.Username, credential.Token));
        }

        public Task Save(ICredential credential)
        {
            // TODO: implement credential saving
            this.credential = credential;
            return TaskEx.FromResult(true);
        }
    }
}