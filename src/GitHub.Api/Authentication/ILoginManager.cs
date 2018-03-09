using System.Threading.Tasks;

namespace GitHub.Unity
{
    /// <summary>
    /// Provides services for logging into a GitHub server.
    /// </summary>
    interface ILoginManager
    {
        /// <summary>
        /// Attempts to log into a GitHub server.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The logged in user.</returns>
        /// <exception cref="AuthorizationException">
        /// The login authorization failed.
        /// </exception>
        Task<LoginResultData> Login(UriString host, string username, string password);
        Task<LoginResultData> ContinueLogin(LoginResultData loginResultData, string twofacode);

        /// <summary>
        /// Logs out of GitHub server.
        /// </summary>
        /// <param name="hostAddress">The address of the server.</param>
        /// <inheritdoc/>
        Task Logout(UriString hostAddress);
    }
}