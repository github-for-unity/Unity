using System;

namespace GitHub.Api
{
    class PortableGitManager : PortablePackageManager, IPortableGitManager
    {
        readonly Lazy<string> gitExecutablePath;
        readonly Lazy<string> gitEtcDirPath;
//        readonly Lazy<IFile> systemConfigFile;

//        readonly IEmbeddedResource embeddedSystemConfigFile;
//        readonly IEmbeddedResource embeddedGitAttributesFile;
//        readonly IProcessStarter processStarter;

//        [ImportingConstructor]
//        public PortableGitManager(
//            IOperatingSystem operatingSystem,
//            IProcessStarter processStarter,
//            IProgram program,
//            IZipArchive zipArchive)
//            : this(operatingSystem,
//                processStarter,
//                program,
//                zipArchive,
//                new EmbeddedResource(typeof(PortableGitManager).Assembly, "GitHub.PortableGit.Resources.gitconfig", "gitconfig", operatingSystem),
//                new EmbeddedResource(typeof(PortableGitManager).Assembly, "GitHub.PortableGit.Resources.gitattributes.suggested", ".gitattributes", operatingSystem))
//        {
//        }

//        public PortableGitManager(
//            IOperatingSystem operatingSystem,
//            IProcessStarter processStarter,
//            IProgram program,
//            IZipArchive zipArchive,
//            IEmbeddedResource embeddedSystemConfigFile,
//            IEmbeddedResource embeddedGitAttributesFile)
//            : base(operatingSystem, program, zipArchive)
//        {
//            Ensure.ArgumentNotNull(embeddedSystemConfigFile, "embeddedSystemConfigFile");
//            Ensure.ArgumentNotNull(embeddedGitAttributesFile, "embeddedGitAttributesFile");
//            Ensure.ArgumentNotNull(processStarter, "processStarter");

//            this.embeddedSystemConfigFile = embeddedSystemConfigFile;
//            this.embeddedGitAttributesFile = embeddedGitAttributesFile;
//            this.processStarter = processStarter;

//            gitExecutablePath = new Lazy<string>(() =>
//            {
//                var path = Path.Combine(GetPortableGitDestinationDirectory(), "cmd", "git.exe");
//                Debug.Assert(operatingSystem != null, "The base class should verify this is not null");
//                if (!operatingSystem.FileExists(path))
//                {
//                    log.Error(CultureInfo.InvariantCulture, "git.exe doesn't exist at '{0}'", path);
//                }
//                return path;
//            });

//            gitEtcDirPath = new Lazy<string>(() => Path.Combine(GetPortableGitDestinationDirectory(), "mingw32", "etc"));
//            systemConfigFile = new Lazy<IFile>(
//                () => operatingSystem.GetFile(Path.Combine(EtcDirectoryPath, "gitconfig")));
//        }

        public PortableGitManager(IEnvironment environment, IFileSystem fileSystem, ISharpZipLibHelper sharpZipLibHelper) 
            : base(environment, fileSystem, sharpZipLibHelper)
        {
            
        }

        public void ExtractGitIfNeeded()
        {
            ExtractPackageIfNeeded("PortableGit.zip", null, null);
        }

        /// <summary>
        /// Indicates if PortableGit has been extracted or not.
        /// </summary>
        public bool IsExtracted()
        {
            return IsPackageExtracted();
        }

        public string GetPortableGitDestinationDirectory(bool createIfNeeded = false)
        {
            return GetPackageDestinationDirectory(createIfNeeded);
        }

        public void EnsureSystemConfigFileExtracted()
        {
            //TODO: Look here next
//            var configFile = systemConfigFile.Value;
//            if (configFile.Exists) return Observable.Return(configFile);
//
//            embeddedSystemConfigFile.ExtractToFile(EtcDirectoryPath);
//            configFile.Refresh();
//            Debug.Assert(configFile.Exists, "After extracting the system config file, we expect it to exist.");
//            return Observable.Return(configFile);
        }

        public string ExtractSuggestedGitAttributes(string targetDirectory)
        {
            throw new NotImplementedException();

//            return embeddedGitAttributesFile.ExtractToFile(targetDirectory);
        }

        protected override string GetExpectedVersion()
        {
            return "f02737a78695063deace08e96d5042710d3e32db";
        }

        protected override string GetPathToCanary(string rootDir)
        {
            return FileSystem.Combine(rootDir, "cmd", "git.exe");
        }

        protected override string GetPackageName()
        {
            return "PortableGit";
        }
    }
}
