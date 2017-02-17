using GitHub.Unity;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    abstract class GitEnvironment : IGitEnvironment
    {
        protected IEnvironment Environment { get; private set; }
        protected IFileSystem FileSystem { get; private set; }
        protected ILogging Logger { get; private set; }

        protected GitEnvironment(IEnvironment environment, IFileSystem filesystem)
        {
            Environment = environment;
            FileSystem = filesystem;
            Logger = Logging.GetLogger(GetType());
        }

        public bool ValidateGitInstall(string path)
        {
            return FileSystem.FileExists(path);
        }

        public virtual async Task<string> FindGitInstallationPath(IProcessManager processManager)
        {
            return await LookForGitInPath(processManager);
        }

        public abstract string GetGitExecutableExtension();

        public string FindRoot(string path)
        {
            if (string.IsNullOrEmpty(FileSystem.GetDirectoryName(path)))
            {
                return null;
            }

            if (FileSystem.DirectoryExists(FileSystem.Combine(path, ".git")))
            {
                return path;
            }

            return FindRoot(FileSystem.GetParentDirectory(path));
        }

        public void Configure(ProcessStartInfo psi, string workingDirectory)
        {
            Guard.ArgumentNotNull(psi, "psi");

            // We need to essentially fake up what git-cmd.bat does
            string homeDir = Environment.UserProfilePath;

            var userPath = Environment.Path;

            var appPath = workingDirectory;
            var gitPathRoot = Environment.GitInstallPath;
            var gitLfsPath = Environment.GitInstallPath;

            // Paths to developer tools such as msbuild.exe
            //var developerPaths = StringExtensions.JoinForAppending(";", developerEnvironment.GetPaths());
            var developerPaths = "";

            //psi.EnvironmentVariables["github_shell"] = "true";
            //psi.EnvironmentVariables["git_install_root"] = gitPath; // todo: remove in favor of github_git
            //psi.EnvironmentVariables["github_git"] = gitPath;
            psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
            psi.EnvironmentVariables["TERM"] = "msys";

            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";
            //psi.EnvironmentVariables["GIT_TRACE"] = "1";

            if (Environment.IsWindows)
            {
                psi.EnvironmentVariables["PATH"] = String.Format(CultureInfo.InvariantCulture, @"{0}\cmd;{0}\usr\bin;{0}\mingw32\libexec\git-core;{0}\mingw64\libexec\git-core;{0}\usr\share\git-tfs;{1};{2};{3}{4}", gitPathRoot, appPath, gitLfsPath, userPath, developerPaths);
            }
            else
            {
                psi.EnvironmentVariables["PATH"] = String.Format(CultureInfo.InvariantCulture, @"{0}:{0}/libexec/git-core:{1}:{2}:{3}{4}", gitPathRoot, appPath, gitLfsPath, userPath, developerPaths);
            }
            psi.EnvironmentVariables["GIT_EXEC_PATH"] = Environment.GitInstallPath;


            //foreach (string k in psi.EnvironmentVariables.Keys)
            //{
            //    Logger.Debug("{0}={1}", k, psi.EnvironmentVariables[k]);
            //}


            //psi.EnvironmentVariables["HOME"] = homeDir;
            //psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = FileSystem.GetTempPath();
            //psi.EnvironmentVariables["EDITOR"] = Environment.GetEnvironmentVariable("EDITOR");

            //var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            //if (!String.IsNullOrEmpty(httpProxy))
            //    psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;
            //var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            //if (!String.IsNullOrEmpty(httpsProxy))
            //    psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;

            //var existingSshAgentProcess = sshAgentBridge.GetRunningSshAgentInfo();
            //if (existingSshAgentProcess != null)
            //{
            //    psi.EnvironmentVariables["SSH_AGENT_PID"] = existingSshAgentProcess.ProcessId;
            //    psi.EnvironmentVariables["SSH_AUTH_SOCK"] = existingSshAgentProcess.AuthSocket;
            //}

            bool internalUseOnly = false;
            if (internalUseOnly)
            {
                psi.EnvironmentVariables["GIT_PAGER"] = "cat";
                psi.EnvironmentVariables["LC_ALL"] = "C";
                psi.EnvironmentVariables["GIT_ASKPASS"] = "true";
                psi.EnvironmentVariables["DISPLAY"] = "localhost:1";
                psi.EnvironmentVariables["SSH_ASKPASS"] = "true";
                psi.EnvironmentVariables["GIT_SSH"] = "ssh-noprompt";

                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }

            psi.WorkingDirectory = workingDirectory;
        }

        protected async Task<string> LookForGitInPath(IProcessManager processManager)
        {
            string gitPath = null;
            var task = new ProcessTask(Environment, processManager,
                Environment.IsWindows ? "where" : "which",
                "git",
                x => gitPath = x);
            await task.RunAsync(CancellationToken.None);
            return gitPath;
        }

    }
}