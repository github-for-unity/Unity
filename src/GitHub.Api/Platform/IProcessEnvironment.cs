using System.Diagnostics;

namespace GitHub.Unity
{
    public interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo psi, NPath workingDirectory);

        NPath FindRoot(NPath path);
    }
}