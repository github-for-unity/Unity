﻿using System;
using System.Threading;

namespace GitHub.Unity
{
    interface IZipHelper
    {
        void Extract(string archive, string outFolder, CancellationToken cancellationToken, IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null);
    }
}
