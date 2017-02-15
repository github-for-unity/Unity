using NSubstitute;

namespace GitHub.Unity.Tests
{
    public class BaseOutputProcessorTests
    {
        protected const string TestRootPath = @"c:\TestSource";

        internal IGitObjectFactory CreateGitObjectFactory()
        {
            var gitObjectFactory = Substitute.For<IGitObjectFactory>();

            gitObjectFactory.CreateGitStatusEntry(Arg.Any<string>(), Arg.Any<GitFileStatus>(), Arg.Any<string>(), Arg.Any<bool>())
                                 .Returns(info => {
                                     var path = (string)info[0];
                                     var status = (GitFileStatus)info[1];
                                     var originalPath = (string)info[2];
                                     var staged = (bool)info[3];

                                     return new GitStatusEntry(path, TestRootPath + @"\" + path, null, status,
                                         originalPath, staged);
                                 });

            gitObjectFactory.CreateGitLock(Arg.Any<string>(), Arg.Any<string>())
                                 .Returns(info => {
                                     var path = (string)info[0];
                                     var user = (string)info[1];

                                     return new GitLock(path, TestRootPath + @"\" + path, user);
                                 });

            return gitObjectFactory;
        }
    }
}
