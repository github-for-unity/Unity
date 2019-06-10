namespace GitHub.Unity
{
    public class KeychainAdapter : IKeychainAdapter
    {
        public ICredential Credential { get; private set; }

        public void Set(ICredential credential)
        {
            Credential = credential;
        }

        public void Update(string token, string username)
        {
            Credential.Update(token, username);
        }

        public void Clear()
        {
            Credential = null;
        }
    }

    public interface IKeychainAdapter
    {
        ICredential Credential { get; }
        void Set(ICredential credential);
        void Update(string token, string username);
        void Clear();
    }
}
