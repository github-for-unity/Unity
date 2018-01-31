using System;

namespace GitHub.Unity
{
    public struct ValidateGitInstallResult
    {
        public bool IsValid;
        public Version GitVersion;
        public Version GitLfsVersion;

        public ValidateGitInstallResult(bool isValid, Version gitVersion, Version gitLfsVersion)
        {
            IsValid = isValid;
            GitVersion = gitVersion;
            GitLfsVersion = gitLfsVersion;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + IsValid.GetHashCode();
            hash = hash * 23 + (GitVersion?.GetHashCode() ?? 0);
            hash = hash * 23 + (GitLfsVersion?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is ValidateGitInstallResult)
                return Equals((ValidateGitInstallResult)other);
            return false;
        }

        public bool Equals(ValidateGitInstallResult other)
        {
            return IsValid == other.IsValid &&
                object.Equals(GitVersion, other.GitVersion) &&
                object.Equals(GitLfsVersion, other.GitLfsVersion)
                ;
        }

        public static bool operator ==(ValidateGitInstallResult lhs, ValidateGitInstallResult rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ValidateGitInstallResult lhs, ValidateGitInstallResult rhs)
        {
            return !(lhs == rhs);
        }
    }
}