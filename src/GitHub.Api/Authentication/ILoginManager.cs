using System.Threading.Tasks;
using Octokit;

namespace GitHub.Api
{
    /// <summary>
    /// Provides services for logging into a GitHub server.
    /// </summary>
    interface ILoginManager
    {
        /// <summary>
        /// Attempts to log into a GitHub server.
        /// </summary>
        /// <param name="hostAddress">The address of the server.</param>
        /// <param name="client">An octokit client configured to access the server.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The logged in user.</returns>
        /// <exception cref="AuthorizationException">
        /// The login authorization failed.
        /// </exception>
        Task<LoginResultData> Login(HostAddress hostAddress, IGitHubClient client, string username, string password);
        Task<LoginResultData> ContinueLogin(LoginResultData loginResultData, string twofacode);

        /// <summary>
        /// Attempts to log into a GitHub server using existing credentials.
        /// </summary>
        /// <param name="hostAddress">The address of the server.</param>
        /// <param name="client">An octokit client configured to access the server.</param>
        /// <returns>The logged in user.</returns>
        /// <exception cref="AuthorizationException">
        /// The login authorization failed.
        /// </exception>
        Task<User> LoginFromCache(HostAddress hostAddress, IGitHubClient client);

        /// <summary>
        /// Logs out of GitHub server.
        /// </summary>
        /// <param name="hostAddress">The address of the server.</param>
        Task Logout(HostAddress hostAddress, IGitHubClient client);
    }
}