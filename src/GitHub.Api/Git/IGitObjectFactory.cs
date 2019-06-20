namespace GitHub.Unity
{
    public interface IGitObjectFactory
    {
        GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus indexStatus, GitFileStatus workTreeStatus, string originalPath = null);
    }
}
