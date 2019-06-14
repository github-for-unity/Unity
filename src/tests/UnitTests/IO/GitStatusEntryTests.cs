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
                GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Modified, "SomeOriginalPath");

            gitStatusEntry1.Should().NotBe(gitStatusEntry2);

            gitStatusEntry1 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Modified, GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Renamed, GitFileStatus.Modified, "SomeOriginalPath");

            gitStatusEntry1.Should().NotBe(gitStatusEntry2);

        }

        [Test]
        public void ShouldNotBeEqualIfPathIsDifferent()
        {
            var gitStatusEntry1 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath2", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry1.Should().NotBe(gitStatusEntry2);
        }

        [Test]
        public void ShouldBeEqualIfOriginalpathIsNull()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Added);

            gitStatusEntry.Should().Be(gitStatusEntry);
        }

        [Test]
        public void ShouldBeEqual()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");

            gitStatusEntry.Should().Be(gitStatusEntry);
        }

        [Test]
        public void ShouldBeEqualToOther()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");

            gitStatusEntry.Should().Be(gitStatusEntry2);
        }

        [Test]
        public void ShouldNotBeEqualIfOneIsStaged()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath");

            gitStatusEntry.Should().NotBe(gitStatusEntry2);
        }

        [Test]
        public void ShouldBeEqualIfBothAreStaged()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");

            var gitStatusEntry2 = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");

            gitStatusEntry.Should().Be(gitStatusEntry2);
        }

        [Test]
        public void StagedIsTrue()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Modified, GitFileStatus.None, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Deleted, GitFileStatus.None, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Copied, GitFileStatus.None, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Renamed, GitFileStatus.None, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeTrue();
        }

        [Test]
        public void StagedIsFalse()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Added, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeFalse();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Modified, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeFalse();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Deleted, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeFalse();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Copied, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeFalse();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.None, GitFileStatus.Renamed, "SomeOriginalPath");
            gitStatusEntry.Staged.Should().BeFalse();
        }

        [Test]
        public void UnmergedDetection()
        {
            var gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.Added, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Deleted, GitFileStatus.Deleted, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Unmerged, GitFileStatus.Unmerged, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Added, GitFileStatus.Unmerged, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Unmerged, GitFileStatus.Added, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Deleted, GitFileStatus.Unmerged, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();

            gitStatusEntry = new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath",
                GitFileStatus.Unmerged, GitFileStatus.Deleted, "SomeOriginalPath");
            gitStatusEntry.Unmerged.Should().BeTrue();
        }
    }
}
