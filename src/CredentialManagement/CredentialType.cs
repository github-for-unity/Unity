using System.Diagnostics.CodeAnalysis;

namespace GitHub.Authentication.CredentialManagement
{
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32",
        Justification = "This is a uint as required by the unmanaged API")]
    public enum CredentialType : uint
    {
        None = 0,
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4
    }
}
