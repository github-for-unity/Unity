using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    public class ProcessManager
    {
        private static readonly ILogger logger = Logger.GetLogger<ProcessManager>();
        private static readonly IFileSystem fs = new FileSystem();

        private static ProcessManager instance;
        public static ProcessManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ProcessManager();
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        private readonly IGitEnvironment gitEnvironment;

        public ProcessManager()
        {
            gitEnvironment = new GitEnvironment();
        }

        public ProcessManager(IGitEnvironment gitEnvironment)
        {
            this.gitEnvironment = gitEnvironment;
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

            path = path ?? gitEnvironment.Environment.GetEnvironmentVariable("PATH");
            var executablePath = path.Split(Path.PathSeparator)
                .Select(directory =>
                {
                    try
                    {
                        var unquoted = directory.RemoveSurroundingQuotes();
                        var expanded = gitEnvironment.Environment.ExpandEnvironmentVariables(unquoted);
                        return Path.Combine(expanded, executable);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error while looking for {0} in {1}\n{2}", executable, directory, e);
                        return null;
                    }
                })
                .Where(x => x != null)
                .FirstOrDefault(x => fs.FileExists(x));

            return executablePath;
        }
    }
}
