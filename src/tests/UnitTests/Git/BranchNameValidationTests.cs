using System;
using System.Linq;
using FluentAssertions;
using GitHub.Unity.Helpers;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class BranchNameValidatorTests
    {
        [TestCase(true, "feature1", TestName = "Branch name is valid")]
        [TestCase(true, "feature-1", TestName = "Branch name with hyphen is valid")]
        [TestCase(true, "feature.1", TestName = "Branch name can contain dots")]
        [TestCase(true, "feature..1", TestName = "Branch name cannot contain consecutive dots")]
        [TestCase(false, "feature 1", TestName = "Branch name cannot contain a space")]
        [TestCase(false, "feature~1", TestName = "Branch name cannot contain a ~")]
        [TestCase(false, "feature^1", TestName = "Branch name cannot contain a ^")]
        [TestCase(false, "feature:1", TestName = "Branch name cannot contain a :")]
        [TestCase(false, "feature?1", TestName = "Branch name cannot contain a ?")]
        [TestCase(false, "feature*1", TestName = "Branch name cannot contain a *")]
        [TestCase(false, "feature[1", TestName = "Branch name cannot contain a [")]
        [TestCase(false, "/feature1", TestName = "Branch name cannot begin with a slash")]
        [TestCase(false, "feature1/", TestName = "Branch name cannot end with a slash")]
        [TestCase(false, "feature1.", TestName = "Branch name cannot end with a dot")]
        [TestCase(false, "feature1.lock", TestName = "Branch name cannot end with .lock")]
        [TestCase(true, "a", TestName = "Single character is valid")]
        [TestCase(false, "@", TestName = "Single character cannot be @")]
        [TestCase(false, ".", TestName = "Single character cannot be [period]")]
        [TestCase(true, "features/feature-1", TestName = "Folder and branch name is valid")]
        [TestCase(true, "features\\feature-1", TestName = "Backslash is not a valid character")]
        [TestCase(true, ".hidden", TestName = "Branch name is valid when starting with [period]")]
        [TestCase(false, ".features/feature-1", TestName = "Folder and branch name is not valid when starting with [period]")]
        [TestCase(false, "features//feature-1", TestName = "Multiple consecutive slashes are not valid")]
        [TestCase(false, null, TestName = "null string is not valid")]
        [TestCase(false, "", TestName = "Empty string is not valid")]
        [TestCase(false, "/", TestName = "Single slash is not valid")]
        [TestCase(false, "asdf@{", TestName = "Sequence @{ is not valid")]
        public void TestFeatureString(bool isValid, string branch)
        {
            BranchNameValidator.IsBranchNameValid(branch).Should().Be(isValid);
        }

        [TestCase(true, 45, 45, 45, TestName = "Can test with ascii values")]
        [TestCase(false, 45, 45, 39, TestName = "No individual ASCII value should be < octal(40)")]
        [TestCase(false, 45, 45, 177, TestName = "No individual ASCII value should = octal(177)")]
        public void TestFeatureStringFromAsciiArray(bool isValid, params int[] asciiValues)
        {
            var branch = new string(asciiValues.Select(Convert.ToChar).ToArray());
            BranchNameValidator.IsBranchNameValid(branch).Should().Be(isValid);
        }
    }
}
