using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests.UI
{
    public class TestTreeData : ITreeData
    {
        public string FullPath { get; set; }
        public string Path { get; set; }
        public bool IsActive { get; set; }
        public string CustomStringTag { get; set;  }
        public int CustomIntTag { get; set; }
    }

    [TestFixture]
    public class TreeLoaderTests
    {
      
    }
}
