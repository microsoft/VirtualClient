// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class DataTableExtensionsTests
    {
        [Test]
        public void GetMetricExtensionParsesExpectedMetricNamesAndValuesFromDataTableRows()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("Id", typeof(Guid)));
            table.Columns.Add(new DataColumn("MetricName", typeof(string)));
            table.Columns.Add(new DataColumn("MetricValue", typeof(double)));

            table.Rows.Add(Guid.NewGuid(), "Metric1", 1234);
            table.Rows.Add(Guid.NewGuid(), "Metric2", 9876);

            IEnumerable<Metric> metrics = table.GetMetrics(nameIndex: 1, valueIndex: 2);

            Assert.AreEqual(2, metrics.Count());
            Assert.IsTrue(metrics.ElementAt(0).Name == "Metric1");
            Assert.IsTrue(metrics.ElementAt(0).Value == 1234);
            Assert.IsTrue(metrics.ElementAt(1).Name == "Metric2");
            Assert.IsTrue(metrics.ElementAt(1).Value == 9876);
        }

        [Test]
        public void GetMetricExtensionHandlesDBNullValuesInDataTableRowValues()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("Id", typeof(Guid)));
            table.Columns.Add(new DataColumn("MetricName", typeof(string)));
            table.Columns.Add(new DataColumn("MetricValue", typeof(double)));

            table.Rows.Add(Guid.NewGuid(), "Metric1", 1234);
            table.Rows.Add(Guid.NewGuid(), "Metric2", DBNull.Value);

            IEnumerable<Metric> metrics = table.GetMetrics(nameIndex: 1, valueIndex: 2);

            Assert.AreEqual(1, metrics.Count());
            Assert.IsTrue(metrics.First().Name == "Metric1");
            Assert.IsTrue(metrics.First().Value == 1234);
        }
    }
}
