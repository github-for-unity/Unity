using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IKeychain
    {
        KeychainAdapter Connect(UriString host);
        Task<KeychainAdapter> Load(UriString host);
        Task Clear(UriString host);
        Task Flush(UriString host);
        void UpdateToken(UriString host, string token);
        void Save(ICredential credential);
        void Initialize();
        bool HasKeys { get; }
    }
}