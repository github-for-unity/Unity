using System;

namespace GitHub.Unity
{
    class DefaultEnvironment : IEnvironment
    {
        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return ExpandEnvironmentVariables(Environment.GetFolderPath(folder));
        }

        public string ExpandEnvironmentVariables(string name)
        {
            return Environment.ExpandEnvironmentVariables(name);
        }

        public string GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        public string GetTempPath()
        {
            return System.IO.Path.GetTempPath();
        }

        public string UserProfilePath { get { return Environment.GetEnvironmentVariable("USERPROFILE"); } }
        public string Path { get { return Environment.GetEnvironmentVariable("PATH"); } }
        public string NewLine { get { return Environment.NewLine; } }
        public string GitInstallPath { get; set; }
        public bool IsWindows { get; set; }
    }
}