namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using MathNet.Numerics.Integration;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// openssl s_time metrics parser
    /// </summary>
    public class OpenSslTlsMetricsParser : MetricsParser
    {
        private const string TotalBytesRead = "TotalBytesRead";
        private const string NoOfConnections = "NumberOfConnections";
        private const string Duration = "Duration";
        private const string BytesperConnection = "BytesReadPerConnection";
        private const string NewConnThroughput = "NewConnectionThroughput";
        private const string NewConnPersec = "NewConnectionsPerSec";

        private const string ReuseTotalBytesRead = "ReuseTotalBytesRead";
        private const string ReuseNoOfConnections = "ReuseNumberOfConnections";
        private const string ReuseDuration = "ReuseDuration";
        private const string ReuseBytesperConnection = "ReuseBytesReadPerConnection";
        private const string ReuseConnThroughput = "ReuseConnectionThroughput";
        private const string ReuseConnPerSec = "ReuseConnectionsPerSec";

        /// <summary>
        /// parse output of openssl s_time
        /// </summary>
        public OpenSslTlsMetricsParser(string resultsText, string commandArguments)
            : base(resultsText)
        {
            commandArguments.ThrowIfNullOrWhiteSpace(nameof(commandArguments));
            this.CommandArguments = commandArguments;
        }

        /// <summary>
        /// The command line arguments provided to the OpenSSL command (e.g. speed -elapsed -seconds 100 -multi 4 aes-256-cbc).
        /// </summary>
        public string CommandArguments { get; }

        /// <summary>
        /// parser function
        /// </summary>
        /// <returns></returns>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();
            var parsedMetrics = this.ParseSTimeOutput();
            double newConnThroughput = parsedMetrics[TotalBytesRead] / parsedMetrics[Duration];
            double reuseConnThroughput = parsedMetrics[ReuseTotalBytesRead] / parsedMetrics[ReuseDuration];
            double newConnPerSec = parsedMetrics[NoOfConnections] / parsedMetrics[Duration];
            double reuseConnPerSec = parsedMetrics[ReuseNoOfConnections] / parsedMetrics[ReuseDuration];

            // add metrics to the list -

            // set 1 - new connection metrics
            metrics.Add(new Metric(TotalBytesRead, parsedMetrics[TotalBytesRead], MetricUnit.Bytes, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(NoOfConnections, parsedMetrics[NoOfConnections], MetricUnit.Count, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(Duration, parsedMetrics[Duration], MetricUnit.Seconds, MetricRelativity.Undefined, verbosity: 3));
            metrics.Add(new Metric(BytesperConnection, parsedMetrics[BytesperConnection], MetricUnit.BytesPerConnection, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(NewConnThroughput, newConnThroughput, MetricUnit.BytesPerSecond, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(NewConnPersec, newConnPerSec, MetricUnit.Count, MetricRelativity.HigherIsBetter, verbosity: 1));

            // set 2 - reuse connection metrics
            metrics.Add(new Metric(ReuseTotalBytesRead, parsedMetrics[ReuseTotalBytesRead], MetricUnit.Bytes, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(ReuseNoOfConnections, parsedMetrics[ReuseNoOfConnections], MetricUnit.Count, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(ReuseDuration, parsedMetrics[ReuseDuration], MetricUnit.Seconds, MetricRelativity.Undefined, verbosity: 3));
            metrics.Add(new Metric(ReuseBytesperConnection, parsedMetrics[ReuseBytesperConnection], MetricUnit.BytesPerConnection, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(ReuseConnThroughput, reuseConnThroughput, MetricUnit.BytesPerSecond, MetricRelativity.HigherIsBetter, verbosity: 1));
            metrics.Add(new Metric(ReuseConnPerSec, reuseConnPerSec, MetricUnit.Count, MetricRelativity.HigherIsBetter, verbosity: 1));

            return metrics;
        }

        /// <summary>
        /// parse s_time output and extract metrics
        /// </summary>
        /// <returns> metric names and values </returns>
        public Dictionary<string, double> ParseSTimeOutput()
        {
            string input = this.RawText;
            var metrics = new Dictionary<string, double>();

            /* 
            * output from s_time
            * Collecting connection statistics for 5 seconds
               ...
              1184 connections in 0.78s; 1517.95 connections/user sec, bytes read 382432
              1184 connections in 6 real seconds, 323 bytes read per connection
               ...
              Now timing with session id reuse.
              starting
              ...
              1696 connections in 0.86s; 1972.09 connections/user sec, bytes read 547808
              1696 connections in 6 real seconds, 323 bytes read per connection
            */
            
            // ignore the first connections and connections per user metric as it is conflicting with 
            // total connections in n seconds
            
            // Initial run
            var match1 = Regex.Match(input, @"(\d+) connections in ([\d.]+)s; ([\d.]+) connections/user sec, bytes read (\d+)");
            if (match1.Success)
            {
                metrics[TotalBytesRead] = Convert.ToDouble(match1.Groups[4].Value);
            }

            var match2 = Regex.Match(input, @"(\d+) connections in (\d+) real seconds, (\d+) bytes read per connection");
            if (match2.Success)
            {
                metrics[NoOfConnections] = Convert.ToDouble(match2.Groups[1].Value);
                metrics[Duration] = Convert.ToDouble(match2.Groups[2].Value);
                metrics[BytesperConnection] = Convert.ToDouble(match2.Groups[3].Value);
            }

            // Session reuse run
            var reuseSection = input.Substring(input.IndexOf("Now timing with session id reuse."));
            var match3 = Regex.Match(reuseSection, @"(\d+) connections in ([\d.]+)s; ([\d.]+) connections/user sec, bytes read (\d+)");
            if (match3.Success)
            {
                metrics[ReuseTotalBytesRead] = Convert.ToDouble(match3.Groups[4].Value);
            }

            var match4 = Regex.Match(reuseSection, @"(\d+) connections in (\d+) real seconds, (\d+) bytes read per connection");
            if (match4.Success)
            {
                metrics[ReuseNoOfConnections] = Convert.ToDouble(match4.Groups[1].Value);
                metrics[ReuseDuration] = Convert.ToDouble(match4.Groups[2].Value);
                metrics[ReuseBytesperConnection] = Convert.ToDouble(match4.Groups[3].Value);
            }

            return metrics;
        }
    }
}
