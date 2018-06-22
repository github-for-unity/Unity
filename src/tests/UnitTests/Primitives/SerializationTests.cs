using GitHub.Unity;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UnitTests.Primitives
{
    [TestFixture]
    class SerializationTests
    {
        [TestCase("2018-05-01T12:04:29.1234567-02:00", "2018-05-01T14:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.123456-02:00", "2018-05-01T14:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.12345-02:00", "2018-05-01T14:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.1234-02:00", "2018-05-01T14:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.123-02:00", "2018-05-01T14:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.12-02:00", "2018-05-01T14:04:29.120+00:00")]
        [TestCase("2018-05-01T12:04:29.1-02:00", "2018-05-01T14:04:29.100+00:00")]
        [TestCase("2018-05-01T12:04:29-02:00", "2018-05-01T14:04:29.000+00:00")]
        [TestCase("2018-05-01T12:04:29.1234567Z", "2018-05-01T12:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.123456Z", "2018-05-01T12:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.12345Z", "2018-05-01T12:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.1234Z", "2018-05-01T12:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.123Z", "2018-05-01T12:04:29.123+00:00")]
        [TestCase("2018-05-01T12:04:29.12Z", "2018-05-01T12:04:29.120+00:00")]
        [TestCase("2018-05-01T12:04:29.1Z", "2018-05-01T12:04:29.100+00:00")]
        [TestCase("2018-05-01T12:04:29Z", "2018-05-01T12:04:29.000+00:00")]
        public void FromLocalStringToUniversalDateTimeOffset(string input, string expected)
        {
            var dtInput = DateTimeOffset.ParseExact(input, Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var output = dtInput.ToUniversalTime().ToString(Constants.Iso8601Format);
            Assert.AreEqual(expected, output);

            var json = $@"{{""date"":""{input}""}}";
            Assert.DoesNotThrow(() => json.FromJson<ADateTimeOffset>(lowerCase: true));
        }

        [Test]
        public void JsonSerializationUsesKnownFormat()
        {
            var now = DateTimeOffset.Now;
            var output = new ADateTimeOffset { Date = now };
            var json = output.ToJson(lowerCase: true);
            Assert.AreEqual($@"{{""date"":""{ now.ToUniversalTime().ToString(Constants.Iso8601Format, CultureInfo.InvariantCulture) }""}}", json);
        }

        [Test]
        public void DateTimeSerializationRoundTrip()
        {
            var dt1 = DateTimeOffset.ParseExact("2018-05-01T12:04:29.0000000-02:00", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var dt2 = DateTimeOffset.ParseExact("2018-05-01T12:04:29.000-02:00", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var dt3 = DateTimeOffset.ParseExact("2018-05-01T12:04:29-02:00", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var stru = dt1.ToUniversalTime().ToString(Constants.Iso8601FormatZ);
            var dt4 = DateTimeOffset.ParseExact(stru, Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var str1 = dt1.ToJson();
            var ret1 = str1.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt1, ret1);
            var str2 = dt2.ToJson();
            var ret2 = str2.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt2, ret2);
            var str3 = dt3.ToJson();
            var ret3 = str3.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt3, ret3);
            var str4 = dt4.ToJson();
            var ret4 = str4.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt4, ret4);

            Assert.AreEqual(dt1, dt2);
            Assert.AreEqual(dt2, dt3);
            Assert.AreEqual(dt3, dt4);
        }

        [Test]
        public void DateTimeSerializationRoundTripFormatPointZ()
        {
            var dt1 = DateTimeOffset.ParseExact("2018-05-01T15:04:29.00Z", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var str1 = dt1.ToJson();
            var ret1 = str1.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt1, ret1);

            var dt2 = DateTimeOffset.ParseExact("2018-05-01T15:04:29Z", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var str2 = dt2.ToJson();
            var ret2 = str2.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt2, ret2);

            Assert.AreEqual(dt1, dt2);
        }

        class ADateTimeOffset
        {
            public DateTimeOffset Date;
        }

        class TestData
        {
            public List<string> Things { get; set; } = new List<string>();
        }

        [Test]
        public void ListDeserializesCorrectly()
        {
            var data = new TestData();
            data.Things.Add("something");
            var json = data.ToJson();
            var ret = json.FromJson<TestData>();
            CollectionAssert.AreEquivalent(data.Things, ret.Things);
        }
    }
}
