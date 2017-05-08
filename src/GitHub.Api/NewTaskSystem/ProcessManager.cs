using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class ProcessManager
    {
        public CancellationToken Token { get; }

        public ProcessManager(CancellationToken token)
        {
            this.Token = token;
        }

        public ProcessStartInfo Configure(string executable, string arguments, bool withInput = false)
        {
            var psi = new ProcessStartInfo(executable, arguments)
            {
                CreateNoWindow = true,
                RedirectStandardInput = withInput,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false
            };
            return psi;
        }
    }
}