using GitHub.Logging;
using System;
using System.Diagnostics;

namespace GitHub.Unity
{
    class ProcessEnvironment : IProcessEnvironment
    {
        protected IEnvironment Environment { get; private set; }
        protected ILogging Logger { get; private set; }

        public ProcessEnvironment(IEnvironment environment)
        {
            Logger = LogHelper.GetLogger(GetType());
            Environment = environment;
        }

        public void Configure(ProcessStartInfo psi, NPath workingDirectory, bool dontSetupGit = false)
        {
            psi.WorkingDirectory = workingDirectory;
            psi.EnvironmentVariables["HOME"] = NPath.HomeDirectory;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = NPath.SystemTemp;

            // if we don't know where git is, then there's nothing else to configure
            if (!Environment.GitInstallPath.IsInitialized || dontSetupGit)
                return;


            Guard.ArgumentNotNull(psi, "psi");

            // We need to essentially fake up what git-cmd.bat does

            var gitPathRoot = Environment.GitInstallPath;
            var gitLfsPath = Environment.GitInstallPath;
            var gitExecutableDir = Environment.GitExecutablePath.Parent; // original path to git (might be different from install path if it's a symlink)

            // Paths to developer tools such as msbuild.exe
            //var developerPaths = StringExtensions.JoinForAppending(";", developerEnvironment.GetPaths());
            var developerPaths = "";

            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";

            string path;
            var baseExecPath = gitPathRoot;
            var binPath = baseExecPath;
            if (Environment.IsWindows)
            {
                if (baseExecPath.DirectoryExists("mingw32"))
                    baseExecPath = baseExecPath.Combine("mingw32");
                else
                    baseExecPath = baseExecPath.Combine("mingw64");
                binPath = baseExecPath.Combine("bin");
            }

            var execPath = baseExecPath.Combine("libexec", "git-core");
            if (!execPath.DirectoryExists())
                execPath = NPath.Default;

            if (Environment.IsWindows)
            {
                var userPath = @"C:\windows\system32;C:\windows";
                path = $"{gitPathRoot}\\cmd;{gitPathRoot}\\usr\\bin;{execPath};{binPath};{gitLfsPath};{userPath}{developerPaths}";
            }
            else
            {
                path = $"{gitExecutableDir}:{binPath}:{execPath}:{gitLfsPath}:{Environment.Path}:{developerPaths}";
            }

            if (execPath.IsInitialized)
                psi.EnvironmentVariables["GIT_EXEC_PATH"] = execPath.ToString();

            psi.EnvironmentVariables["PATH"] = path;
            psi.EnvironmentVariables["GHU_FULLPATH"] = path;
            psi.EnvironmentVariables["GHU_WORKINGDIR"] = workingDirectory;

            psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
            psi.EnvironmentVariables["TERM"] = "msys";

            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!String.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!String.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
        }
    }
}