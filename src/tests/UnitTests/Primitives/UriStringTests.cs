using GitHub.Unity;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests.Primitives
{
    [TestFixture]
    class UriStringTests
    {
        [TestCase("http://url.com/path/file.zip?cb=1", "file.zip")]
        [TestCase("http://url.com/path/file?cb=1", "file")]
        public void FilenameParsing(string url, string expectedFilename)
        {
            var uriString = new UriString(url);
            Assert.AreEqual(expectedFilename, uriString.Filename);
        }
    }
}
