using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    public class GitStatusEntryTests
    {
        [Test]
        public void ShouldNotBeEqualIfGitFileStatusIsDifferent()
        {
            var gitStatusEntry1 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Modified, "SomeOriginalPath");

            gitStatusEntry1.Should().NotBe(gitStatusEntry2);
        }

        [Test]
        public void ShouldNotBeEqualIfPathIsDifferent()
        {
            var gitStatusEntry1 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath2", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry1.Should().NotBe(gitStatusEntry2);
        }

        [Test]
        public void ShouldBeEqualIfOriginalpathIsNull()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added);

            gitStatusEntry.Should().Be(gitStatusEntry);
        }

        [Test]
        public void ShouldBeEqual()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry.Should().Be(gitStatusEntry);
        }

        [Test]
        public void ShouldBeEqualToOther()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry.Should().Be(gitStatusEntry2);
        }

        [Test]
        public void ShouldNotBeEqualIfOneIsStaged()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath", staged: true);

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry.Should().NotBe(gitStatusEntry2);
        }

        [Test]
        public void ShouldBeEqualIfBothAreStaged()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath", true);

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, "SomeOriginalPath", true);

            gitStatusEntry.Should().Be(gitStatusEntry2);
        }
    }
}