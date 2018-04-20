namespace GitHub.Unity
{
    class KeychainAdapter : IKeychainAdapter
    {
        public ICredential Credential { get; private set; }

        public void Set(ICredential credential)
        {
            Credential = credential;
        }

        public void UpdateToken(string token, string username)
        {
            Credential.UpdateToken(token, username);
        }

        public void Clear()
        {
            Credential = null;
        }
    }

    public interface IKeychainAdapter
    {
        ICredential Credential { get; }
    }
}
