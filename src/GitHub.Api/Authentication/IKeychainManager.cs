using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IKeychainItem : IDisposable
    {
        UriString Host { get; }
        string Username { get; }
        string Token { get; }
        void UpdateToken(string token);
    }

    interface IKeychainManager
    {
        Task<IKeychainItem> Load(UriString host);
        Task Save(IKeychainItem keychainItem);
        Task Delete(UriString host);
        bool HasCredentials();
        IKeychainItem CachedKeys { get; }
    }
}