using VirtualClient.Common.Contracts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class RedisBenchmarkMetricsParserTests
    {
        private string rawText;
        private RedisBenchmarkMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Redis\RedisBenchmarkResults.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new RedisBenchmarkMetricsParser(this.rawText);
        }


        [Test]
        public void RedisParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(140, metrics.Count);
            MetricAssert.Exists(metrics, "PING_INLINE_Requests/Sec", 3154.38, "requests/second");
            MetricAssert.Exists(metrics, "PING_INLINE_Average_Latency", 15.758, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_INLINE_Min_Latency", 0.416, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_INLINE_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_INLINE_P95_Latency", 16.335, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_INLINE_P99_Latency", 27.935, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_INLINE_Max_Latency", 53.599, "milliSeconds");

            MetricAssert.Exists(metrics, "PING_MBULK_Requests/Sec", 3065.04, "requests/second");
            MetricAssert.Exists(metrics, "PING_MBULK_Average_Latency", 16.231, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_MBULK_Min_Latency", 0.416, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_MBULK_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_MBULK_P95_Latency", 16.799, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_MBULK_P99_Latency", 27.951, "milliSeconds");
            MetricAssert.Exists(metrics, "PING_MBULK_Max_Latency", 52.063, "milliSeconds");

            MetricAssert.Exists(metrics, "SET_Requests/Sec", 3340.12, "requests/second");
            MetricAssert.Exists(metrics, "SET_Average_Latency", 14.829, "milliSeconds");
            MetricAssert.Exists(metrics, "SET_Min_Latency", 0.472, "milliSeconds");
            MetricAssert.Exists(metrics, "SET_P50_Latency", 15.927, "milliSeconds");
            MetricAssert.Exists(metrics, "SET_P95_Latency", 16.383, "milliSeconds");
            MetricAssert.Exists(metrics, "SET_P99_Latency", 27.951, "milliSeconds");
            MetricAssert.Exists(metrics, "SET_Max_Latency", 39.903, "milliSeconds");

            MetricAssert.Exists(metrics, "GET_Requests/Sec", 3066.73, "requests/second");
            MetricAssert.Exists(metrics, "GET_Average_Latency", 16.222, "milliSeconds");
            MetricAssert.Exists(metrics, "GET_Min_Latency", 3.832, "milliSeconds");
            MetricAssert.Exists(metrics, "GET_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "GET_P95_Latency", 16.511, "milliSeconds");
            MetricAssert.Exists(metrics, "GET_P99_Latency", 27.983, "milliSeconds");
            MetricAssert.Exists(metrics, "GET_Max_Latency", 44.191, "milliSeconds");

            MetricAssert.Exists(metrics, "INCR_Requests/Sec", 3127.64, "requests/second");
            MetricAssert.Exists(metrics, "INCR_Average_Latency", 15.891, "milliSeconds");
            MetricAssert.Exists(metrics, "INCR_Min_Latency", 0.448, "milliSeconds");
            MetricAssert.Exists(metrics, "INCR_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "INCR_P95_Latency", 16.671, "milliSeconds");
            MetricAssert.Exists(metrics, "INCR_P99_Latency", 30.799, "milliSeconds");
            MetricAssert.Exists(metrics, "INCR_Max_Latency", 44.031, "milliSeconds");

            MetricAssert.Exists(metrics, "LPUSH_Requests/Sec", 3049.62, "requests/second");
            MetricAssert.Exists(metrics, "LPUSH_Average_Latency", 16.310, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH_Min_Latency", 6.744, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH_P50_Latency", 15.943, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH_P95_Latency", 17.887, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH_P99_Latency", 31.807, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH_Max_Latency", 50.495, "milliSeconds");

            MetricAssert.Exists(metrics, "RPUSH_Requests/Sec", 3158.06, "requests/second");
            MetricAssert.Exists(metrics, "RPUSH_Average_Latency", 15.736, "milliSeconds");
            MetricAssert.Exists(metrics, "RPUSH_Min_Latency", 0.488, "milliSeconds");
            MetricAssert.Exists(metrics, "RPUSH_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "RPUSH_P95_Latency", 16.215, "milliSeconds");
            MetricAssert.Exists(metrics, "RPUSH_P99_Latency", 26.639, "milliSeconds");
            MetricAssert.Exists(metrics, "RPUSH_Max_Latency", 44.031, "milliSeconds");

            MetricAssert.Exists(metrics, "LPOP_Requests/Sec", 3048.32, "requests/second");
            MetricAssert.Exists(metrics, "LPOP_Average_Latency", 16.316, "milliSeconds");
            MetricAssert.Exists(metrics, "LPOP_Min_Latency", 0.584, "milliSeconds");
            MetricAssert.Exists(metrics, "LPOP_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "LPOP_P95_Latency", 18.047, "milliSeconds");
            MetricAssert.Exists(metrics, "LPOP_P99_Latency", 31.823, "milliSeconds");
            MetricAssert.Exists(metrics, "LPOP_Max_Latency", 56.639, "milliSeconds");

            MetricAssert.Exists(metrics, "RPOP_Requests/Sec", 3148.52, "requests/second");
            MetricAssert.Exists(metrics, "RPOP_Average_Latency", 15.794, "milliSeconds");
            MetricAssert.Exists(metrics, "RPOP_Min_Latency", 0.440, "milliSeconds");
            MetricAssert.Exists(metrics, "RPOP_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "RPOP_P95_Latency", 16.239, "milliSeconds");
            MetricAssert.Exists(metrics, "RPOP_P99_Latency", 27.935, "milliSeconds");
            MetricAssert.Exists(metrics, "RPOP_Max_Latency", 52.095, "milliSeconds");

            MetricAssert.Exists(metrics, "SADD_Requests/Sec", 3104.24, "requests/second");
            MetricAssert.Exists(metrics, "SADD_Average_Latency", 16.005, "milliSeconds");
            MetricAssert.Exists(metrics, "SADD_Min_Latency", 0.616, "milliSeconds");
            MetricAssert.Exists(metrics, "SADD_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "SADD_P95_Latency", 18.159, "milliSeconds");
            MetricAssert.Exists(metrics, "SADD_P99_Latency", 31.839, "milliSeconds");
            MetricAssert.Exists(metrics, "SADD_Max_Latency", 55.999, "milliSeconds");

            MetricAssert.Exists(metrics, "HSET_Requests/Sec", 3056.42, "requests/second");
            MetricAssert.Exists(metrics, "HSET_Average_Latency", 16.273, "milliSeconds");
            MetricAssert.Exists(metrics, "HSET_Min_Latency", 0.712, "milliSeconds");
            MetricAssert.Exists(metrics, "HSET_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "HSET_P95_Latency", 16.543, "milliSeconds");
            MetricAssert.Exists(metrics, "HSET_P99_Latency", 31.759, "milliSeconds");
            MetricAssert.Exists(metrics, "HSET_Max_Latency", 47.967, "milliSeconds");

            MetricAssert.Exists(metrics, "SPOP_Requests/Sec", 3130.28, "requests/second");
            MetricAssert.Exists(metrics, "SPOP_Average_Latency", 15.874, "milliSeconds");
            MetricAssert.Exists(metrics, "SPOP_Min_Latency", 0.384, "milliSeconds");
            MetricAssert.Exists(metrics, "SPOP_P50_Latency", 15.943, "milliSeconds");
            MetricAssert.Exists(metrics, "SPOP_P95_Latency", 16.479, "milliSeconds");
            MetricAssert.Exists(metrics, "SPOP_P99_Latency", 31.743, "milliSeconds");
            MetricAssert.Exists(metrics, "SPOP_Max_Latency", 50.751, "milliSeconds");

            MetricAssert.Exists(metrics, "ZADD_Requests/Sec", 3042.94, "requests/second");
            MetricAssert.Exists(metrics, "ZADD_Average_Latency", 16.345, "milliSeconds");
            MetricAssert.Exists(metrics, "ZADD_Min_Latency", 0.632, "milliSeconds");
            MetricAssert.Exists(metrics, "ZADD_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "ZADD_P95_Latency", 17.407, "milliSeconds");
            MetricAssert.Exists(metrics, "ZADD_P99_Latency", 31.903, "milliSeconds");
            MetricAssert.Exists(metrics, "ZADD_Max_Latency", 44.031, "milliSeconds");

            MetricAssert.Exists(metrics, "ZPOPMIN_Requests/Sec", 3157.16, "requests/second");
            MetricAssert.Exists(metrics, "ZPOPMIN_Average_Latency", 15.738, "milliSeconds");
            MetricAssert.Exists(metrics, "ZPOPMIN_Min_Latency", 0.392, "milliSeconds");
            MetricAssert.Exists(metrics, "ZPOPMIN_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "ZPOPMIN_P95_Latency", 16.247, "milliSeconds");
            MetricAssert.Exists(metrics, "ZPOPMIN_P99_Latency", 27.871, "milliSeconds");
            MetricAssert.Exists(metrics, "ZPOPMIN_Max_Latency", 59.263, "milliSeconds");

            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_Requests/Sec", 3096.74, "requests/second");
            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_Average_Latency", 16.063, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_Min_Latency", 0.464, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_P50_Latency", 15.935, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_P95_Latency", 16.135, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_P99_Latency", 20.511, "milliSeconds");
            MetricAssert.Exists(metrics, "LPUSH (needed to benchmark LRANGE)_Max_Latency", 55.455, "milliSeconds");

            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_Requests/Sec", 3115.85, "requests/second");
            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_Average_Latency", 15.860, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_Min_Latency", 0.904, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_P50_Latency", 15.847, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_P95_Latency", 16.223, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_P99_Latency", 27.759, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_100 (first 100 elements)_Max_Latency", 56.607, "milliSeconds");

            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_Requests/Sec", 3018.32, "requests/second");
            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_Average_Latency", 16.066, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_Min_Latency", 0.920, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_P50_Latency", 15.551, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_P95_Latency", 19.327, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_P99_Latency", 31.599, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_300 (first 300 elements)_Max_Latency", 65.791, "milliSeconds");

            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_Requests/Sec", 3037.57, "requests/second");
            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_Average_Latency", 15.768, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_Min_Latency", 0.856, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_P50_Latency", 15.359, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_P95_Latency", 17.983, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_P99_Latency", 31.327, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_500 (first 450 elements)_Max_Latency", 61.343, "milliSeconds");

            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_Requests/Sec", 3037.85, "requests/second");
            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_Average_Latency", 15.540, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_Min_Latency", 1.048, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_P50_Latency", 15.159, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_P95_Latency", 17.823, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_P99_Latency", 30.575, "milliSeconds");
            MetricAssert.Exists(metrics, "LRANGE_600 (first 600 elements)_Max_Latency", 59.967, "milliSeconds");

            MetricAssert.Exists(metrics, "MSET (10 keys)_Requests/Sec", 3040.53, "requests/second");
            MetricAssert.Exists(metrics, "MSET (10 keys)_Average_Latency", 16.313, "milliSeconds");
            MetricAssert.Exists(metrics, "MSET (10 keys)_Min_Latency", 0.488, "milliSeconds");
            MetricAssert.Exists(metrics, "MSET (10 keys)_P50_Latency", 15.927, "milliSeconds");
            MetricAssert.Exists(metrics, "MSET (10 keys)_P95_Latency", 27.967, "milliSeconds");
            MetricAssert.Exists(metrics, "MSET (10 keys)_P99_Latency", 35.903, "milliSeconds");
            MetricAssert.Exists(metrics, "MSET (10 keys)_Max_Latency", 59.903, "milliSeconds");

        }

        [Test]
        public void RedisParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectRedisoutputPath = Path.Combine(workingDirectory, @"Examples\Redis\RedisIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectRedisoutputPath);
            this.testParser = new RedisBenchmarkMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The Redis output file has incorrect format for parsing", exception.Message);
        }
    }
}
