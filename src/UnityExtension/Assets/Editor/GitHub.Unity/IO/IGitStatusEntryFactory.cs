namespace GitHub.Unity
{
    interface IGitStatusEntryFactory
    {
        GitStatusEntry Create(string path, GitFileStatus status, string originalPath = null, bool staged = false);
        GitLock CreateGitLock(string file, string server, string user, int userId);
    }
}
