using System;
using System.Threading;

namespace GitHub.Unity
{
    interface IZipHelper
    {
        void Extract(string archive, string outFolder, CancellationToken cancellationToken,
            Func<long, long, bool> onProgress = null);
    }
}
