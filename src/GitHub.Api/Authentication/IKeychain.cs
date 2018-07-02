using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    public interface IKeychain
    {
        IKeychainAdapter Connect(UriString host);
        IKeychainAdapter Load(UriString host);
        void Clear(UriString host, bool deleteFromCredentialManager);
        void Save(UriString host);
        void SetCredentials(ICredential credential);
        void Initialize();
        Connection[] Connections { get; }
        IList<UriString> Hosts { get; }
        bool HasKeys { get; }
        void SetToken(UriString host, string token, string username);

        event Action ConnectionsChanged;
    }
}
