using System;
using System.Threading;

namespace GitHub.Api
{
    interface IZipHelper
    {
        void ExtractZipFile(string archive, string outFolder, CancellationToken? cancellationToken = null,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null);
    }
}
