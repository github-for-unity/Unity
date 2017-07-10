using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity.Helpers
{
    public static class Validation
    {
        public static bool IsBranchNameValid(string branchName)
        {
            return !String.IsNullOrEmpty(branchName)
                && !branchName.Equals(".")
                && !branchName.StartsWith("/")
                && !branchName.EndsWith("/")
                && !branchName.StartsWith(".")
                && !branchName.EndsWith(".")
                && !branchName.Contains("//")
                && !branchName.Contains(@"\")
                && !branchName.EndsWith(".lock")
                && BranchNameRegex.IsMatch(branchName);
        }

        public static readonly Regex BranchNameRegex = new Regex(@"^(?<name>[\.\w\d\/\-_]+)$");
    }
}
