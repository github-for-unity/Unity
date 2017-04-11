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
        public static ITask<GitStatus?> GitStatusTask
        {
            get
            {
                var task = Substitute.For<ITask<GitStatus?>>();
                task.Done.Returns(true);
                task.Queued.Returns(TaskQueueSetting.Queue);
                task.Run(Arg.Any<CancellationToken>());
                task.RunAsync(Arg.Any<CancellationToken>());
                return task;
            }
        }
        public static ITask<IEnumerable<GitLock>> GitListLocksTask
        {
            get
            {
                var task = Substitute.For<ITask<IEnumerable<GitLock>>>();
                task.Done.Returns(true);
                task.Queued.Returns(TaskQueueSetting.Queue);
                task.Run(Arg.Any<CancellationToken>());
                task.RunAsync(Arg.Any<CancellationToken>());
                return task;
            }
        }
    }
}