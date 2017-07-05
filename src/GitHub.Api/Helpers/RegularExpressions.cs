using System.Text.RegularExpressions;

namespace GitHub.Unity.Helpers
{
    public static class RegularExpressions
    {
        public static readonly Regex BranchNameRegex = new Regex(@"^(?<name>[\w\d\/\-_]+)$");
    }

    public static class BranchNameValidator
    {
        public static bool IsBranchNameValid(string branchName)
        {
            return !string.IsNullOrEmpty(branchName)
                && !branchName.StartsWith("/")
                && !branchName.EndsWith("/")
                && RegularExpressions.BranchNameRegex.IsMatch(branchName);
        }
    }
}
