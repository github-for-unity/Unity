using System;
using System.Globalization;

namespace GitHub.Unity
{
    public class HostAddress
    {
        public static HostAddress GitHubDotComHostAddress = new HostAddress();
        static readonly Uri gistUri = new Uri("https://gist.github.com");

        /// <summary>
        /// Creates a host address based on the hostUri based on the expected patterns for GitHub.com and
        /// GitHub Enterprise instances. The passed in URI can be any URL to the instance.
        /// </summary>
        /// <param name="hostUri">The URI to a GitHub or GitHub Enterprise instance.</param>
        /// <returns></returns>
        public static HostAddress Create(Uri hostUri)
        {
            return IsGitHubDotCom(hostUri)
                ? GitHubDotComHostAddress
                : new HostAddress(hostUri);
        }

        /// <summary>
        /// Creates a host address from a host name or URL as a string.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static HostAddress Create(string host)
        {
            Uri uri;
            if (Uri.TryCreate(host, UriKind.Absolute, out uri)
                   || Uri.TryCreate("https://" + host, UriKind.Absolute, out uri))
            {
                return Create(uri);
            }
            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                "The host '{0}' is not a valid host",
                host));
        }

        private HostAddress(Uri enterpriseUri)
        {
            WebUri = new Uri(enterpriseUri, new Uri("/", UriKind.Relative));
            ApiUri = new Uri(enterpriseUri, new Uri("/api/v3/", UriKind.Relative));
            CredentialCacheKeyHost = WebUri.ToString();
        }

        public HostAddress()
        {
            WebUri = new Uri("https://github.com");
            ApiUri = new Uri("https://api.github.com");
            CredentialCacheKeyHost = WebUri.ToString();
        }

        /// <summary>
        /// The Base URL to the host. For example, "https://github.com" or "https://ghe.io"
        /// </summary>
        public Uri WebUri { get; set; }

        /// <summary>
        /// The Base Url to the host's API endpoint. For example, "https://api.github.com" or
        ///  "https://ghe.io/api/v3"
        /// </summary>
        public Uri ApiUri { get; set; }

        // If the host name is "api.github.com" or "gist.github.com", we really only want "github.com",
        // since that's the same cache key for all the other github.com operations.
        public string CredentialCacheKeyHost { get; private set; }

        public static bool IsGitHubDotCom(Uri hostUri)
        {
            return hostUri.IsSameHost(GitHubDotComHostAddress.WebUri)
                   || hostUri.IsSameHost(GitHubDotComHostAddress.ApiUri)
                   || hostUri.IsSameHost(gistUri);
        }

        public static bool IsGitHubDotCom(string url)
        {
            if (String.IsNullOrEmpty(url))
                return false;
            Uri uri = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return false;
            return IsGitHubDotCom(uri);
        }

        public bool IsGitHubDotCom()
        {
            return IsGitHubDotCom(ApiUri);
        }

        public string Title
        {
            get {  return IsGitHubDotCom() ? "GitHub" : ApiUri.Host; }
        }

        public override int GetHashCode()
        {
            return (WebUri?.GetHashCode() ?? 0) ^ (ApiUri?.GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as HostAddress;
            return other != null && WebUri.IsSameHost(other.WebUri) && ApiUri.IsSameHost(other.ApiUri);
        }
    }
}
