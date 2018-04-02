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
            Guard.ArgumentNotNull(psi, "psi");
       
            psi.WorkingDirectory = workingDirectory;
            psi.EnvironmentVariables["HOME"] = NPath.HomeDirectory;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = NPath.SystemTemp;
            psi.EnvironmentVariables["GHU_WORKINGDIR"] = workingDirectory;
            psi.EnvironmentVariables["PATH"] = Environment.Path;
            psi.EnvironmentVariables["GHU_FULLPATH"] = Environment.Path;

            // if we don't know where git is, then there's nothing else to configure
            if (!Environment.GitInstallPath.IsInitialized || dontSetupGit)
                return;
    
            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!String.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;
    
            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!String.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
    
            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";

            if (Environment.IsWindows)
            {
                // We need to essentially fake up what git-cmd.bat does
                var gitPathRoot = Environment.GitInstallPath;
    
                var baseExecPath = gitPathRoot;
                if (baseExecPath.DirectoryExists("mingw32"))
                    baseExecPath = baseExecPath.Combine("mingw32");
                else
                    baseExecPath = baseExecPath.Combine("mingw64");
                var binPath = baseExecPath.Combine("bin");
    
                var execPath = baseExecPath.Combine("libexec", "git-core");
                if (!execPath.DirectoryExists())
                    execPath = NPath.Default;

                var userPath = @"C:\windows\system32;C:\windows";
                var path = $"{gitPathRoot}\\cmd;{gitPathRoot}\\usr\\bin;{execPath};{binPath}";
    
                Logger.Trace("Calculated Path: {0}", path);
    
                if (execPath.IsInitialized)
                    psi.EnvironmentVariables["GIT_EXEC_PATH"] = execPath.ToString();
    
                psi.EnvironmentVariables["PATH"] = path;
                psi.EnvironmentVariables["GHU_FULLPATH"] = path;
    
                psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
                psi.EnvironmentVariables["TERM"] = "msys";
            }
        }
    }
}