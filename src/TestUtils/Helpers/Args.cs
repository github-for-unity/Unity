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
        public static SearchOption SearchOption { get { return Arg.Any<SearchOption>(); } }
        public static GitFileStatus GitFileStatus { get { return Arg.Any<GitFileStatus>(); } }
        public static GitConfigSource GitConfigSource { get { return Arg.Any<GitConfigSource>(); } }
        public static GitStatus GitStatus { get { return Arg.Any<GitStatus>(); } }
        public static IEnumerable<GitLock> EnumerableGitLock { get { return Arg.Any<IEnumerable<GitLock>>(); } }
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