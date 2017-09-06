namespace GitHub.Unity
{
    struct ValidateGitInstallResult
    {
        public bool IsValid;
        public SoftwareVersion GitVersionTask;
        public SoftwareVersion GitLfsVersionTask;

        public ValidateGitInstallResult(bool isValid, SoftwareVersion gitVersionTask, SoftwareVersion gitLfsVersionTask)
        {
            IsValid = isValid;
            GitVersionTask = gitVersionTask;
            GitLfsVersionTask = gitLfsVersionTask;
        }
    }
}