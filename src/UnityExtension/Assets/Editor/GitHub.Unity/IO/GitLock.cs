namespace GitHub.Unity
{
    struct GitLock
    {
        public readonly string Path;
        public readonly string FullPath;
        public readonly string Server;
        public readonly string User;
        public readonly int UserId;

        public GitLock(string path, string fullPath, string server, string user, int userId)
        {
            Path = path;
            FullPath = fullPath;
            Server = server;
            User = user;
            UserId = userId;
        }
    }
}
