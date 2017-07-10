using TestUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
            };

            gitLogEntry.AssertEqual(gitLogEntry);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsNull()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
            };

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldNotEqualAnotherWhenFieldsAreDifferent()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
            };

            gitLogEntry1.AssertNotEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsEmpty()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
            };

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsNotEmpty()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime,
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
            };

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldNotEqualAnotherWhenChangesAreDifferent()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
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
                TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                TimeValue = commitTime,
                CommitTimeValue = commitTime
            };

            gitLogEntry1.AssertNotEqual(gitLogEntry2);
        }
    }
}