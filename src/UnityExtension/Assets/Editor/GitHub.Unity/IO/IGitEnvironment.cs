using System.Diagnostics;

namespace GitHub.Unity
{
    interface IGitEnvironment
    {
        void Configure(ProcessStartInfo psi, string workingDirectory);
        IEnvironment Environment { get; }
    }
}