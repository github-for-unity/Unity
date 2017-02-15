namespace GitHub.Api
{
    interface IGitClient
    {
        IRepository GetRepository();
        ConfigBranch? GetActiveBranch();
        ConfigRemote? GetActiveRemote(string defaultRemote = "origin");
        string RepositoryPath { get; }
    }
}