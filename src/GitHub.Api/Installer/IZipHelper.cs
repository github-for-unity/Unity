using System;
using System.Threading;

namespace GitHub.Unity
{
    interface IZipHelper
    {
        void ExtractZipFile(string archive, string outFolder, CancellationToken? cancellationToken = null,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null);
    }
}
