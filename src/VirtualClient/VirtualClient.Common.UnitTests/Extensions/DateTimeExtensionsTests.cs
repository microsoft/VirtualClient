// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class DateTimeExtensionsTests
    {
        [Test]
        public void SafeAddExtensionReturnsTheExpectedDateTimeValue()
        {
            DateTime time1 = DateTime.Parse("2023-04-25T00:00:00.0000000");

            DateTime time2 = time1.SafeAdd(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(DateTime.Parse("2023-04-25T00:00:00.5000000"), time2);

            DateTime time3 = time1.SafeAdd(TimeSpan.FromSeconds(10));
            Assert.AreEqual(DateTime.Parse("2023-04-25T00:00:10.0000000"), time3);

            DateTime time4 = time1.SafeAdd(TimeSpan.FromMinutes(20));
            Assert.AreEqual(DateTime.Parse("2023-04-25T00:20:00.0000000"), time4);

            DateTime time5 = time1.SafeAdd(TimeSpan.FromHours(5));
            Assert.AreEqual(DateTime.Parse("2023-04-25T05:00:00.0000000"), time5);

            DateTime time6 = time1.SafeAdd(TimeSpan.FromDays(25));
            Assert.AreEqual(DateTime.Parse("2023-05-20T00:00:00.0000000"), time6);
        }

        [Test]
        public void SafeAddExtensionHandlesOutOfRangeExceptions()
        {
            DateTime time = DateTime.MaxValue.SafeAdd(TimeSpan.MaxValue);
            Assert.AreEqual(DateTime.MaxValue, time);
        }

        [Test]
        public void SafeSubtractExtensionReturnsTheExpectedDateTimeValue()
        {
            DateTime time1 = DateTime.Parse("2023-04-25T01:00:00.0000000");

            TimeSpan timespan1 = time1.SafeSubtract(DateTime.Parse("2023-04-25T00:59:59.5000000"));
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), timespan1);
        }
    }
}