namespace GitHub.Unity
{
    interface IGitObjectFactory
    {
        GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus status, string originalPath = null, bool staged = false);
    }
}
