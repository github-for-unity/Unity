namespace GitHub.Api
{
    class MacCredentialManager : ICredentialManager
    {
        private ICredential credential;

        public void Delete(ICredential credential)
        {
            // TODO: implement credential deletion
            credential = null;
        }

        public ICredential Load(string key)
        {
            // TODO: implement credential loading
            return new Credential(key).Set(credential.Key, credential.Value);
        }

        public void Save(ICredential credential)
        {
            // TODO: implement credential saving
            this.credential = credential;
        }
    }
}