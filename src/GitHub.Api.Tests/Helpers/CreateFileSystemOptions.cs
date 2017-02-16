using System.Collections.Generic;

namespace GitHub.Unity.Tests
{
    class CreateFileSystemOptions
    {
        public const string DefaultTemporaryPath = @"c:\tmp";

        public string[] FilesThatExist { get; set; }
        public IDictionary<string, string[]> FileContents { get; set; }
        public IList<string> RandomFileNames { get; set; }
        public string TemporaryPath { get; set; } = DefaultTemporaryPath;
        public string[] DirectoriesThatExist { get; set; }
        public IDictionary<SubstituteFactory.FolderContentsKey, string[]> FolderContents { get; set; }
    }
}