using System;

namespace GitHub.Unity
{
    struct ValidateGitInstallResult
    {
        public bool IsValid;
        public Version GitVersion;
        public Version GitLfsVersion;

        public ValidateGitInstallResult(bool isValid, Version gitVersion, Version gitLfsVersion)
        {
            IsValid = isValid;
            GitVersion = gitVersion;
            GitLfsVersion = gitLfsVersion;
        }
    }
}