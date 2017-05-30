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
        private readonly IProcessEnvironment gitEnvironment;
        private readonly CancellationToken cancellationToken;

        public ProcessManager(IEnvironment environment, IProcessEnvironment gitEnvironment)
            : this(environment, gitEnvironment, CancellationToken.None)
        {
        }

        public ProcessManager(IEnvironment environment, IProcessEnvironment gitEnvironment, CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.gitEnvironment = gitEnvironment;
            this.cancellationToken = cancellationToken;
        }

        public T Configure<T>(T processTask, bool withInput = false) where T : IProcess
        {
            return Configure(processTask,
                processTask.ProcessName != null ? processTask.ProcessName : environment.GitExecutablePath,
                processTask.ProcessArguments,
                environment.RepositoryPath, withInput);
        }

        public T Configure<T>(T processTask, string executableFileName, string arguments, string workingDirectory = null, bool withInput = false)
             where T : IProcess
        {
            Guard.ArgumentNotNull(executableFileName, nameof(executableFileName));

            //logger.Trace("Configuring process - \"" + executableFileName + " " + arguments + "\" cwd:" + workingDirectory);
            var startInfo = new ProcessStartInfo
            {
                RedirectStandardInput = withInput,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            gitEnvironment.Configure(startInfo, workingDirectory ?? environment.RepositoryPath);
            if (executableFileName.ToNPath().IsRelative)
                executableFileName = FindExecutableInPath(executableFileName, startInfo.EnvironmentVariables["PATH"]) ?? executableFileName;
            startInfo.FileName = executableFileName;
            startInfo.Arguments = arguments;
            processTask.Configure(startInfo);
            return processTask;
        }

        public IProcess RunCommandLineWindow(string workingDirectory)
        {
            var shell = environment.IsWindows ? "cmd" : environment.IsMac ? "xterm" : "sh";
            var startInfo = new ProcessStartInfo(shell)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            gitEnvironment.Configure(startInfo, workingDirectory);
            var p = new ProcessTask<string>(cancellationToken);
            p.Configure(startInfo);
            return p;
        }

        public IProcess Reconnect(IProcess processTask, int pid)
        {
            logger.Trace("Reconnecting process " + pid);
            var p = Process.GetProcessById(pid);
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            processTask.Configure(p);
            return processTask;
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
