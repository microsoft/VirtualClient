// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;

    /// <summary>
    /// Represents the result of a single metric
    /// </summary>
    [DebuggerDisplay("{Name,nq}={Value} {Unit != null ? Unit : \"\",nq}")]
    public class Metric : IEquatable<Metric>
    {
        /// <summary>
        /// Creates a metric
        /// </summary>
        [JsonConstructor]
        public Metric(string name, double value)
        {
            this.Name = name;
            this.Value = value;
            this.Relativity = MetricRelativity.Undefined;
            this.Metadata = new Dictionary<string, IConvertible>(); 
            this.Tags = new List<string>();
        }

        /// <summary>
        /// Creates a metric
        /// </summary>
        public Metric(string name, double value, IEnumerable<string> tags = null, string description = null, IDictionary<string, IConvertible> metadata = null)
            : this(name, value)
        {
            this.Description = description;
            if (tags?.Any() == true)
            {
                this.Tags.AddRange(tags);
            }

            if (metadata?.Any() == true)
            {
                foreach (KeyValuePair<string, IConvertible> item in metadata)
                {
                    this.Metadata[item.Key] = item.Value;
                }
            }
        }

        /// <summary>
        /// Creates a metric
        /// </summary>
        public Metric(string name, double value, MetricRelativity relativity, IEnumerable<string> tags = null, string description = null, IDictionary<string, IConvertible> metadata = null)
            : this(name, value, tags: tags, description: description, metadata: metadata)
        {
            this.Relativity = relativity;
        }

        /// <summary>
        /// Creates a metric
        /// </summary>
        public Metric(string name, double value, string unit, IEnumerable<string> tags = null, string description = null, IDictionary<string, IConvertible> metadata = null)
            : this(name, value, tags: tags, description: description, metadata: metadata)
        {
            this.Unit = unit;
        }

        /// <summary>
        /// Creates a metric
        /// </summary>
        public Metric(string name, double value, string unit, MetricRelativity relativity, IEnumerable<string> tags = null, string description = null, IDictionary<string, IConvertible> metadata = null)
            : this(name, value, unit, tags: tags, description: description, metadata: metadata)
        {
            this.Relativity = relativity;
        }

        /// <summary>
        /// Creates a metric
        /// </summary>
        public Metric(string name, double value, string unit, MetricRelativity relativity, int verbosity, IEnumerable<string> tags = null, string description = null, IDictionary<string, IConvertible> metadata = null)
            : this(name, value, unit, relativity, tags: tags, description: description, metadata: metadata)
        {
            this.Verbosity = verbosity;
        }

        /// <summary>
        /// Represents the case where a valid measurement could not be taken.
        /// </summary>
        public static Metric None { get; } = new Metric("n/a", 0);

        /// <summary>
        /// Name of metric
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Name of metric
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Defines how the metric value relates to the preferred outcome 
        /// (e.g. higher/lower is better).
        /// </summary>
        public MetricRelativity Relativity { get; set; }

        /// <summary>
        /// Result of test
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Unit of result
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// StartTime
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Unit of result
        /// </summary>
        public List<string> Tags { get; }

        /// <summary>
        /// Telemetry context for metric.
        /// </summary>
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Metadata { get; }

        /// <summary>
        /// Metric verbosity to describe importance/priority of the metric.
        /// 
        /// Verbosity Levels (only 1, 3, and 5 are currently used):
        /// - 1 (Standard/Critical): Most important metrics for decision making - bandwidth, throughput, IOPS, key latency percentiles (p50, p99)
        /// - 2 (Reserved): Reserved for future expansion
        /// - 3 (Detailed): Additional detailed metrics - supplementary percentiles (p70, p90, p95, p99.9)
        /// - 4 (Reserved): Reserved for future expansion
        /// - 5 (Verbose): All diagnostic/internal metrics - histogram buckets, standard deviations, byte counts, I/O counts
        /// 
        /// Currently, only levels 1, 3, and 5 are actively used. Levels 2 and 4 are reserved for future use.
        /// 
        /// Default = 1 (Standard).
        /// </summary>
        public int Verbosity { get; set; } = 1;

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(Metric lhs, Metric rhs)
        {
            if (object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if (object.ReferenceEquals(null, lhs) || object.ReferenceEquals(null, rhs))
            {
                return false;
            }

            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Determines if two objects are NOT equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are NOT equal. False otherwise.</returns>
        public static bool operator !=(Metric lhs, Metric rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Override method determines if the two objects are equal
        /// </summary>
        /// <param name="obj">Defines the object to compare against the current instance.</param>
        /// <returns>
        /// Type:  System.Boolean
        /// True if the objects are equal or False if not
        /// </returns>
        public override bool Equals(object obj)
        {
            bool areEqual = false;

            if (object.ReferenceEquals(this, obj))
            {
                areEqual = true;
            }
            else
            {
                // Apply value-type semantics to determine
                // the equality of the instances
                Metric itemDescription = obj as Metric;
                if (itemDescription != null)
                {
                    areEqual = this.Equals(itemDescription);
                }
            }

            return areEqual;
        }

        /// <summary>
        /// Method determines if the other object is equal to this instance
        /// </summary>
        /// <param name="other">Defines the object to compare against the current instance.</param>
        /// <returns>
        /// Type:  System.Boolean
        /// True if the objects are equal or False if not
        /// </returns>
        public virtual bool Equals(Metric other)
        {
            return other != null && this.GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// Override method returns a unique integer hash code
        /// identifier for the class instance
        /// </summary>
        /// <returns>
        /// Type:  System.Int32
        /// A unique integer identifier for the class instance
        /// </returns>
        public override int GetHashCode()
        {
            StringBuilder hashBuilder = new StringBuilder()
                .Append($"{this.Name},{this.Value},{this.Unit},{this.Description},{this.Relativity},{this.Verbosity},{string.Join(",", this.Tags)},{this.StartTime},{this.EndTime}");

            return hashBuilder.ToString().ToLowerInvariant().GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}