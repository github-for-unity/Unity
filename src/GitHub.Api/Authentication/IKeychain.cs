using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IKeychain
    {
        KeychainAdapter Connect(UriString host);
        Task<KeychainAdapter> Load(UriString host);
        void Clear(UriString host);
        void Clear();
        Task Flush(UriString host);
        void UpdateToken(UriString host, string token);
        void Save(ICredential credential);
        void Initialize();
        bool HasKeys { get; }
    }
}