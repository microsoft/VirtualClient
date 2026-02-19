// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NetworkPingExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        public void NetworkPingExecutorThrowsIfIPAddressIsNotDefined()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "NotDefined" }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(ErrorReason.InstructionsNotProvided, exception.Reason);
                Assert.IsTrue(exception.Message.Contains("IP address"));
            }
        }

        [Test]
        [TestCase("")]
        [TestCase("invalid-ip")]
        [TestCase("999.999.999.999")]
        [TestCase("not.an.ip.address")]
        public void NetworkPingExecutorThrowsIfIPAddressIsInvalid(string invalidIpAddress)
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), invalidIpAddress }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(ErrorReason.InstructionsNotValid, exception.Reason);
                Assert.IsTrue(exception.Message.Contains("Invalid IP address format"));
            }
        }

        [Test]
        public void NetworkPingExecutorParsesIPAddressParameter()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual("192.168.1.1", executor.IPAddress);
            }
        }

        [Test]
        [TestCase("192.168.1.1")]
        [TestCase("10.0.0.1")]
        [TestCase("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
        [TestCase("::1")]
        public void NetworkPingExecutorAcceptsValidIPAddresses(string ipAddress)
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), ipAddress }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual(ipAddress, executor.IPAddress);
            }
        }

        [Test]
        public void NetworkPingExecutorUsesDefaultPingIterationsWhenNotSpecified()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual(50, executor.PingIterations);
            }
        }

        [Test]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        public void NetworkPingExecutorParsesPingIterationsParameter(int iterations)
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.PingIterations), iterations }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual(iterations, executor.PingIterations);
            }
        }

        [Test]
        public void NetworkPingExecutorDurationIsNullWhenNotSpecified()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsNull(executor.Duration);
            }
        }

        [Test]
        public void NetworkPingExecutorDurationIsNullWhenSetToEmptyString()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.Duration), string.Empty }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsNull(executor.Duration);
            }
        }

        [Test]
        public void NetworkPingExecutorDurationIsNullWhenSetToNull()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.Duration), null }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsNull(executor.Duration);
            }
        }

        [Test]
        [TestCase("00:05:00", 300)] // 5 minutes = 300 seconds
        [TestCase("00:10:00", 600)] // 10 minutes = 600 seconds
        [TestCase("01:00:00", 3600)] // 1 hour = 3600 seconds
        [TestCase("00:00:30", 30)] // 30 seconds
        public void NetworkPingExecutorParsesTimeSpanFormatForDuration(string durationString, int expectedSeconds)
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.Duration), durationString }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsNotNull(executor.Duration);
                Assert.AreEqual(expectedSeconds, executor.Duration.Value.TotalSeconds);
            }
        }

        [Test]
        [TestCase(300)] // 300 seconds
        [TestCase(600)] // 600 seconds
        [TestCase(3600)] // 3600 seconds
        public void NetworkPingExecutorParsesNumericFormatForDuration(int durationSeconds)
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.Duration), durationSeconds }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsNotNull(executor.Duration);
                Assert.AreEqual(durationSeconds, executor.Duration.Value.TotalSeconds);
            }
        }

        [Test]
        public void NetworkPingExecutorSupportsSettingBothDurationAndPingIterations()
        {
            // When Duration is set, it takes precedence over PingIterations
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.Duration), "00:05:00" },
                { nameof(NetworkPingExecutor.PingIterations), 100 }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsNotNull(executor.Duration);
                Assert.AreEqual(300, executor.Duration.Value.TotalSeconds);
                Assert.AreEqual(100, executor.PingIterations);
            }
        }

        [Test]
        public void NetworkPingExecutorParametersAreConsistentWithProfile()
        {
            // Verify that the parameters match what's expected in the PERF-NETWORK-PING.json profile
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(NetworkPingExecutor.IPAddress), "192.168.1.1" },
                { nameof(NetworkPingExecutor.Duration), null },
                { nameof(NetworkPingExecutor.PingIterations), 300 }
            };

            using (NetworkPingExecutor executor = new NetworkPingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual("192.168.1.1", executor.IPAddress);
                Assert.IsNull(executor.Duration);
                Assert.AreEqual(300, executor.PingIterations);
            }
        }
    }
}
