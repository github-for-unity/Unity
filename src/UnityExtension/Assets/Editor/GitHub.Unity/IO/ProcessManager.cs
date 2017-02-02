using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class ProcessManager: IProcessManager
    {
        private static readonly ILogger logger = Logger.GetLogger<ProcessManager>();

        private readonly IEnvironment environment;
        private readonly IGitEnvironment gitEnvironment;
        private readonly IFileSystem fileSystem;

        public ProcessManager(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fileSystem)
        {
            this.environment = environment;
            this.gitEnvironment = gitEnvironment;
            this.fileSystem = fileSystem;
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
                CreateNoWindow = true
            };

            gitEnvironment.Configure(startInfo, workingDirectory);
            startInfo.FileName = FindExecutableInPath(executableFileName, startInfo.EnvironmentVariables["PATH"]) ?? executableFileName;
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
            Ensure.ArgumentNotNullOrEmpty(executable, "executable");

            if (Path.IsPathRooted(executable)) return executable;

            path = path ?? environment.GetEnvironmentVariable("PATH");
            var executablePath = path.Split(Path.PathSeparator)
                .Select(directory =>
                {
                    try
                    {
                        var unquoted = directory.RemoveSurroundingQuotes();
                        var expanded = environment.ExpandEnvironmentVariables(unquoted);
                        return Path.Combine(expanded, executable);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error while looking for {0} in {1}\n{2}", executable, directory, e);
                        return null;
                    }
                })
                .Where(x => x != null)
                .FirstOrDefault(x => fileSystem.FileExists(x));

            return executablePath;
        }
    }
}
