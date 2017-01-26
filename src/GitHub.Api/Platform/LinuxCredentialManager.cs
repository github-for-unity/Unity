namespace GitHub.Api
{
    class LinuxCredentialManager : ICredentialManager
    {
        public void Delete(ICredential credential)
        {
            // TODO: implement credential deletion on windows
        }

        public ICredential Load(string key)
        {
            // TODO: implement credential loading on windows
            return new Credential(key).Set("", "");
        }

        public void Save(ICredential credential)
        {
            // TODO: implement credential saving on windows
        }
    }
}