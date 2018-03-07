using System;
using System.Collections.Generic;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture, Isolated]
    class FindCommonPathTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var substituteFactory = new TestUtils.SubstituteFactory();
            var fileSystem = substituteFactory.CreateFileSystem(new CreateFileSystemOptions() { });

            NPath.FileSystem = fileSystem;
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            NPath.FileSystem = null;
        }

        [Test]
        public void ShouldNotErrorIfEmpty()
        {
            Action item;

            item = () => { FileSystemHelpers.FindCommonPath(new List<string>()); };
            item.ShouldNotThrow<InvalidOperationException>();

            item = () => { FileSystemHelpers.FindCommonPath(new List<string> { null }); };
            item.ShouldNotThrow<InvalidOperationException>();

            item = () => { FileSystemHelpers.FindCommonPath(new List<string> { "" }); };
            item.ShouldNotThrow<InvalidOperationException>();
        }

        [Test]
        public void ShouldFindPaths()
        {
            AssertCommonPathFound(null, ".");
            AssertCommonPathFound(null, ".", "./f1/asdf.txt");
            AssertCommonPathFound(".", "./f2", "./f1/asdf.txt");
            AssertCommonPathFound(@".\f1", "./f1/asdf2.txt", "./f1/asdf.txt");
            AssertCommonPathFound(@".\f1", "./f1/asdf2.txt", "./f1/c2/asdf.txt");
            AssertCommonPathFound(@".\f1\c2", "./f1/c2/asdf2.txt", "./f1/c2/asdf.txt");
            AssertCommonPathFound(@".\f1\c2", "./f1/c2/asdf2.txt", "./f1/c2/asdf.txt", "./f1/c2/c3/asdf.txt");
            AssertCommonPathFound(@".\f1", "./f1/c2/asdf2.txt", "./f1/c2/asdf.txt", "./f1/asdf.txt");
        }

        private static void AssertCommonPathFound(string expected, params string[] paths)
        {
            var findCommonPath = FileSystemHelpers.FindCommonPath(paths);
            findCommonPath.Should().Be(expected);
        }
    }
}
