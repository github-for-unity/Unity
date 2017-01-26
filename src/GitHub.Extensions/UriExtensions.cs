using System;

namespace GitHub.Extensions
{
    public static class UriExtensions
    {
        /// <summary>
        /// Appends a relative path to the URL.
        /// </summary>
        /// <remarks>
        /// The Uri constructor for combining relative URLs have a different behavior with URLs that end with /
        /// than those that don't.
        /// </remarks>
        public static Uri Append(this Uri uri, string relativePath)
        {
            if (!uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                uri = new Uri(uri + "/");
            }
            return new Uri(uri, new Uri(relativePath, UriKind.Relative));
        }

        public static bool IsHypertextTransferProtocol(this Uri uri)
        {
            return uri.Scheme == "http" || uri.Scheme == "https";
        }

        public static bool IsSameHost(this Uri uri, Uri compareUri)
        {
            return uri.Host.Equals(compareUri.Host, StringComparison.OrdinalIgnoreCase);
        }
    }
}
