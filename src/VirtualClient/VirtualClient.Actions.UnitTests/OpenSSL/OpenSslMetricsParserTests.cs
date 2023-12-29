// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class OpenSslMetricsParserTests
    {
        // Results containing all ciphers and all types/byte sizes.
        private static string examplesDir;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            try
            {
                OpenSslMetricsParserTests.examplesDir = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Examples", "OpenSSL");
            }
            catch (FileNotFoundException exc)
            {
                Assert.Fail($"One or more of the expected OpenSSL results file examples is missing. {exc.Message}");
            }
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_SingleCipher_Scenario()
        {
            /* In this scenario, we are evaluating a single cipher as well as all byte buffer sizes.
             
               Example:
               type             16 bytes     64 bytes    256 bytes   1024 bytes   8192 bytes  16384 bytes
               sha256           98952.23k   300483.21k   717624.35k  1098886.72k  1296898.18k  1316783.17k
             */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-aes-256-cbc-results.txt")),
                "speed -elapsed -seconds 10 aes-256-cbc");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.AreEqual(6, metrics.Count());
            Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond));

            OpenSslMetricsParserTests.AssertMetricsMatch("AES-256-CBC", metrics, new Dictionary<string, double>
            {
                { "AES-256-CBC 16-byte", 1109022.36 },
                { "AES-256-CBC 64-byte", 1209391.13 },
                { "AES-256-CBC 256-byte", 1232451.87 },
                { "AES-256-CBC 1024-byte", 1237247.28 },
                { "AES-256-CBC 8192-byte", 1239375.05 },
                { "AES-256-CBC 16384-byte", 1239090.79 },
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_SingleCipher_MultiThreaded_Scenario()
        {
            /* In this scenario, we are evaluating a single cipher as well as all byte buffer sizes
               while running multi-threaded. OpenSSL does not include a header in the results when
               using the -multi option.

               Example:
               version: 3.0.0-beta3-dev
               built on: built on: Thu Aug  5 18:45:56 2021 UTC
               options:bn(64,64) 
               compiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG
               CPUINFO: OPENSSL_ia32cap=0xfeda32235f8bffff:0x1c2fb9
               md5        971380.35k   1693511.81k   2079132.16k  2200152.47k  2133075.56k  2108594.59k
           */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                 File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-md5-results-multi.txt")),
                "speed -elapsed -seconds 10 -multi 4 md5");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsTrue(metrics.Count() == 6);
            Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond));

            OpenSslMetricsParserTests.AssertMetricsMatch("md5", metrics, new Dictionary<string, double>
            {
                { "md5 16-byte", 971380.35 },
                { "md5 64-byte", 1693511.81 },
                { "md5 256-byte", 2079132.16 },
                { "md5 1024-byte", 2200152.47 },
                { "md5 8192-byte", 2133075.56 },
                { "md5 16384-byte", 2108594.59 }
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_SingleCipher_MultiThreaded_Scenario2()
        {
            /* In this scenario, we are evaluating a single cipher as well as a single buffer size
               while running multi-threaded. OpenSSL does not include a header in the results when
               using the -multi option.

               Example:
               version: 3.0.0-beta3-dev
               built on: built on: Thu Aug  5 18:45:56 2021 UTC
               options:bn(64,64) 
               compiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG
               CPUINFO: OPENSSL_ia32cap=0xfeda32235f8bffff:0x1c2fb9
               md5       2108594.59k
           */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                 File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-md5-results-multi-with-type-subset.txt")),
                "speed -elapsed -seconds 10 -multi 4 -bytes 16384 md5");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsTrue(metrics.Count() == 1);
            Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond));

            OpenSslMetricsParserTests.AssertMetricsMatch("md5", metrics, new Dictionary<string, double>
            {
                { "md5 16384-byte", 2108594.59 }
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_SingleCipher_MultiThreaded_Scenario3()
        {
            /* In this scenario, we are evaluating a single cipher as well as a single buffer size
               while running multi-threaded. OpenSSL does not include a header in the results when
               using the -multi option.

               Example:
               version: 3.0.0-beta3-dev
               built on: built on: Thu Aug  5 18:45:56 2021 UTC
               options:bn(64,64) 
               compiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG
               CPUINFO: OPENSSL_ia32cap=0xfeda32235f8bffff:0x1c2fb9
               md5       2108594.59k
           */

            // More than 1 -bytes parameter defined. Only the last one will be used.
            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                 File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-md5-results-multi-with-type-subset.txt")),
                "speed -elapsed -seconds 10 -multi 4 -bytes 8192 -bytes 16384 md5");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsTrue(metrics.Count() == 1);
            Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond));

            OpenSslMetricsParserTests.AssertMetricsMatch("md5", metrics, new Dictionary<string, double>
            {
                { "md5 16384-byte", 2108594.59 }
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_RSAAlgorithm_Scenario()
        {
            /* In this scenario, we are evaluating a single cipher as well as all byte buffer sizes.
             
               Example:
                                sign    verify    sign/s verify/s
                rsa 2048 bits 0.000820s 0.000024s   1219.7  41003.9
             */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-rsa2048-results.txt")),
                "speed -elapsed -seconds 10 rsa2048");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsTrue(metrics.Count() == 4);

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 2048 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 2048 bits sign", 0.000790 },
                { "rsa 2048 bits verify", 0.000015 },
                { "rsa 2048 bits sign/s", 1316.7 },
                { "rsa 2048 bits verify/s", 42004.9 }
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_ed25519_Scenario()
        {
            /* In this scenario, we are evaluating a single cipher as well as all byte buffer sizes.
             
               Example:
                                              sign    verify    sign/s verify/s
                 253 bits EdDSA (Ed25519)   0.0000s   0.0000s 261116.2  78003.2
             */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-multi-ed25519.txt")),
                "speed -multi 16 -seconds 5 ed25519");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.AreEqual(4, metrics.Count());

            OpenSslMetricsParserTests.AssertMetricsMatch("253 bits EdDSA (Ed25519)", metrics, new Dictionary<string, double>
            {
                { "253 bits EdDSA (Ed25519) sign", 0 },
                { "253 bits EdDSA (Ed25519) verify", 0 },
                { "253 bits EdDSA (Ed25519) sign/s", 261116.2 },
                { "253 bits EdDSA (Ed25519) verify/s", 78003.2 }
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_AllCiphers_Scenario()
        {
            /* In this scenario, we are evaluating all ciphers as well as all byte buffer sizes.
             
               Example:
               type             16 bytes     64 bytes    256 bytes   1024 bytes   8192 bytes  16384 bytes
               md5              86478.31k   234491.80k   475166.47k   643611.03k   718366.04k   724760.44k
               sha1            112072.53k   357766.41k   871397.13k  1368425.47k  1639579.37k  1664715.98k
               sha256           98952.23k   300483.21k   717624.35k  1098886.72k  1296898.18k  1316783.17k
             */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-results.txt")),
                "speed -elapsed -seconds 10");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.AreEqual(224, metrics.Count());
            // Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond)); --> changed with inclusion of RSA coverage

            OpenSslMetricsParserTests.AssertMetricsMatch("md5", metrics, new Dictionary<string, double>
            {
                { "md5 16-byte", 86478.31 },
                { "md5 64-byte", 234491.80 },
                { "md5 256-byte", 475166.47 },
                { "md5 1024-byte", 643611.03 },
                { "md5 8192-byte", 718366.04 },
                { "md5 16384-byte", 724760.44 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha1", metrics, new Dictionary<string, double>
            {
                { "sha1 16-byte", 112072.53 },
                { "sha1 64-byte", 357766.41 },
                { "sha1 256-byte", 871397.13 },
                { "sha1 1024-byte", 1368425.47 },
                { "sha1 8192-byte", 1639579.37 },
                { "sha1 16384-byte", 1664715.98 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha256", metrics, new Dictionary<string, double>
            {
                { "sha256 16-byte", 98952.23 },
                { "sha256 64-byte", 300483.21 },
                { "sha256 256-byte", 717624.35 },
                { "sha256 1024-byte", 1098886.72 },
                { "sha256 8192-byte", 1296898.18 },
                { "sha256 16384-byte", 1316783.17 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha512", metrics, new Dictionary<string, double>
            {
                { "sha512 16-byte", 45048.15 },
                { "sha512 64-byte", 180506.46 },
                { "sha512 256-byte", 340705.2 },
                { "sha512 1024-byte", 518054.81 },
                { "sha512 8192-byte", 620266.84 },
                { "sha512 16384-byte", 631089.29 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("hmac(md5)", metrics, new Dictionary<string, double>
            {
                { "hmac(md5) 16-byte", 59612.33 },
                { "hmac(md5) 64-byte", 180417.03 },
                { "hmac(md5) 256-byte", 413875.75 },
                { "hmac(md5) 1024-byte", 611803.72 },
                { "hmac(md5) 8192-byte", 712230.5 },
                { "hmac(md5) 16384-byte", 719883.47 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("des-ede3", metrics, new Dictionary<string, double>
            {
                { "des-ede3 16-byte", 28731.24 },
                { "des-ede3 64-byte", 29099.97 },
                { "des-ede3 256-byte", 29181.98 },
                { "des-ede3 1024-byte", 29235.1 },
                { "des-ede3 8192-byte", 29230.97 },
                { "des-ede3 16384-byte", 29238.89 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-128-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-128-cbc 16-byte", 1569302.45 },
                { "aes-128-cbc 64-byte", 1589805.17 },
                { "aes-128-cbc 256-byte", 1657786.91 },
                { "aes-128-cbc 1024-byte", 1674053.7 },
                { "aes-128-cbc 8192-byte", 1677365.52 },
                { "aes-128-cbc 16384-byte", 1633415.99 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-192-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-192-cbc 16-byte", 912407.69 },
                { "aes-192-cbc 64-byte", 1160122.78 },
                { "aes-192-cbc 256-byte", 1286967.41 },
                { "aes-192-cbc 1024-byte", 1351096.97},
                { "aes-192-cbc 8192-byte", 1365794 },
                { "aes-192-cbc 16384-byte", 1343098.06 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-256-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-256-cbc 16-byte", 775919.04 },
                { "aes-256-cbc 64-byte", 1039918.13 },
                { "aes-256-cbc 256-byte", 1138510.81 },
                { "aes-256-cbc 1024-byte", 1153518.87 },
                { "aes-256-cbc 8192-byte", 1156818.26 },
                { "aes-256-cbc 16384-byte", 1176283.27 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-128-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-128-cbc 16-byte", 99896.12 },
                { "camellia-128-cbc 64-byte", 153483.42 },
                { "camellia-128-cbc 256-byte", 178735.37 },
                { "camellia-128-cbc 1024-byte", 180291.82 },
                { "camellia-128-cbc 8192-byte", 180676.2 },
                { "camellia-128-cbc 16384-byte", 187749.17 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-192-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-192-cbc 16-byte", 84453.66 },
                { "camellia-192-cbc 64-byte", 115236.42 },
                { "camellia-192-cbc 256-byte", 130669.72 },
                { "camellia-192-cbc 1024-byte", 132710.3 },
                { "camellia-192-cbc 8192-byte", 136902.79 },
                { "camellia-192-cbc 16384-byte", 138821.09 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-256-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-256-cbc 16-byte", 69699.13 },
                { "camellia-256-cbc 64-byte", 109973.58 },
                { "camellia-256-cbc 256-byte", 129509.29 },
                { "camellia-256-cbc 1024-byte", 136012.77 },
                { "camellia-256-cbc 8192-byte", 138190.85 },
                { "camellia-256-cbc 16384-byte", 138823.82  }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("ghash", metrics, new Dictionary<string, double>
            {
                { "ghash 16-byte", 488056.36 },
                { "ghash 64-byte", 1614821.05 },
                { "ghash 256-byte", 4198891.63 },
                { "ghash 1024-byte", 6159644.33 },
                { "ghash 8192-byte", 8829444.92 },
                { "ghash 16384-byte", 8812728.05 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rand", metrics, new Dictionary<string, double>
            {
                { "rand 16-byte", 12140.14 },
                { "rand 64-byte", 53908.09 },
                { "rand 256-byte", 196501.89 },
                { "rand 1024-byte", 1062619.07 },
                { "rand 8192-byte", 4221050.33 },
                { "rand 16384-byte", 5279181.48 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa  512 bits", metrics, new Dictionary<string, double>
            {
                { "rsa  512 bits sign", 0.000038 },
                { "rsa  512 bits verify", 0.000002 },
                { "rsa  512 bits sign/s", 26538.8 },
                { "rsa  512 bits verify/s", 432400.4 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 1024 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 1024 bits sign", 0.000099 },
                { "rsa 1024 bits verify", 0.000006 },
                { "rsa 1024 bits sign/s", 10096.9 },
                { "rsa 1024 bits verify/s", 172208.4 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 2048 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 2048 bits sign", 0.000320 },
                { "rsa 2048 bits verify", 0.000019 },
                { "rsa 2048 bits sign/s", 3121.9 },
                { "rsa 2048 bits verify/s", 52842.2 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 3072 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 3072 bits sign", 0.001967 },
                { "rsa 3072 bits verify", 0.000040 },
                { "rsa 3072 bits sign/s", 508.3 },
                { "rsa 3072 bits verify/s", 24786.9 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 4096 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 4096 bits sign", 0.004418 },
                { "rsa 4096 bits verify", 0.000068 },
                { "rsa 4096 bits sign/s", 226.4 },
                { "rsa 4096 bits verify/s", 14620.4 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 7680 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 7680 bits sign", 0.041450 },
                { "rsa 7680 bits verify", 0.000231 },
                { "rsa 7680 bits sign/s", 24.1 },
                { "rsa 7680 bits verify/s", 4323.9 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa 15360 bits", metrics, new Dictionary<string, double>
            {
                { "rsa 15360 bits sign", 0.210629 },
                { "rsa 15360 bits verify", 0.000914 },
                { "rsa 15360 bits sign/s", 4.7 },
                { "rsa 15360 bits verify/s", 1094.3 }
            });
        }

        [Test]
        public void OpenSslParserParsesResultsCorrectly_AllCiphers_BufferByteSizeSubset_Scenario()
        {
            /* In this scenario, we are evaluating all ciphers but are not evaluating all type/byte buffer sizes.
               For example here, we are just evaluating 8192 and 16384 byte buffer sizes.

               Example:
               type            16384 bytes
               md5              724760.44k
               sha1            1664715.98k
               sha256          1316783.17k
             */
            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-results-with-type-subset.txt")),
                "speed -elapsed -seconds 10 -bytes 16384");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.AreEqual(154, metrics.Count());
            // Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond)); --> changed with inclusion of RSA coverage

            OpenSslMetricsParserTests.AssertMetricsMatch("md5", metrics, new Dictionary<string, double>
            {
                { "md5 16384-byte", 724760.44 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha1", metrics, new Dictionary<string, double>
            {
                { "sha1 16384-byte", 1664715.98 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha256", metrics, new Dictionary<string, double>
            {
                { "sha256 16384-byte", 1316783.17 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha512", metrics, new Dictionary<string, double>
            {
                { "sha512 16384-byte", 631089.29 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("hmac(md5)", metrics, new Dictionary<string, double>
            {
                { "hmac(md5) 16384-byte", 719883.47 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("des-ede3", metrics, new Dictionary<string, double>
            {
                { "des-ede3 16384-byte", 29238.89 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-128-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-128-cbc 16384-byte", 1633415.99 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-192-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-192-cbc 16384-byte", 1343098.06 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-256-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-256-cbc 16384-byte", 1176283.27 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-128-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-128-cbc 16384-byte", 187749.17 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-192-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-192-cbc 16384-byte", 138821.09 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-256-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-256-cbc 16384-byte", 138823.82  }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("ghash", metrics, new Dictionary<string, double>
            {
                { "ghash 16384-byte", 8812728.05 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rand", metrics, new Dictionary<string, double>
            {
                { "rand 16384-byte", 5279181.48 }
            });
        }

        [Test]
        public void OpenSslParserHandlesAnomaliesWhereTheResultsShowAsANegativeNumber()
        {
            /* In this scenario, we are evaluating that we can handle the anomaly where the results are
               a negative number. This indicates some type of issue in the workload itself. We simply exclude
               these results because they are not a valid measurement.
             
               Example:
               type             16 bytes     64 bytes    256 bytes   1024 bytes   8192 bytes  16384 bytes
               sha1            -112072      357766.41k   871397.13k  1368425.47k  1639579.37k  1664715.98k
               sha256          -98952.23    300483.21k   717624.35k  1098886.72k  1296898.18k  1316783.17k
             */

            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                File.ReadAllText(Path.Combine(OpenSslMetricsParserTests.examplesDir, "OpenSSL-speed-results-with-anomalies.txt")),
                "speed -elapsed -seconds 10");

            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.AreEqual(206, metrics.Count());
            // Assert.IsTrue(metrics.All(m => m.Unit == MetricUnit.KilobytesPerSecond)); --> changed with inclusion of RSA coverage

            OpenSslMetricsParserTests.AssertMetricsMatch("md5", metrics, new Dictionary<string, double>
            {
                { "md5 64-byte", 234491.80 },
                { "md5 256-byte", 475166.47 },
                { "md5 1024-byte", 643611.03 },
                { "md5 8192-byte", 718366.04 },
                { "md5 16384-byte", 724760.44 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha1", metrics, new Dictionary<string, double>
            {
                { "sha1 64-byte", 357766.41 },
                { "sha1 256-byte", 871397.13 },
                { "sha1 1024-byte", 1368425.47 },
                { "sha1 8192-byte", 1639579.37 },
                { "sha1 16384-byte", 1664715.98 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha256", metrics, new Dictionary<string, double>
            {
                { "sha256 256-byte", 717624.35 },
                { "sha256 1024-byte", 1098886.72 },
                { "sha256 8192-byte", 1296898.18 },
                { "sha256 16384-byte", 1316783.17 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("sha512", metrics, new Dictionary<string, double>
            {
                { "sha512 256-byte", 340705.2 },
                { "sha512 1024-byte", 518054.81 },
                { "sha512 8192-byte", 620266.84 },
                { "sha512 16384-byte", 631089.29 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("hmac(md5)", metrics, new Dictionary<string, double>
            {
                { "hmac(md5) 16-byte", 59612.33 },
                { "hmac(md5) 64-byte", 180417.03 },
                { "hmac(md5) 256-byte", 413875.75 },
                { "hmac(md5) 1024-byte", 611803.72 },
                { "hmac(md5) 8192-byte", 712230.5 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("des-ede3", metrics, new Dictionary<string, double>
            {
                { "des-ede3 16-byte", 28731.24 },
                { "des-ede3 64-byte", 29099.97 },
                { "des-ede3 1024-byte", 29235.1 },
                { "des-ede3 8192-byte", 29230.97 },
                { "des-ede3 16384-byte", 29238.89 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-128-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-128-cbc 64-byte", 1589805.17 },
                { "aes-128-cbc 256-byte", 1657786.91 },
                { "aes-128-cbc 1024-byte", 1674053.7 },
                { "aes-128-cbc 16384-byte", 1633415.99 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-192-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-192-cbc 256-byte", 1286967.41 },
                { "aes-192-cbc 1024-byte", 1351096.97},
                { "aes-192-cbc 8192-byte", 1365794 },
                { "aes-192-cbc 16384-byte", 1343098.06 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("aes-256-cbc", metrics, new Dictionary<string, double>
            {
                { "aes-256-cbc 64-byte", 1039918.13 },
                { "aes-256-cbc 256-byte", 1138510.81 },
                { "aes-256-cbc 1024-byte", 1153518.87 },
                { "aes-256-cbc 8192-byte", 1156818.26 },
                { "aes-256-cbc 16384-byte", 1176283.27 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-128-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-128-cbc 16-byte", 99896.12 },
                { "camellia-128-cbc 64-byte", 153483.42 },
                { "camellia-128-cbc 1024-byte", 180291.82 },
                { "camellia-128-cbc 8192-byte", 180676.2 },
                { "camellia-128-cbc 16384-byte", 187749.17 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-192-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-192-cbc 64-byte", 115236.42 },
                { "camellia-192-cbc 256-byte", 130669.72 },
                { "camellia-192-cbc 1024-byte", 132710.3 },
                { "camellia-192-cbc 8192-byte", 136902.79 },
                { "camellia-192-cbc 16384-byte", 138821.09 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("camellia-256-cbc", metrics, new Dictionary<string, double>
            {
                { "camellia-256-cbc 16-byte", 69699.13 },
                { "camellia-256-cbc 64-byte", 109973.58 },
                { "camellia-256-cbc 256-byte", 129509.29 },
                { "camellia-256-cbc 1024-byte", 136012.77 },
                { "camellia-256-cbc 8192-byte", 138190.85 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("ghash", metrics, new Dictionary<string, double>
            {
                { "ghash 64-byte", 1614821.05 },
                { "ghash 256-byte", 4198891.63 },
                { "ghash 1024-byte", 6159644.33 },
                { "ghash 8192-byte", 8829444.92 },
                { "ghash 16384-byte", 8812728.05 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rand", metrics, new Dictionary<string, double>
            {
                { "rand 16-byte", 12140.14 },
                { "rand 64-byte", 53908.09 },
                { "rand 256-byte", 196501.89 },
                { "rand 1024-byte", 1062619.07 },
                { "rand 8192-byte", 4221050.33 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("rsa  512", metrics, new Dictionary<string, double>
            {
                { "rsa  512 bits sign", 0.000038 },
                { "rsa  512 bits verify", 0.000002 },
                { "rsa  512 bits sign/s", 26538.8 },
                { "rsa  512 bits verify/s", 432400.4 }
            });

            OpenSslMetricsParserTests.AssertMetricsMatch("256 bits SM2 (CurveSM2)", metrics, new Dictionary<string, double>
            {
                { "256 bits SM2 (CurveSM2) sign", 0.0003 },
                { "256 bits SM2 (CurveSM2) verify", 0.0003 },
                { "256 bits SM2 (CurveSM2) sign/s", 2862.3 },
                { "256 bits SM2 (CurveSM2) verify/s", 3079.5 }
            });
        }

        [Test]
        public void OpenSslParserThrowsIfTheResultsProvidedAreNotValid()
        {
            OpenSslMetricsParser parser = new OpenSslMetricsParser(
                "These are not valid OpenSSL speed command results.",
                "speed -elapsed -seconds 10");

            Assert.Throws<SchemaException>(() => parser.Parse());
        }

        private static void AssertMetricsMatch(string cipher, IEnumerable<Metric> metrics, IDictionary<string, double> expectedResults)
        {
            IEnumerable<Metric> cipherMetrics = metrics.Where(m => m.Name.StartsWith(cipher));
            Assert.IsNotNull(cipherMetrics, $"Metrics for cipher '{cipher}' not found.");
            Assert.IsNotEmpty(cipherMetrics, $"Metrics for cipher '{cipher}' not found.");
            Assert.AreEqual(expectedResults.Count(), cipherMetrics.Count(), $"Expected metrics not defined for cipher '{cipher}'.");

            for (int i = 0; i < cipherMetrics.Count(); i++)
            {
                Metric metric = cipherMetrics.ElementAt(i);
                KeyValuePair<string, double> expectedResult = expectedResults.ElementAt(i);

                Assert.AreEqual(
                    expectedResult.Key,
                    metric.Name,
                    $"Name does not match for metric '{metric.Name}' (expected={expectedResult.Key}, actual={metric.Name})");

                Assert.AreEqual(
                    expectedResult.Value,
                    metric.Value,
                    $"Value does not match for metric '{metric.Name}' (expected={expectedResult.Value}, actual={metric.Value})");
            }
        }
    }
}