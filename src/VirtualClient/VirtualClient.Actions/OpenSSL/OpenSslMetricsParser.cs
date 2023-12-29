// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// A metrics parser for OpenSSL speed workload results.
    /// </summary>
    public class OpenSslMetricsParser : MetricsParser
    {
        private const string ColumnCipher = "Cipher";
        private const string ColumnBytes = "Bytes";
        private const string ColumnKilobytesPerSec = "KilobytesPerSec";
        private const string ColumnUnit = "Unit";
        private const string ColumnValue = "Value";

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenSslMetricsParser"/> class.
        /// </summary>
        /// <param name="resultsText">The raw results from the OpenSSL speed command.</param>
        /// <param name="commandArguments">
        /// The command line arguments provided to the OpenSSL command (e.g. speed -elapsed -seconds 100 -multi 4 aes-256-cbc).
        /// </param>
        public OpenSslMetricsParser(string resultsText, string commandArguments)
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
        /// The parsed results of the cipher/crypto algorithm performance output.
        /// </summary>
        protected DataTable CipherResults { get; private set; }

        /// <summary>
        /// The parsed results of the rsa algorithm performance output.
        /// </summary>
        protected DataTable RSAResults { get; private set; }

        /// <summary>
        /// True if the results have been parsed.
        /// </summary>
        protected bool IsParsed
        {
            get
            {
                return this.CipherResults != null;
            }
        }

        /// <summary>
        /// Returns the set of metrics parsed from the OpenSSL speed results.
        /// </summary>
        /// <example>
        /// Example Results Format:
        /// <code>
        /// version: 3.0.0-beta3-dev
        /// built on: built on: Thu Aug  5 18:45:56 2021 UTC
        /// options:bn(64,64) 
        /// compiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG
        /// CPUINFO: OPENSSL_ia32cap=0xfeda32235f8bffff:0x405f46f1bf2fb9
        /// The 'numbers' are in 1000s of bytes per second processed.
        /// type             16 bytes     64 bytes    256 bytes   1024 bytes   8192 bytes  16384 bytes
        /// md5              86478.31k   234491.80k   475166.47k   643611.03k   718366.04k   724760.44k
        /// sha1            112072.53k   357766.41k   871397.13k  1368425.47k  1639579.37k  1664715.98k
        /// sha256           98952.23k   300483.21k   717624.35k  1098886.72k  1296898.18k  1316783.17k
        /// sha512           45048.15k   180506.46k   340705.20k   518054.81k   620266.84k   631089.29k
        /// hmac(md5)        59612.33k   180417.03k   413875.75k   611803.72k   712230.50k   719883.47k
        /// des-ede3         28731.24k    29099.97k    29181.98k    29235.10k    29230.97k    29238.89k
        /// aes-128-cbc   -1040172814.93   1589805.17k  1657786.91k  1674053.70k  1677365.52k  1633415.99k
        /// aes-192-cbc     912407.69k  1160122.78k  1286967.41k  1351096.97k  1365794.00k  1343098.06k
        /// aes-256-cbc     775919.04k  1039918.13k  1138510.81k  1153518.87k  1156818.26k  1176283.27k
        /// camellia-128-cbc    99896.12k   153483.42k   178735.37k   180291.82k   180676.20k   187749.17k
        /// camellia-192-cbc    84453.66k   115236.42k   130669.72k   132710.30k   136902.79k   138821.09k
        /// camellia-256-cbc    69699.13k   109973.58k   129509.29k   136012.77k   138190.85k   138823.82k
        /// ghash           488056.36k  1614821.05k  4198891.63k  6159644.33k  8829444.92k  8812728.05k
        /// rand             12140.14k    53908.09k   196501.89k  1062619.07k  4221050.33k  5279181.48k
        ///                   sign    verify    sign/s verify/s
        /// rsa  512 bits 0.000038s 0.000002s  26538.8 432400.4
        /// rsa 1024 bits 0.000099s 0.000006s  10096.9 172208.4
        /// rsa 2048 bits 0.000320s 0.000019s   3121.9  52842.2
        /// rsa 3072 bits 0.001967s 0.000040s    508.3  24786.9
        /// rsa 4096 bits 0.004418s 0.000068s    226.4  14620.4
        /// rsa 7680 bits 0.041450s 0.000231s     24.1   4323.9
        /// rsa 15360 bits 0.210629s 0.000914s      4.7   1094.3
        /// </code>
        /// </example>
        /// <returns>
        /// A set of <see cref="Metric"/> instances each describing a single performance measurement.
        /// </returns>
        public override IList<Metric> Parse()
        {
            bool cipherResultsValid = false;
            bool rsaResultsValid = false;

            IEnumerable<int> bufferByteSizes = this.GetCipherBufferByteSizes();
            if (this.TryParseCipherPerformanceResults(bufferByteSizes, out DataTable cipherResults))
            {
                cipherResultsValid = true;
                this.CipherResults = cipherResults;
            }

            if (this.TryParseSignVerifyPerformanceResults(out DataTable rsaResults))
            {
                rsaResultsValid = true;
                this.RSAResults = rsaResults;
            }

            if (!cipherResultsValid && !rsaResultsValid)
            {
                throw new SchemaException(
                    $"Invalid results format. The results provided to the parser are not valid/complete OpenSSL speed workload results. Results: {Environment.NewLine}" +
                    $"{this.RawText.Substring(0, this.RawText.Length / 2)}...");
            }

            List<Metric> metrics = new List<Metric>();
            if (cipherResultsValid)
            {
                foreach (DataRow row in this.CipherResults.Rows)
                {
                    // Ex: aes-256-cbc (8192 bytes)
                    string metricName = $"{row[OpenSslMetricsParser.ColumnCipher]} {row[OpenSslMetricsParser.ColumnBytes]}-byte";
                    double metricValue = (double)row[OpenSslMetricsParser.ColumnKilobytesPerSec];

                    // There is an anomaly that sometimes happens where the result is a negative number. It indicates
                    // some type of issue in the OpenSSL speed test itself and the result is not valid. We exclude these
                    // results.
                    //
                    // Example:
                    // type             16 bytes      64 bytes     256 bytes    1024 bytes    8192 bytes     16384 bytes
                    // aes-128-cbc   -1040172814.93   1589805.17k  1657786.91k  1674053.70k   1677365.52k    1633415.99k
                    if (metricValue >= 0)
                    {
                        metrics.Add(new Metric(metricName, metricValue, MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter));
                    }
                }
            }

            if (rsaResultsValid)
            {
                foreach (DataRow row in this.RSAResults.Rows)
                {
                    string metricName = $"{row[OpenSslMetricsParser.ColumnCipher]} {row[OpenSslMetricsParser.ColumnUnit]}";
                    double metricValue = (double)row[OpenSslMetricsParser.ColumnValue];

                    if (metricValue >= 0)
                    {
                        if (metricName.Contains("/"))
                        {
                            metrics.Add(new Metric(metricName, metricValue, $"{row[OpenSslMetricsParser.ColumnUnit]}", MetricRelativity.HigherIsBetter));
                        }
                        else
                        {
                            metrics.Add(new Metric(metricName, metricValue, MetricUnit.Seconds, MetricRelativity.LowerIsBetter));
                        }
                    }
                }
            }

            return metrics;
        }

        private IEnumerable<int> GetCipherBufferByteSizes()
        {
            List<int> byteSizes = new List<int>();

            // Example:
            // type    16 bytes     64 bytes    256 bytes   1024 bytes   8192 bytes  16384 bytes
            Match cipherTypes = Regex.Match(this.RawText, $@"(?<=type\s*)(?:([0-9]+)\s*bytes\s*)+", RegexOptions.IgnoreCase);

            // Note:
            // When running on Linux with the '-multi' option, we found a difference in the format of the output.
            // OpenSSL DOES NOT include the type/byte size headers. When we hit this scenario, we need to fallback to
            // another way of identifying the type/byte sizes. The OpenSSL speed command supports 1 of 2 options:
            // 1) No byte sizes are defined. In this scenario, all byte sizes will be used.
            // 2) A specific byte size is defined. In this scenario, only that byte size will be used.
            if (cipherTypes.Success && cipherTypes.Groups.Count == 2)
            {
                foreach (Capture cipherSize in cipherTypes.Groups[1].Captures)
                {
                    byteSizes.Add(int.Parse(cipherSize.Value));
                }
            }
            else
            {
                // Example (i.e. no header to work with):
                // version: 3.0.0 - beta3 - dev
                // built on: built on: Thu Aug  5 18:45:56 2021 UTC
                // options:bn(64, 64)
                // compiler: gcc - fPIC - pthread - m64 - Wa,--noexecstack - Wall - O3 - DOPENSSL_USE_NODELETE - DL_ENDIAN - DOPENSSL_PIC - DOPENSSL_BUILDING_OPENSSL - DNDEBUG
                // CPUINFO: OPENSSL_ia32cap = 0xfeda32235f8bffff:0x1c2fb9
                // md5        971380.35k   1693511.81k   2079132.16k  2200152.47k  2133075.56k  2108594.59k
                MatchCollection explicitByteSizes = Regex.Matches(this.CommandArguments, @"(?:-bytes\=*\s*([0-9]+))", RegexOptions.IgnoreCase);
                if (explicitByteSizes?.Any() == true)
                {
                    // OpenSSL allows only 1 but does not throw an error if more than 1 is defined. If more than
                    // one is defined, the last -bytes defined will be used.
                    byteSizes.Add(int.Parse(explicitByteSizes.Last().Groups[1].Value));
                }
                else
                {
                    // The default buffer byte sizes.
                    byteSizes.AddRange(new int[] { 16, 64, 256, 1024, 8192, 16384 });
                }
            }

            return byteSizes;
        }

        private bool TryParseCipherPerformanceResults(IEnumerable<int> cipherBytes, out DataTable results)
        {
            results = null;
            bool parsedSuccessfully = false;

            // Example:
            // sha256         98952.23k       300483.21k   717624.35k   1098886.72k  1296898.18k  1316783.17k
            // aes-128-cbc   -1040172814.93   1589805.17k  1657786.91k  1674053.70k  1677365.52k  1633415.99k
            MatchCollection cipherPerformanceResults = Regex.Matches(this.RawText, $@"([a-z0-9\-\(\)]+)(\s*[0-9\.]+k|\s*-[0-9\.]+)+", RegexOptions.IgnoreCase);
            if (cipherPerformanceResults?.Any() == true)
            {
                DataTable cipherResults = new DataTable();
                cipherResults.Columns.AddRange(new DataColumn[]
                {
                    new DataColumn(OpenSslMetricsParser.ColumnCipher, typeof(string)),
                    new DataColumn(OpenSslMetricsParser.ColumnBytes, typeof(int)),
                    new DataColumn(OpenSslMetricsParser.ColumnKilobytesPerSec, typeof(double)),
                });

                foreach (Match match in cipherPerformanceResults)
                {
                    if (match.Groups.Count == 3
                        && match.Groups[2].Captures?.Any() == true
                        && match.Groups[2].Captures.Count == cipherBytes.Count())
                    {
                        int typeIndex = 0;
                        string cipher = match.Groups[1].Value.Trim();
                        foreach (Capture performanceResult in match.Groups[2].Captures)
                        {
                            Match numericMatch = Regex.Match(performanceResult.Value, @"[-0-9\.]+", RegexOptions.IgnoreCase);
                            if (numericMatch.Success)
                            {
                                parsedSuccessfully = true;
                                double kilobytesPerSec = double.Parse(numericMatch.Value.Trim());
                                cipherResults.Rows.Add(cipher, cipherBytes.ElementAt(typeIndex), kilobytesPerSec);
                            }

                            typeIndex++;
                        }
                    }
                }

                if (parsedSuccessfully)
                {
                    results = cipherResults;
                }
            }

            return parsedSuccessfully;
        }

        private bool TryParseSignVerifyPerformanceResults(out DataTable results)
        {
            results = null;
            bool parsedSuccessfully = false;

            IEnumerable<string> rsaColumns = new List<string>()
            {
                "sign",
                "verify",
                "sign/s",
                "verify/s"
            };

            // Example:
            //                    sign verify    sign/s verify/s
            //  rsa 2048 bits 0.000820s 0.000024s   1219.7  41003.9
            MatchCollection rsaPerformanceResults = Regex.Matches(this.RawText, $@"((?:\w *\(*)+(?:bits|\)))(\s*[0-9\.]+s)(\s*[0-9\.]+s)(\s*[0-9\.]+)(\s*[0-9\.]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (rsaPerformanceResults?.Any() == true)
            {
                // return datatable with rsa name, column, value per row
                DataTable rsaResults = new DataTable();
                rsaResults.Columns.AddRange(new DataColumn[]
                {
                    new DataColumn(OpenSslMetricsParser.ColumnCipher, typeof(string)),
                    new DataColumn(OpenSslMetricsParser.ColumnUnit, typeof(string)),
                    new DataColumn(OpenSslMetricsParser.ColumnValue, typeof(double)),
                });

                foreach (Match match in rsaPerformanceResults)
                {
                    if (match.Groups.Count == 6
                        && match.Groups[2].Captures?.Any() == true)
                    {
                        int typeIndex = 0;
                        string rsaAlgorithm = match.Groups[1].Value.Trim();
                        for (int i = 2; i < 6; i++)
                        {
                            Match numericMatch = Regex.Match(match.Groups[i].Value, @"[-0-9\.]+", RegexOptions.IgnoreCase);
                            if (numericMatch.Success)
                            {
                                parsedSuccessfully = true;
                                double value = double.Parse(numericMatch.Value.Trim());
                                rsaResults.Rows.Add(rsaAlgorithm, rsaColumns.ElementAt(typeIndex), value);
                            }

                            typeIndex++;
                        }
                    }
                }

                if (parsedSuccessfully)
                {
                    results = rsaResults;
                }
            }

            return parsedSuccessfully;
        }
    }
}
