namespace GitHub.Unity
{
    struct GitLock
    {
        public readonly string Path;
        public readonly string FullPath;
        public readonly string User;

        public GitLock(string path, string fullPath, string user)
        {
            Path = path;
            FullPath = fullPath;
            User = user;
        }
    }
}
