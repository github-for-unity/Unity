using System;

namespace GitHub.Api
{
    /// <summary>
    /// Represents a repository, either local or retreived via the GitHub API.
    /// </summary>
    interface IRepository : IEquatable<IRepository>
    {
        /// <summary>
        /// Gets the name of the repository.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the repository clone URL.
        /// </summary>
        UriString CloneUrl { get; }

        /// <summary>
        /// Gets the name of the owner of the repository, taken from the clone URL.
        /// </summary>
        string Owner { get; }

        /// <summary>
        /// Gets the local path of the repository.
        /// </summary>
        string LocalPath { get; }
        bool IsGitHub { get; }
        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        string CurrentRemote { get; }
        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        string CurrentBranch { get; }
    }
}