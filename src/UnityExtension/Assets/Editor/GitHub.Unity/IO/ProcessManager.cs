using GitHub.Unity;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class ProcessManager : IProcessManager
    {
        private static readonly ILogging logger = Logging.GetLogger<ProcessManager>();

        private readonly IEnvironment environment;
        private readonly IGitEnvironment gitEnvironment;
        private readonly CancellationToken cancellationToken;

        public ProcessManager(IEnvironment environment, IGitEnvironment gitEnvironment)
            : this(environment, gitEnvironment, CancellationToken.None)
        {
        }

        public ProcessManager(IEnvironment environment, IGitEnvironment gitEnvironment, CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.gitEnvironment = gitEnvironment;
            this.cancellationToken = cancellationToken;
        }

        public IProcess Configure(string executableFileName, string arguments, string workingDirectory)
        {
            logger.Debug("Configuring process - \"" + executableFileName + " " + arguments + "\" cwd:" + workingDirectory);
            var startInfo = new ProcessStartInfo(executableFileName, arguments)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            gitEnvironment.Configure(startInfo, workingDirectory);
            if (executableFileName.ToNPath().IsRelative)
                executableFileName = FindExecutableInPath(executableFileName, startInfo.EnvironmentVariables["PATH"]) ?? executableFileName;
            startInfo.FileName = executableFileName;
            return new ProcessWrapper(startInfo);
        }

        public IProcess Reconnect(int pid)
        {
            logger.Debug("Reconnecting process " + pid);
            var p = Process.GetProcessById(pid);
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            return new ProcessWrapper(p.StartInfo);
        }

        private string FindExecutableInPath(string executable, string path = null)
        {
            Guard.ArgumentNotNullOrWhiteSpace(executable, "executable");

            if (executable.ToNPath().IsRelative) return executable;

            path = path ?? environment.GetEnvironmentVariable("PATH");
            var executablePath = path.Split(Path.PathSeparator)
                .Select(directory =>
                {
                    try
                    {
                        var unquoted = directory.RemoveSurroundingQuotes();
                        var expanded = environment.ExpandEnvironmentVariables(unquoted);
                        logger.Debug("expanded:'{0}' executable:'{1}'", expanded, executable);
                        return expanded.ToNPath().Combine(executable);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error while looking for {0} in {1}\n{2}", executable, directory, e);
                        return null;
                    }
                })
                .Where(x => x != null)
                .FirstOrDefault(x => x.FileExists());

            return executablePath;
        }

        public CancellationToken CancellationToken { get { return cancellationToken; } }
    }
}
