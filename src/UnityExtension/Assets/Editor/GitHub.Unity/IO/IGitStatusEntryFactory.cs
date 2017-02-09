namespace GitHub.Unity
{
    interface IGitStatusEntryFactory
    {
        GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus status, string originalPath = null, bool staged = false);
        GitLock CreateGitLock(string path, string server, string user, int userId);
    }
}
