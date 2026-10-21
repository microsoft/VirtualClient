// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ParameterCompatibilityTests
    {
        private static IEnumerable<TestCaseData> ReaderCases
        {
            get
            {
                object[][] readers = new object[][]
                {
                    new object[] { typeof(ApacheBenchExecutor), "RequestCount", "NoOfRequests", 50000 },
                    new object[] { typeof(ApacheBenchExecutor), "ConcurrentRequestCount", "NoOfConcurrentRequests", 50 },
                    new object[] { typeof(CtsTrafficClientExecutor), "ConnectionCount", "Connections", 1 },
                    new object[] { typeof(CtsTrafficClientExecutor), "IterationCount", "Iterations", 1 },
                    new object[] { typeof(DotNetRuntimeExecutor), "JvmInstanceCount", "NumberOfJvmInstances", null },
                    new object[] { typeof(DotNetRuntimeExecutor), "WarehouseCount", "NumberOfWarehouses", null },
                    new object[] { typeof(HPLinpackExecutor), "ProcessCount", "NumberOfProcesses", 8 },
                    new object[] { typeof(HammerDBExecutor), "VirtualUserCount", "VirtualUsers", null },
                    new object[] { typeof(NetworkPingExecutor), "PingIterationCount", "PingIterations", 50 },
                    new object[] { typeof(NginxServerExecutor), "WorkerCount", "Workers", 0 },
                    new object[] { typeof(LatteClientExecutor2), "IterationCount", "Iterations", 100100 },
                    new object[] { typeof(LatteServerExecutor2), "IterationCount", "Iterations", 100100 },
                    new object[] { typeof(OpenFOAMExecutor), "IterationCount", "Iterations", null },
                    new object[] { typeof(RedisBenchmarkClientExecutor), "ClientInstanceCount", "ClientInstances", 1 },
                    new object[] { typeof(SysbenchExecutor), "ThreadCount", "Threads", null },
                    new object[] { typeof(MemcachedServerExecutor), "MaxServerConnectionCount", "ServerMaxConnections", null },
                    new object[] { typeof(TlsOpenSslClientExecutor), "ClientInstanceCount", "ClientInstances", 1 }
                };

                foreach (object[] reader in readers)
                {
                    foreach (string scenario in new string[] { "Legacy", "Canonical", "Both", "Default", "LegacyNull", "CanonicalNull", "InvalidLegacy", "InvalidCanonical" })
                    {
                        yield return new TestCaseData(reader[0], reader[1], reader[2], reader[3], scenario)
                            .SetName($"{((Type)reader[0]).Name}_{reader[1]}_{scenario}");
                    }
                }
            }
        }

        [TestCaseSource(nameof(ReaderCases))]
        public void CountReadersPreserveAliasesPrecedenceDefaultsAndConversions(
            Type executorType, string canonical, string legacy, object defaultValue, string scenario)
        {
            var fixture = new MockFixture();
            fixture.Setup(PlatformID.Unix, Architecture.X64);
            var parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            object expected = defaultValue;
            Type expectedException = null;
            bool required = executorType == typeof(MemcachedServerExecutor);
            bool stringValue = executorType.GetProperty(canonical).PropertyType == typeof(string);

            switch (scenario)
            {
                case "Legacy":
                    parameters[legacy] = "17";
                    expected = 17;
                    break;
                case "Canonical":
                    parameters[canonical] = "23";
                    expected = 23;
                    break;
                case "Both":
                    parameters[legacy] = "17";
                    parameters[canonical] = "23";
                    expected = 23;
                    break;
                case "Default":
                    expectedException = required ? typeof(KeyNotFoundException) : null;
                    break;
                case "LegacyNull":
                    parameters[legacy] = null;
                    expectedException = required ? typeof(InvalidCastException) : null;
                    break;
                case "CanonicalNull":
                    parameters[legacy] = "17";
                    parameters[canonical] = null;
                    expectedException = required ? typeof(InvalidCastException) : null;
                    break;
                case "InvalidLegacy":
                    parameters[legacy] = "invalid";
                    expected = "invalid";
                    expectedException = stringValue ? null : typeof(FormatException);
                    break;
                case "InvalidCanonical":
                    parameters[legacy] = "17";
                    parameters[canonical] = "invalid";
                    expected = "invalid";
                    expectedException = stringValue ? null : typeof(FormatException);
                    break;
            }

            using (var executor = (VirtualClientComponent)Activator.CreateInstance(executorType, fixture.Dependencies, parameters))
            {
                PropertyInfo property = executorType.GetProperty(canonical);
                if (expectedException != null)
                {
                    var error = Assert.Throws<TargetInvocationException>(() => property.GetValue(executor));
                    Assert.IsInstanceOf(expectedException, error.InnerException);
                }
                else
                {
                    Assert.AreEqual(expected?.ToString(), property.GetValue(executor)?.ToString());
                }
            }
        }
    }
}
