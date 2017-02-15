using System.Threading;

namespace GitHub.Api
{
    interface ISharpZipLibHelper
    {
        void ExtractZipFile(string archive, string outFolder, CancellationToken? cancellationToken = null);
    }
}