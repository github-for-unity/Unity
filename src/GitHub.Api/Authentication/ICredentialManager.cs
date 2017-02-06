using System;
using System.Threading.Tasks;

namespace GitHub.Api
{
    public interface ICredential : IDisposable
    {
        string Host { get; }
        string Username { get; }
        string Token { get; }
        void UpdateToken(string token);
    }

    public interface ICredentialManager
    {
        Task<ICredential> Load(HostAddress host);
        Task Save(ICredential credential);
        Task Delete(HostAddress host);
    }
}