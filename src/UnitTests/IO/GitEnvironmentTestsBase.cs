using System;
using System.IO;
using NSubstitute;
using GitHub.Unity;

namespace UnitTests
{
    public class GitEnvironmentTestsBase
    {
        //Intentionally returning object here
        protected object BuildFindRootFileSystem()
        {
            var filesystem = Substitute.For<IFileSystem>();

            filesystem
                .GetDirectoryName(Arg.Any<string>())
                .Returns(info => Path.GetDirectoryName((string) info[0]));

            filesystem.Combine(Arg.Any<string>(), Arg.Any<string>())
                .Returns(info => Path.Combine((string) info[0], (string) info[1]));

            filesystem.GetParentDirectory(Arg.Any<string>())
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

            filesystem.DirectoryExists(Arg.Any<string>())
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
            return filesystem;
        }
    }
}