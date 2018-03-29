using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class ProcessManager : IProcessManager
    {
        private static readonly ILogging logger = LogHelper.GetLogger<ProcessManager>();

        private readonly IEnvironment environment;
        private readonly IProcessEnvironment gitEnvironment;
        private readonly CancellationToken cancellationToken;

        public ProcessManager(IEnvironment environment, IProcessEnvironment gitEnvironment, CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.gitEnvironment = gitEnvironment;
            this.cancellationToken = cancellationToken;
        }

        public T Configure<T>(T processTask, NPath? executable = null, string arguments = null,
            NPath? workingDirectory = null,
            bool withInput = false,
            bool dontSetupGit = false)
             where T : IProcess
        {
            logger.Trace("Configure executable:{0}", executable);
            
            if (executable == null)
            {
                if (processTask.ProcessName?.ToNPath() != null)
                {
                    executable = processTask.ProcessName.ToNPath();
                }
                else
                {
                    executable = environment.GitExecutablePath;
                    dontSetupGit = environment.IsCustomGitExecutable;
                }
            }

            //If this null check fails, be sure you called Configure() on your task
            Guard.ArgumentNotNull(executable, nameof(executable));

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

            logger.Trace("gitEnvironment.Configure dontSetupGit:{0}", dontSetupGit);
            gitEnvironment.Configure(startInfo, workingDirectory ?? environment.RepositoryPath, dontSetupGit);

            if (executable.Value.IsRelative)
            {
                executable = executable.Value.FileName.ToNPath();
                executable = FindExecutableInPath(executable.Value, startInfo.EnvironmentVariables["PATH"]) ?? executable;
            }
            startInfo.FileName = executable;
            startInfo.Arguments = arguments ?? processTask.ProcessArguments;
            processTask.Configure(startInfo);
            return processTask;
        }

        public void RunCommandLineWindow(NPath workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            if (environment.IsWindows)
            {
                startInfo.FileName = "cmd";
                gitEnvironment.Configure(startInfo, workingDirectory, environment.IsCustomGitExecutable);
            }
            else if (environment.IsMac)
            {
                // we need to create a temp bash script to set up the environment properly, because
                // osx terminal app doesn't inherit the PATH env var and there's no way to pass it in

                var envVarFile = NPath.GetTempFilename();
                startInfo.FileName = "open";
                startInfo.Arguments = $"-a Terminal {envVarFile}";
                gitEnvironment.Configure(startInfo, workingDirectory, environment.IsCustomGitExecutable);

                var envVars = startInfo.EnvironmentVariables;
                var scriptContents = new List<string> {
                    $"cd \"{envVars["GHU_WORKINGDIR"]}\"",
                };

                if (!environment.IsCustomGitExecutable) {
                    scriptContents.Add($"PATH=\"{envVars["GHU_FULLPATH"]}\" /bin/bash");
                }
                else {
                    scriptContents.Add($"/bin/bash");
                }
                
                environment.FileSystem.WriteAllLines(envVarFile, scriptContents.ToArray());
                Mono.Unix.Native.Syscall.chmod(envVarFile, (Mono.Unix.Native.FilePermissions)493); // -rwxr-xr-x mode (0755)
            }
            else
            {
                startInfo.FileName = "sh";
                gitEnvironment.Configure(startInfo, workingDirectory, environment.IsCustomGitExecutable);
            }

            Process.Start(startInfo);
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

        private NPath? FindExecutableInPath(NPath executable, string searchPaths = null)
        {
            Guard.ArgumentNotNullOrWhiteSpace(executable, "executable");

            if (executable.IsRelative) return executable;

            searchPaths = searchPaths ?? environment.GetEnvironmentVariable("PATH");
            var executablePath = searchPaths.Split(Path.PathSeparator)
                .Where(x => !String.IsNullOrEmpty(x))
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
                        return new NPath?();
                    }
                })
                .Where(x => x != null)
                .FirstOrDefault(x => x.Value.FileExists());

            return executablePath;
        }

        public CancellationToken CancellationToken { get { return cancellationToken; } }
    }
}
