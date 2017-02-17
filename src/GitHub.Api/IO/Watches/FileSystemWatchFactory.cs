namespace GitHub.Api
{
    class FileSystemWatchFactory : IFileSystemWatchFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly IFileSystemWatchWrapperFactory fileSystemWatchWrapperFactory;
        private readonly FactoryStrategy strategy;

        public FileSystemWatchFactory(IEnvironment environment,
            IFileSystemWatchWrapperFactory fileSystemWatchWrapperFactory, IFileSystem fileSystem)
        {
            this.fileSystemWatchWrapperFactory = fileSystemWatchWrapperFactory;
            this.fileSystem = fileSystem;
            if (environment.IsWindows)
            {
                strategy = new DefaultStrategy(this);
            }
            else
            {
                strategy = new AdaptiveStrategy(this);
            }
        }

        public FileSystemWatch CreteFileSystemWatch(string path, bool recursive = false, string filter = null)
        {
            return strategy.CreateFileSystemWatch(path, recursive, filter);
        }

        private abstract class FactoryStrategy
        {
            protected FactoryStrategy(FileSystemWatchFactory factory)
            {
                Factory = factory;
            }

            public abstract FileSystemWatch CreateFileSystemWatch(string path, bool recursive, string filter);

            protected FileSystemWatchFactory Factory { get; }
        }

        private class DefaultStrategy : FactoryStrategy
        {
            public DefaultStrategy(FileSystemWatchFactory factory) : base(factory)
            {}

            public override FileSystemWatch CreateFileSystemWatch(string path, bool recursive, string filter)
            {
                return new DefaultFileSystemWatch(Factory.fileSystemWatchWrapperFactory, path, recursive, filter);
            }
        }

        private class AdaptiveStrategy : FactoryStrategy
        {
            public AdaptiveStrategy(FileSystemWatchFactory factory) : base(factory)
            {}

            public override FileSystemWatch CreateFileSystemWatch(string path, bool recursive, string filter)
            {
                return new AdaptiveFileSystemWatch(Factory.fileSystemWatchWrapperFactory, Factory.fileSystem, path,
                    recursive, filter);
            }
        }
    }
}
