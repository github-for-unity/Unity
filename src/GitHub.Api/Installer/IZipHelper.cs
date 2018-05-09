using System;
using System.Threading;

namespace GitHub.Unity
{
    interface IZipHelper
    {
        bool Extract(string archive, string outFolder, CancellationToken cancellationToken,
            Func<long, long, bool> onProgress, Func<string, bool> onFilter = null);
    }
}
