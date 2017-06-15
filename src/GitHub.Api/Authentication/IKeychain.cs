using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IKeychain
    {
        IKeychainAdapter Connect(UriString host);
        Task<IKeychainAdapter> Load(UriString host);
        Task Clear(UriString host, bool deleteFromCredentialManager);
        Task Save(UriString host);
        void UpdateToken(UriString host, string token);
        void SetCredentials(ICredential credential);
        void Initialize();
        Connection[] Connections { get; }
        IList<UriString> ConnectionKeys { get; }
        bool HasKeys { get; }
        void SetToken(UriString host, string token);
    }
}