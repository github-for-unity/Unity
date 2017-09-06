using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class SoftwareVersionTests
    {
        [Test]
        public void LessThanOrEqualTo()
        {
            (new SoftwareVersion(1, 1, 0) <= new SoftwareVersion(1, 1, 0)).Should().BeTrue();
            (new SoftwareVersion(1, 0, 0) <= new SoftwareVersion(1, 1, 0)).Should().BeTrue();
        }

        [Test]
        public void LessThan()
        {
            (new SoftwareVersion(1, 1, 0) < new SoftwareVersion(1, 1, 0)).Should().BeFalse();
            (new SoftwareVersion(1, 0, 0) < new SoftwareVersion(1, 1, 0)).Should().BeTrue();
        }
        [Test]
        public void GreaterThanOrEqualTo()
        {
            (new SoftwareVersion(1, 1, 0) >= new SoftwareVersion(1, 1, 0)).Should().BeTrue();
            (new SoftwareVersion(1, 0, 0) >= new SoftwareVersion(1, 1, 0)).Should().BeFalse();
        }

        [Test]
        public void GreaterThan()
        {
            (new SoftwareVersion(2, 12, 0) > new SoftwareVersion(2, 1, 0)).Should().BeTrue();
            (new SoftwareVersion(1, 1, 0) > new SoftwareVersion(1, 1, 0)).Should().BeFalse();
            (new SoftwareVersion(1, 0, 0) > new SoftwareVersion(1, 1, 0)).Should().BeFalse();
        }
    }
}
