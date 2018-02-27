using System.Threading.Tasks;
using Octokit;

namespace OctoRun
{
    interface IKeychain
    {
        string Login { get; set; }
        string Token { get; set; }
        string Code { get; set; }
    }


    class CredentialStore : ICredentialStore, IKeychain
    {
        public string Login { get; set; }
        public string Token { get; set; }
        public string Code { get; set; }

        public Task<Credentials> GetCredentials()
        {
            return Task.FromResult(new Credentials(Login, Token));
        }
    }
}