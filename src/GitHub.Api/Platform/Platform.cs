using System;
using GitHub.Api;

namespace GitHub.Api
{
    public class Platform : IPlatform
    {
        public Platform(IEnvironment environment)
        {
            if (environment.IsWindows)
            {
                CredentialManager =  new WindowsCredentialManager();
            }
            else if (environment.IsMac)
            {
                CredentialManager = new MacCredentialManager();
            }
            else
            {
                CredentialManager = new LinuxCredentialManager();
            }
}

        public ICredentialManager CredentialManager { get; private set; }
    }
}