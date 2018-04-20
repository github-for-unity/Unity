using System;

namespace GitHub.Unity
{
    sealed class Credential : ICredential
    {
        public Credential(UriString host)
        {
            this.Host = host;
        }

        public Credential(UriString host, string username, string token)
        {
            this.Host = host;
            this.Username = username;
            this.Token = token;
        }

        public void UpdateToken(string token, string username)
        {
            this.Token = token;
            this.Username = username;
        }

        public UriString Host { get; private set; }
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