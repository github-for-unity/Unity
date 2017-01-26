using System;

namespace GitHub.Api
{
    public interface ICredential : IDisposable
    {
        string Host { get; }
        string Key { get; }
        string Value { get; }
    }

    public interface ICredentialManager
    {
        ICredential Load(string key);
        void Save(ICredential credential);
        void Delete(ICredential credential);
    }
}