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
            var dt1 = DateTimeOffset.ParseExact("2018-05-01T15:04:29.00Z", new []{ Constants.Iso8601FormatPointZ }, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            DateTimeOffset.ParseExact("2018-05-01T15:04:29.00Z", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var str1 = dt1.ToJson();
            var ret1 = str1.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt1, ret1);
        }

        [Test]
        public void DateTimeSerializationRoundTripFormatZ()
        {
            var dt1 = DateTimeOffset.ParseExact("2018-05-01T15:04:29Z", Constants.Iso8601Formats, CultureInfo.InvariantCulture, Constants.DateTimeStyle);
            var str1 = dt1.ToJson();
            var ret1 = str1.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt1, ret1);
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
