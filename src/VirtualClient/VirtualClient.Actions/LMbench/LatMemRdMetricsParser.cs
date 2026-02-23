using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualClient.Contracts;

namespace VirtualClient.Actions
{
    /// <summary>
    /// 
    /// </summary>
    public class LatMemRdMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex LatMemRdSectionDelimiter = new Regex(@$"({Environment.NewLine})(\s)*({Environment.NewLine})", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Initializes a new instance of the <see cref="LatMemRdMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText"></param>
        public LatMemRdMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            IList<Metric> metrics = new List<Metric>();
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, LatMemRdSectionDelimiter);
            foreach (var section in this.Sections)
            {
                var lines = section.Value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var values = line.Split(' ');
                    string strideSize = section.Key.Split('=')[1];
                    long arraySizeInBytes = this.RoundOffToNearest512Multiple(double.Parse(values[0]) * 1024 * 1024) * 512;
                    var metadata = new Dictionary<string, IConvertible>();

                    metadata.Add("StrideSizeInBytes", strideSize);
                    metadata.Add("ArraySize", this.ArraySizeWithUnit(arraySizeInBytes));
                    metadata.Add("ArraySizeInBytes", arraySizeInBytes);
                    
                    metrics.Add(new Metric($"Latency", double.Parse(values[1]), "ns", MetricRelativity.LowerIsBetter, null, $"Latency for memory read operation for Array size in MB {values[0]} & stride size {strideSize} in nano seconds", metadata));
                }
            }

            return metrics;

        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.RawText.Trim();
        }

        private long RoundOffToNearest512Multiple(double number)
        {
            return (long)Math.Round(number / 512.0);
        }

        private string ArraySizeWithUnit(double bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024 * 1024)}_MiB";
            }
            else if (bytes >= 1024)
            {
                return $"{bytes / 1024}_KiB";
            }
            else
            {
                return $"{bytes}_B";
            }
        }
    }
}
