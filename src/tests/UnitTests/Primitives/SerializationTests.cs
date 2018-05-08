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
            var dt1 = DateTimeOffset.ParseExact("2018-05-01T12:04:29.0000000-02:00", Constants.Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var dt2 = DateTimeOffset.ParseExact("2018-05-01T12:04:29.000-02:00", Constants.Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var dt3 = DateTimeOffset.ParseExact("2018-05-01T12:04:29-02:00", Constants.Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var str1 = dt1.ToJson();
            var ret1 = str1.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt1, ret1);
            var str2 = dt2.ToJson();
            var ret2 = str2.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt2, ret2);
            var str3 = dt3.ToJson();
            var ret3 = str3.FromJson<DateTimeOffset>();
            Assert.AreEqual(dt3, ret3);

            Assert.AreEqual(dt1, dt2);
            Assert.AreEqual(dt2, dt3);
        }
    }
}
