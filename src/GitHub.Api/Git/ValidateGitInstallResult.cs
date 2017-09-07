namespace GitHub.Unity
{
    struct ValidateGitInstallResult
    {
        public bool IsValid;
        public SoftwareVersion GitVersion;
        public SoftwareVersion GitLfsVersion;

        public ValidateGitInstallResult(bool isValid, SoftwareVersion gitVersion, SoftwareVersion gitLfsVersion)
        {
            IsValid = isValid;
            GitVersion = gitVersion;
            GitLfsVersion = gitLfsVersion;
        }
    }
}