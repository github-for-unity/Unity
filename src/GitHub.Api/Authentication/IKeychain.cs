﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IKeychain
    {
        IKeychainAdapter Connect(UriString host);
        Task<IKeychainAdapter> Load(UriString host);
        Task Clear(UriString host, bool deleteFromCredentialManager);
        Task Save(UriString host);
        void SetCredentials(ICredential credential);
        void Initialize();
        Connection[] Connections { get; }
        IList<UriString> Hosts { get; }
        bool HasKeys { get; }
        void SetToken(UriString host, string token, string username);

        event Action ConnectionsChanged;
    }
}