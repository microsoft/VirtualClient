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
        private static readonly Regex LatMemRdSectionDelimiter = new Regex(@"(\r)(\n)(\s)*(\r)(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Initializes a new instance of the <see cref="LatMemRdMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText"></param>
        public LatMemRdMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private long RoundOffToNearest512Multiple(double number)
        {
            return (long)Math.Round(number / 512.0);
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            IList<Metric> metrics = new List<Metric>();
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, LatMemRdSectionDelimiter);
            foreach (var section in this.Sections)
            {
                var lines = section.Value.Split("\r\n");
                foreach (var line in lines)
                {
                    var values = line.Split(' ');
                    string strideSize = section.Key.Split('=')[1];
                    var metadata = new Dictionary<string, IConvertible>();
                    metadata.Add("StrideSizeBytes", strideSize);
                    metadata.Add("ArraySizeInMiB", values[0]);
                    metrics.Add(new Metric($"Latency_StrideBytes_{strideSize}_Array_{values[0]}_B/KiB/MiB", double.Parse(values[1]), "ns", MetricRelativity.LowerIsBetter, null, $"Latency for memory read operation for Array size in MB {values[0]} & stride size {strideSize} in nano seconds", metadata));
                }
            }

            return metrics;

        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Converting all CRLF(Windows EOL) to LF(Unix EOL).
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");

            // Converting all LF to CRLF.
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", "\r\n");

            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.PreprocessedText.Trim();
        }
    }
}
