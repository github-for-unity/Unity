using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.Logging;

namespace GitHub.Unity
{
    public static class CopyHelper
    {
        private static readonly ILogging Logger = LogHelper.GetLogger(typeof(CopyHelper));

        public static void Copy(NPath fromPath, NPath toPath)
        {
            Logger.Trace("Copying from {0} to {1}", fromPath, toPath);
            
            try
            {
                CopyFolder(fromPath, toPath);
            }
            catch (Exception ex1)
            {
                Logger.Warning(ex1, "Error copying.");

                try
                {
                    CopyFolderContents(fromPath, toPath);
                }
                catch (Exception ex2)
                {
                    Logger.Error(ex2, "Error copying contents.");
                    throw;
                }
            }
            finally
            {
                fromPath.DeleteIfExists();
            }
        }
        public static void CopyFolder(NPath fromPath, NPath toPath)
        {
            Logger.Trace("CopyFolder from {0} to {1}", fromPath, toPath);
            toPath.DeleteIfExists();
            toPath.EnsureParentDirectoryExists();
            fromPath.Move(toPath);
        }

        public static void CopyFolderContents(NPath fromPath, NPath toPath)
        {
            Logger.Trace("CopyFolder Contents from {0} to {1}", fromPath, toPath);
            toPath.DeleteContents();
            fromPath.MoveFiles(toPath, true);
        }
    }
}
