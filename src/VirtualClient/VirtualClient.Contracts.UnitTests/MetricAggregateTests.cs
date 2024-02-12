// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class MetricAggregateTests
    {
        [Test]
        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("   ")]
        public void MetricAggregateConstructorsValidatesRequiredParameters(string invalidArgument)
        {
            Assert.Throws<ArgumentException>(() => new MetricAggregate(invalidArgument));
        }

        [Test]
        public void MetricAggregateConstructorsSetPropertiesToExpectedValues()
        {
            string expectedMetricName = "AnyName";
            string expectedMetricUnit = "any/sec";
            MetricAggregateType expectedAggregateType = MetricAggregateType.Raw;

            MetricAggregate metricAggregate = new MetricAggregate(expectedMetricName);
            Assert.AreEqual(expectedMetricName, metricAggregate.Name);
            Assert.IsNull(metricAggregate.Unit);
            Assert.AreEqual(MetricAggregateType.Average, metricAggregate.AggregateType);

            metricAggregate = new MetricAggregate(expectedMetricName, expectedMetricUnit);
            Assert.AreEqual(expectedMetricName, metricAggregate.Name);
            Assert.AreEqual(expectedMetricUnit, metricAggregate.Unit);
            Assert.AreEqual(MetricAggregateType.Average, metricAggregate.AggregateType);

            metricAggregate = new MetricAggregate(expectedMetricName, expectedMetricUnit, expectedAggregateType);
            Assert.AreEqual(expectedMetricName, metricAggregate.Name);
            Assert.AreEqual(expectedMetricUnit, metricAggregate.Unit);
            Assert.AreEqual(expectedAggregateType, metricAggregate.AggregateType);
        }

        [Test]
        public void MetricAggregateDefaultAggregationTypeIsToCalculateAnAverage()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric");
            Assert.AreEqual(MetricAggregateType.Average, metricAggregate.AggregateType);

            metricAggregate = new MetricAggregate("AnyMetric", "AnyUnit");
            Assert.AreEqual(MetricAggregateType.Average, metricAggregate.AggregateType);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Averages_1()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Average)
            {
                0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(500, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Averages_2()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric")
            {
                0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric(MetricAggregateType.Average);

            Assert.AreEqual(500, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Mins_1()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Min)
            {
                2000, 1000, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(100, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Mins_2()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Min)
            {
                2000, 1000, 100, 100, 100, 400, 500, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(100, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Mins_3()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric")
            {
                2000, 1000, 100, 100, 100, 400, 500, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric(MetricAggregateType.Min);

            Assert.AreEqual(100, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Max_1()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Max)
            {
                1000, 100, 200, 300, 400, 1001, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(1001, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Max_2()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Max)
            {
                1000, 100, 200, 300, 400, 1001, 1001, 1001, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(1001, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Max_3()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric")
            {
                1000, 100, 200, 300, 400, 1001, 1001, 1001, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric(MetricAggregateType.Max);

            Assert.AreEqual(1001, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Median_1()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Median)
            {
                100, 200, 300, 400, 500, 600, 700
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(400, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Median_2()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Median)
            {
                100, 200, 300, 400, 500, 600, 700, 800
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(450, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_Median_3()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric")
            {
                100, 200, 300, 400, 500, 600, 700, 800
            };

            Metric metric = metricAggregate.ToMetric(MetricAggregateType.Median);

            Assert.AreEqual(450, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_RawValue_1()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Raw)
            {
                100, 200, 300, 400, 500, 600, 700, 800, 900, 1000
            };

            Metric metric = metricAggregate.ToMetric();

            Assert.AreEqual(1000, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_RawValue_2()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Raw);
            metricAggregate.Add(100);
            metricAggregate.Add(200);
            metricAggregate.Add(300);

            Metric metric = metricAggregate.ToMetric();

            // Latest value
            Assert.AreEqual(300, metric.Value);
        }

        [Test]
        public void MetricAggregateCalculatesAggregateValuesAsExpected_RawValue_3()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric");
            metricAggregate.Add(100);
            metricAggregate.Add(200);
            metricAggregate.Add(300);

            Metric metric = metricAggregate.ToMetric(MetricAggregateType.Raw);

            // Latest value
            Assert.AreEqual(300, metric.Value);
        }

        [Test]
        public void MetricAggregateReturnsTheExpectedMetricWhenSamplesAreNotPresent()
        {
            MetricAggregate metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Average);
            Metric metric = metricAggregate.ToMetric();

            Assert.IsTrue(object.ReferenceEquals(Metric.None, metric));

            metricAggregate = new MetricAggregate("AnyMetric", MetricAggregateType.Raw);
            metric = metricAggregate.ToMetric();

            Assert.IsTrue(object.ReferenceEquals(Metric.None, metric));
        }
    }
}
