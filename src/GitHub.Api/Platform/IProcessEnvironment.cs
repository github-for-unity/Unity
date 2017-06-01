using System.Diagnostics;

namespace GitHub.Unity
{
    interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo psi, NPath workingDirectory);

        NPath FindRoot(NPath path);
    }
}