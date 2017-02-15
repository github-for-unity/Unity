using System;
using System.Diagnostics;
using System.Globalization;

namespace GitHub.Api
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Repository : IRepository, IEquatable<Repository>
    {
        private readonly IGitClient gitClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryModel"/> class.
        /// </summary>
        /// <param name="gitClient"></param>
        /// <param name="name">The repository name.</param>
        /// <param name="cloneUrl">The repository's clone URL.</param>
        /// <param name="localPath"></param>
        public Repository(IGitClient gitClient, string name, UriString cloneUrl, string localPath)
        {
            Guard.ArgumentNotNull(gitClient, nameof(gitClient));
            Guard.ArgumentNotNullOrWhiteSpace(name, nameof(name));
            Guard.ArgumentNotNull(cloneUrl, nameof(cloneUrl));

            this.gitClient = gitClient;
            Name = name;
            CloneUrl = cloneUrl;
            LocalPath = localPath;
        }

        /// <summary>
        /// Note: We don't consider CloneUrl a part of the hash code because it can change during the lifetime
        /// of a repository. Equals takes care of any hash collisions because of this
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 17 * 23 + (Name?.GetHashCode() ?? 0) * 23 + (Owner?.GetHashCode() ?? 0) * 23 + (LocalPath?.TrimEnd('\\').ToUpperInvariant().GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Repository;
            return Equals(other);
        }

        public bool Equals(Repository other)
        {
            return (Equals((IRepository)other));
        }

        public bool Equals(IRepository other)
        {
            if (ReferenceEquals(this, other))
                return true;
            return other != null &&
                String.Equals(Name, other.Name) &&
                String.Equals(Owner, other.Owner) &&
                String.Equals(CloneUrl, other.CloneUrl) &&
                String.Equals(LocalPath?.TrimEnd('\\'), other.LocalPath?.TrimEnd('\\'), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        public string CurrentBranch
        {
            get
            {
                return gitClient.GetActiveBranch()?.Name;
            }
        }

        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        public string CurrentRemote
        {
            get
            {
                return gitClient.GetActiveRemote()?.Name;
            }
        }

        public string Name { get; private set; }
        public UriString CloneUrl { get; private set; }
        public string LocalPath { get; private set; }
        public string Owner => CloneUrl?.Owner ?? string.Empty;
        public bool IsGitHub { get { return CloneUrl != ""; } }

        internal string DebuggerDisplay => String.Format(
            CultureInfo.InvariantCulture,
            "{4}\tOwner: {0} Name: {1} CloneUrl: {2} LocalPath: {3}",
            Owner,
            Name,
            CloneUrl,
            LocalPath,
            GetHashCode());
    }
}