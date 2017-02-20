namespace GitHub.Unity
{
    class StatusEventDispatcher
    {
        public StatusEventDispatcher(IEnvironment environment, IPlatform platform)
        {
            var fsw = platform.FileSystemWatchFactory.GetOrCreate(environment.RepositoryPath, true);
        }
    }
}