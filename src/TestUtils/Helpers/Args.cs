using System.IO;
using GitHub.Unity;
using NSubstitute;

namespace TestUtils
{
    static class Args
    {
        public static string String { get { return Arg.Any<string>(); } }
        public static bool Bool { get { return Arg.Any<bool>(); } }
        public static SearchOption SearchOption { get { return Arg.Any<SearchOption>(); } }
        public static GitFileStatus GitFileStatus { get { return Arg.Any<GitFileStatus>(); } }
        public static GitConfigSource GitConfigSource { get { return Arg.Any<GitConfigSource>(); } }
        public static GitStatus GitStatus { get { return Arg.Any<GitStatus>(); } }
    }
}