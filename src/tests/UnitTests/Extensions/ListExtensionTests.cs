using System.Linq;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class ListExtensionTests
    {
        [Test]
        public void SpoolTest()
        {
            var results = new[] { "qwer", "asdf", "zxcv", "wert", "1234" }.Spool(10).ToArray();
            results.Length.Should().Be(3);
            results[0].Count.Should().Be(2);
            results[0][0].Should().Be("qwer");
            results[0][1].Should().Be("asdf");

            results[1].Count.Should().Be(2);
            results[1][0].Should().Be("zxcv");
            results[1][1].Should().Be("wert");

            results[2].Count.Should().Be(1);
            results[2][0].Should().Be("1234");
        }
    }
}