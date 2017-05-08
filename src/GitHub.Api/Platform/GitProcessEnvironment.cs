using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    abstract class GitProcessEnvironment : IProcessEnvironment
    {
        protected IEnvironment Environment { get; private set; }
        protected IFileSystem FileSystem { get; private set; }
        protected ILogging Logger { get; private set; }

        protected GitProcessEnvironment(IEnvironment environment, IFileSystem filesystem)
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

        public abstract string GetExecutableExtension();

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
            string homeDir = NPath.HomeDirectory;

            var gitPathRoot = Environment.GitInstallPath;
            var gitLfsPath = Environment.GitInstallPath;

            // Paths to developer tools such as msbuild.exe
            //var developerPaths = StringExtensions.JoinForAppending(";", developerEnvironment.GetPaths());
            var developerPaths = "";

            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";
            //psi.EnvironmentVariables["GIT_TRACE"] = "1";

            string path;
            NPath baseExecPath = gitPathRoot;
            NPath binPath = baseExecPath;
            if (Environment.IsWindows)
            {
                if (baseExecPath.DirectoryExists("mingw32"))
                    baseExecPath = baseExecPath.Combine("mingw32");
                else
                    baseExecPath = baseExecPath.Combine("mingw64");
                binPath = baseExecPath.Combine("bin");
            }
            NPath execPath = baseExecPath.Combine("libexec", "git-core");

            if (Environment.IsWindows)
            {
                var userPath = @"C:\windows\system32;C:\windows";
                path = String.Format(CultureInfo.InvariantCulture, @"{0}\cmd;{0}\usr\bin;{1};{2};{0}\usr\share\git-tfs;{3};{4}{5}",
                    gitPathRoot, execPath, binPath,
                    gitLfsPath, userPath, developerPaths);
            }
            else
            {
                var userPath = Environment.Path;
                path = String.Format(CultureInfo.InvariantCulture, @"{0}:{1}:{2}:{3}{4}",
                    binPath, execPath, gitLfsPath, userPath, developerPaths);
            }
            psi.EnvironmentVariables["GIT_EXEC_PATH"] = execPath.ToString();

            //Logger.Trace("EnvironmentVariables[\"PATH\"]=\"{0}\"", path);

            psi.EnvironmentVariables["PATH"] = path;

            //psi.EnvironmentVariables["github_shell"] = "true";
            //psi.EnvironmentVariables["git_install_root"] = gitPath; // todo: remove in favor of github_git
            //psi.EnvironmentVariables["github_git"] = gitPath;
            psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
            psi.EnvironmentVariables["TERM"] = "msys";


            psi.EnvironmentVariables["HOME"] = homeDir;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = FileSystem.GetTempPath();
            //psi.EnvironmentVariables["EDITOR"] = Environment.GetEnvironmentVariable("EDITOR");

            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!String.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;
            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!String.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;


            //foreach (string k in psi.EnvironmentVariables.Keys)
            //{
            //    Logger.Debug("{0}={1}", k, psi.EnvironmentVariables[k]);
            //}

            //var existingSshAgentProcess = sshAgentBridge.GetRunningSshAgentInfo();
            //if (existingSshAgentProcess != null)
            //{
            //    psi.EnvironmentVariables["SSH_AGENT_PID"] = existingSshAgentProcess.ProcessId;
            //    psi.EnvironmentVariables["SSH_AUTH_SOCK"] = existingSshAgentProcess.AuthSocket;
            //}

            var internalUseOnly = false;
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
            return await new ProcessTask<string>(CancellationToken.None, Environment.IsWindows ? "where" : "which")
                .ConfigureGitProcess(processManager)
                .Task;
        }

    }
}