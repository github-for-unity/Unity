using GitHub.Unity;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    public class GitStatusEntryListTests
    {
        [Test]
        public void ListOf2ShouldEqualListOf2()
        {
            var gitStatusEntry1 = new[]
            {
                new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath"),

                new GitStatusEntry("ASDFSomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Modified)
            };

            var gitStatusEntry2 = new[]
            {
                new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath"),

                new GitStatusEntry("ASDFSomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Modified)
            };

            gitStatusEntry1.AssertEqual(gitStatusEntry2);
        }

        [Test]
        public void ListOf2ShouldNotEqualListOf2InDifferentOrder()
        {
            var gitStatusEntry1 = new[]
            {
                new GitStatusEntry("ASDFSomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Modified),

                new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath")
            };

            var gitStatusEntry2 = new[]
            {
                new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath"),

                new GitStatusEntry("ASDFSomePath", "SomeFullPath", "SomeProjectPath",
                    GitFileStatus.None, GitFileStatus.Modified)
            };

            gitStatusEntry1.AssertNotEqual(gitStatusEntry2);
        }
    }
}
