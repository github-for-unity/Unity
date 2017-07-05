using System.Text.RegularExpressions;

namespace GitHub.Unity.Helpers
{
    public static class RegularExpressions
    {
        public static readonly Regex BranchNameRegex = new Regex(@"^(?<name>[\.\w\d\/\-_]+)$");
    }

    public static class BranchNameValidator
    {
        public static bool IsBranchNameValid(string branchName)
        {
            return !string.IsNullOrEmpty(branchName)
                && !branchName.Equals(".")
                && !branchName.StartsWith("/")
                && !branchName.EndsWith("/")
                && !branchName.EndsWith(".")
                && !branchName.Contains("//")
                && !branchName.Contains(@"\")
                && !branchName.EndsWith(".lock")
                && !(branchName.StartsWith(".") && branchName.Contains("/"))
                && RegularExpressions.BranchNameRegex.IsMatch(branchName);
        }
    }
}
