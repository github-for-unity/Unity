namespace GitHub.Api
{
    interface IGitClient
    {
        IRepository GetRepository();
        ConfigBranch? ActiveBranch { get; }

        ConfigRemote? GetActiveRemote(string defaultRemote = "origin");
        string RepositoryPath { get; }
    }
}