namespace GitHub.Api
{
    sealed class Credential : ICredential
    {
        public Credential(string host)
        {
            this.Host = host;
        }

        public ICredential Set(string key, string value)
        {
            Key = key;
            Value = value;
            return this;
        }

        public string Host { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }


        private bool disposed = false;
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    Value = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}