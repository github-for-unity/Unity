using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests.UI
{
    public class TestTreeData : ITreeData
    {
        public string FullPath { get; set; }
        public string Path { get; set; }
        public bool IsActive { get; set; }
    }

    [TestFixture]
    public class TreeLoaderTests
    {
      
    }
}
