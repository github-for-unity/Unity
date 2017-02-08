using System;

namespace GitHub.Api
{
    sealed class Credential : ICredential
    {
        public Credential(HostAddress host)
        {
            this.Host = host.CredentialCacheKeyHost;
        }

        public Credential(HostAddress host, string username, string token)
        {
            this.Host = host.CredentialCacheKeyHost;
            this.Username = username;
            this.Token = token;
        }

        public void UpdateToken(string token)
        {
            this.Token = token;
        }

        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Token { get; private set; }


        private bool disposed = false;
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    Token = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}