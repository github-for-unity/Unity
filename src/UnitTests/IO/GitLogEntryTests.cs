using System;
using System.Collections.Generic;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    public class GitLogEntryTests
    {
        [Test]
        public void ShouldEqualSelf()
        {
            var gitLogEntry = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            gitLogEntry.AssertEqual(gitLogEntry);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsNull()
        {
            var gitLogEntry1 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = null,
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            var gitLogEntry2 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = null,
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldNotEqualAnotherWhenFieldsAreDifferent()
        {
            var gitLogEntry1 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = null,
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            var gitLogEntry2 = new GitLogEntry()
            {
                AuthorName = "ASDFASDF",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = null,
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            gitLogEntry1.AssertNotEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsEmpty()
        {
            var gitLogEntry1 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            var gitLogEntry2 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsNotEmpty()
        {
            var gitLogEntry1 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                        "SomeOriginalPath"),
                }),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            var gitLogEntry2 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                        "SomeOriginalPath")
                }),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldNotEqualAnotherWhenChangesAreDifferent()
        {
            var gitLogEntry1 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("ASDFASDF", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                        "SomeOriginalPath"),
                }),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            var gitLogEntry2 = new GitLogEntry()
            {
                AuthorName = "AuthorName",
                AuthorEmail = "AuthorEmail",
                MergeA = "MergeA",
                MergeB = "MergeB",
                Changes = new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                        "SomeOriginalPath")
                }),
                CommitID = "CommitID",
                Summary = "Summary",
                Description = "Description",
                Time = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero)
            };

            gitLogEntry1.AssertNotEqual(gitLogEntry2);
        }
    }
}