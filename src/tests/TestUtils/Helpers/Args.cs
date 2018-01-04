using System.IO;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using System.Collections.Generic;

namespace TestUtils
{
    static class Args
    {
        public static string String { get { return Arg.Any<string>(); } }
        public static bool Bool { get { return Arg.Any<bool>(); } }
        public static int Int { get { return Arg.Any<int>(); } }
        public static UriString UriString { get { return Arg.Any<UriString>(); } }
        public static SearchOption SearchOption { get { return Arg.Any<SearchOption>(); } }
        public static GitFileStatus GitFileStatus { get { return Arg.Any<GitFileStatus>(); } }
        public static GitConfigSource GitConfigSource { get { return Arg.Any<GitConfigSource>(); } }
        public static List<GitLogEntry> GitLogs { get { return Arg.Any<List<GitLogEntry>>(); } }
        public static GitAheadBehindStatus GitAheadBehindStatus { get { return Arg.Any<GitAheadBehindStatus>(); } }
        public static GitStatus GitStatus { get { return Arg.Any<GitStatus>(); } }
        public static List<GitLock> GitLocks { get { return Arg.Any<List<GitLock>>(); } }
        public static IEnumerable<GitLock> EnumerableGitLock { get { return Arg.Any<IEnumerable<GitLock>>(); } }
        public static IUser User { get { return Arg.Any<IUser>(); } }
        public static ConfigBranch? NullableConfigBranch { get { return Arg.Any<ConfigBranch?>(); } }
        public static ConfigRemote? NullableConfigRemote { get { return Arg.Any<ConfigRemote?>(); } }
        public static Dictionary<string, ConfigBranch> LocalBranchDictionary { get { return Arg.Any<Dictionary<string, ConfigBranch>>(); } }
        public static Dictionary<string, ConfigRemote> RemoteDictionary { get { return Arg.Any<Dictionary<string, ConfigRemote>>(); } }
        public static Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranchDictionary { get { return Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>(); } }

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