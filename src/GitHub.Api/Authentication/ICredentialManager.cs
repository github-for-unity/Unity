using System;
using System.Threading.Tasks;

namespace GitHub.Api
{
    interface ICredential : IDisposable
    {
        HostAddress Host { get; }
        string Username { get; }
        string Token { get; }
        void UpdateToken(string token);
    }

    interface ICredentialManager
    {
        Task<ICredential> Load(HostAddress host);
        Task Save(ICredential credential);
        Task Delete(HostAddress host);
    }
}