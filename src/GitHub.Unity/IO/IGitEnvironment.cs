using System.Diagnostics;

namespace GitHub.Unity
{
    public interface IGitEnvironment
    {
        void Configure(ProcessStartInfo psi, string workingDirectory);
        IEnvironment Environment { get; }
    }
}