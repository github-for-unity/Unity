using GitHub.Logging;
using System;
using System.Collections.Generic;
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

            var path = Environment.Path;
            psi.EnvironmentVariables["GHU_WORKINGDIR"] = workingDirectory;

            if (dontSetupGit)
            {
                psi.EnvironmentVariables["GHU_FULLPATH"] = path;
                psi.EnvironmentVariables["PATH"] = path;
                return;
            }

            Guard.ArgumentNotNull(psi, "psi");

            var pathEntries = new List<string>();
            string separator = Environment.IsWindows ? ";" : ":";

            NPath libexecPath = NPath.Default;
            List<string> gitPathEntries = new List<string>();
            if (Environment.GitInstallPath.IsInitialized)
            {
                var gitPathRoot = Environment.GitExecutablePath.Resolve().Parent.Parent;
                var gitExecutableDir = Environment.GitExecutablePath.Parent; // original path to git (might be different from install path if it's a symlink)

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

                libexecPath = baseExecPath.Combine("libexec", "git-core");
                if (!libexecPath.DirectoryExists())
                    libexecPath = NPath.Default;

                if (Environment.IsWindows)
                {
                    gitPathEntries.AddRange(new[] { gitPathRoot.Combine("cmd").ToString(), gitPathRoot.Combine("usr", "bin") });
                }
                else
                {
                    gitPathEntries.Add(gitExecutableDir.ToString());
                }

                if (libexecPath.IsInitialized)
                    gitPathEntries.Add(libexecPath);
                gitPathEntries.Add(binPath);

                // we can only set this env var if there is a libexec/git-core. git will bypass internally bundled tools if this env var
                // is set, which will break Apple's system git on certain tools (like osx-credentialmanager)
                if (libexecPath.IsInitialized)
                {
                    psi.EnvironmentVariables["GIT_EXEC_PATH"] = libexecPath.ToString();
                    if (Environment.IsWindows)
                    {
                        psi.EnvironmentVariables["GIT_ASKPASS"] = libexecPath.Combine("git-askpass.exe");
                        psi.EnvironmentVariables["SSH_ASKPASS"] = libexecPath.Combine("git-askpass.exe");
                    }
                }
            }

            if (Environment.GitLfsInstallPath.IsInitialized && libexecPath != Environment.GitLfsInstallPath)
            {
                pathEntries.Add(Environment.GitLfsInstallPath);
            }
            if (gitPathEntries.Count > 0)
                pathEntries.AddRange(gitPathEntries);

            pathEntries.Add("END");

            path = String.Join(separator, pathEntries.ToArray()) + separator + path;

            psi.EnvironmentVariables["GHU_FULLPATH"] = path;
            psi.EnvironmentVariables["PATH"] = path;

            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";

            if (Environment.IsWindows)
            {
                psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
                psi.EnvironmentVariables["TERM"] = "msys";
            }

            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!String.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!String.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;

            psi.EnvironmentVariables["DISPLAY"] = "0";
        }
    }
}
