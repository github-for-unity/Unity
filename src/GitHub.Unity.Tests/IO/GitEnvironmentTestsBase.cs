using System;
using System.IO;
using NSubstitute;

namespace GitHub.Unity.Tests
{
    public class GitEnvironmentTestsBase
    {
        //Intentionally returning object here
        protected object BuildFindRootFileSystem()
        {
            var fileSystem = Substitute.For<IFileSystem>();

            fileSystem
                .GetDirectoryName(Arg.Any<string>())
                .Returns(info => Path.GetDirectoryName((string) info[0]));

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>())
                .Returns(info => Path.Combine((string) info[0], (string) info[1]));

            fileSystem.GetParentDirectory(Arg.Any<string>())
                .Returns(info =>
                {
                    switch ((string) info[0])
                    {
                        case @"c:\Source\file.txt":
                            return  @"c:\Source";

                        case @"c:\Documents\file.txt":
                            return @"c:\Documents";

                        case @"c:\Source":
                        case @"c:\Documents":
                        case @"c:\file.txt":
                            return @"c:";

                        case @"c:":
                            return null;

                        default:
                            throw new ArgumentException();
                    }
                });

            fileSystem.DirectoryExists(Arg.Any<string>())
                .Returns(info =>
                {
                    switch ((string) info[0])
                    {
                        case @"c:\Source\.git":
                            return true;

                        default:
                            return false;
                    }
                });
            return fileSystem;
        }
    }
}