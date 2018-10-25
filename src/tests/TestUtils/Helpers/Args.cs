using System;
using System.IO;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using System.Collections.Generic;

namespace TestUtils
{
    static class Args
    {
        public static string String => Arg.Any<string>();
        public static bool Bool => Arg.Any<bool>();
        public static int Int => Arg.Any<int>();
        public static UriString UriString => Arg.Any<UriString>();
        public static SearchOption SearchOption => Arg.Any<SearchOption>();
        public static GitFileStatus GitFileStatus => Arg.Any<GitFileStatus>();
        public static GitConfigSource GitConfigSource => Arg.Any<GitConfigSource>();
        public static List<GitLogEntry> GitLogs => Arg.Any<List<GitLogEntry>>();
        public static GitAheadBehindStatus GitAheadBehindStatus => Arg.Any<GitAheadBehindStatus>();
        public static GitStatus GitStatus => Arg.Any<GitStatus>();
        public static List<GitLock> GitLocks => Arg.Any<List<GitLock>>();
        public static IEnumerable<GitLock> EnumerableGitLock => Arg.Any<IEnumerable<GitLock>>();
        public static IUser User => Arg.Any<IUser>();
        public static ConfigBranch? NullableConfigBranch => Arg.Any<ConfigBranch?>();
        public static ConfigRemote? NullableConfigRemote => Arg.Any<ConfigRemote?>();
        public static Dictionary<string, ConfigBranch> LocalBranchDictionary => Arg.Any<Dictionary<string, ConfigBranch>>();
        public static Dictionary<string, ConfigRemote> RemoteDictionary => Arg.Any<Dictionary<string, ConfigRemote>>();
        public static Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranchDictionary => Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>();
        public static CacheType CacheType => Arg.Any<CacheType>();
        public static DateTimeOffset DateTimeOffset => Arg.Any<DateTimeOffset>();

        public static ITask<GitStatus?> GitStatusTask
        {
            get
            {
                var task = Substitute.For<ITask<GitStatus?>>();
                task.Affinity.Returns(TaskAffinity.Exclusive);
                return task;
            }
        }
        public static ITask<List<GitLock>> GitListLocksTask
        {
            get
            {
                var task = Substitute.For<ITask<List<GitLock>>>();
                task.Affinity.Returns(TaskAffinity.Concurrent);
                return task;
            }
        }
    }
}
