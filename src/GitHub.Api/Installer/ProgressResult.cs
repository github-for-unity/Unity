using System.Globalization;
using System.Text.RegularExpressions;

namespace GitHub.Helpers
{
    public class ProgressResult
    {
        public ProgressResult() { }
        public ProgressResult(int value) : this(value, string.Empty) { }
        public ProgressResult(int value, string text)
        {
            ProgressValue = value;
            ProgressText = text;
        }

        public int ProgressValue { get; private set; }
        public string ProgressText { get; private set; }

        static readonly Regex userFacingCloneMessageRegex = new Regex(
            @"^(Cloning into \'|Receiving objects\:|Resolving deltas\:|Checking out files\:|Fetching from upstream repo \')",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public bool IsUserFacingMessage
        {
            get { return userFacingCloneMessageRegex.IsMatch(ProgressText); }
        }

//        public override string ToString()
//        {
//            return string.IsNullOrWhiteSpace(ProgressText)
//                ? ProgressValue.ToString(CultureInfo.InvariantCulture)
//                : string.Format(CultureInfo.InvariantCulture, "{0} - '{1}'", ProgressValue, ProgressText);
//        }
    }
}