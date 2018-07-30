using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    public interface IKeychain
    {
        IKeychainAdapter Connect(UriString host);
        IKeychainAdapter LoadFromSystem(UriString host);
        void Clear(UriString host, bool deleteFromCredentialManager);
        void SaveToSystem(UriString host);
        void Initialize();
        Connection[] Connections { get; }
        IList<UriString> Hosts { get; }
        bool HasKeys { get; }

        event Action ConnectionsChanged;
    }
}
