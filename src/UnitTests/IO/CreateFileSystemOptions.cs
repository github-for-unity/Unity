using System.Collections.Generic;

namespace UnitTests
{
    class CreateFileSystemOptions
    {
        public const string DefaultTemporaryPath = @"c:\tmp";

        public string[] FilesThatExist { get; set; }
        public IDictionary<string, string[]> FileContents { get; set; }
        public IList<string> RandomFileNames { get; set; }
        public string TemporaryPath { get; set; } = DefaultTemporaryPath;
        public string[] DirectoriesThatExist { get; set; }
        public IDictionary<FolderContentsKey, string[]> FolderContents { get; set; }
    }
}