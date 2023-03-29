// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Castle.Components.DictionaryAdapter.Xml;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class ProfileExpressionEvaluatorTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPackagePathLocationReferencesOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PackagePath:anyPackage}", packagePath },
                { "--any-path={PackagePath:anyPackage}", $"--any-path={packagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPackagePathLocationReferencesOnUnixSystems_MoreAdvancedScenarios()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                {
                    "{PackagePath:anyPackage}/configure&&{PackagePath:anyPackage}/make&&{PackagePath:anyPackage}/make install",
                    $"{packagePath}/configure&&{packagePath}/make&&{packagePath}/make install"
                },
                {
                    "bash -c \"{PackagePath:anyPackage}/any.folder.1.2.3/execute.sh\"",
                    $"bash -c \"{packagePath}/any.folder.1.2.3/execute.sh\""
                }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        [TestCase("any-package")]
        [TestCase("any_package")]
        [TestCase("any.package")]
        [TestCase("any package")]
        public void ProfileExpressionEvaluatorSupportsPackagePathLocationReferencesThatIncludeSupportedNonAlphanumericCharactersOnUnixSystems(string packageName)
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath(packageName);

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage(packageName).ReturnsAsync(new DependencyPath(packageName, packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { $"{{PackagePath:{packageName}}}", packagePath },
                { $"--any-path={{PackagePath:{packageName}}}", $"--any-path={packagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPackagePathLocationReferencesOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PackagePath:anyPackage}", packagePath },
                { "--any-path={PackagePath:anyPackage}", $"--any-path={packagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPackagePathLocationReferencesOnWindowsSystems_MoreAdvancedScenarios()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                {
                    "{PackagePath:anyPackage}\\configure.cmd&&{PackagePath:anyPackage}\\build.exe&&{PackagePath:anyPackage}\\build.exe install",
                    $"{packagePath}\\configure.cmd&&{packagePath}\\build.exe&&{packagePath}\\build.exe install"
                },
                {
                    "powershell -C \"{PackagePath:anyPackage}\\any.folder.1.2.3\\execute.exe\"",
                    $"powershell -C \"{packagePath}\\any.folder.1.2.3\\execute.exe\""
                }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorPackagePathLocationReferenceExpressionsAreNotCaseSensitive()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PackagePath:anyPackage}", packagePath },
                { "{packagepath:anyPackage}", packagePath },
                { "{PACKAGEPATH:anyPackage}", packagePath }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        [TestCase("any-package")]
        [TestCase("any_package")]
        [TestCase("any.package")]
        [TestCase("any package")]
        public void ProfileExpressionEvaluatorSupportsPackagePathLocationReferencesThatIncludeSupportedNonAlphanumericCharactersOnWindowsSystems(string packageName)
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath(packageName);

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage(packageName).ReturnsAsync(new DependencyPath(packageName, packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { $"{{PackagePath:{packageName}}}", packagePath },
                { $"--any-path={{PackagePath:{packageName}}}", $"--any-path={packagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorThrowsOnEvaluationOfPackagePathsWhenThePackageDoesNotExist()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package is NOT registered with VC (i.e. does not exist).
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            DependencyException error = Assert.Throws<DependencyException>(
                () => ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, "{PackagePath:anyPackage}"));

            Assert.AreEqual(ErrorReason.DependencyNotFound, error.Reason);
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPlatformSpecificPackagePathLocationReferencesOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "linux-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PackagePath/Platform:anyPackage}", platformSpecificPackagePath },
                { "--any-path={PackagePath/Platform:anyPackage}", $"--any-path={platformSpecificPackagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPlatformSpecificPackagePathLocationReferencesOnUnixSystems_MoreAdvancedScenarios()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "linux-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                {
                    "{PackagePath/Platform:anyPackage}/configure&&{PackagePath/Platform:anyPackage}/make&&{PackagePath/Platform:anyPackage}/make install",
                    $"{platformSpecificPackagePath}/configure&&{platformSpecificPackagePath}/make&&{platformSpecificPackagePath}/make install"
                },
                {
                    "bash -c \"{PackagePath/Platform:anyPackage}/any.folder.1.2.3/execute.sh\"",
                    $"bash -c \"{platformSpecificPackagePath}/any.folder.1.2.3/execute.sh\""
                }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorPlatformSpecificPackagePathLocationReferenceExpressionsAreNotCaseSensitive()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "linux-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PackagePath/Platform:anyPackage}", platformSpecificPackagePath },
                { "{packagepath/platform:anyPackage}", platformSpecificPackagePath },
                { "{PACKAGEPATH/PLATFORM:anyPackage}", platformSpecificPackagePath }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        [TestCase("any-package")]
        [TestCase("any_package")]
        [TestCase("any.package")]
        [TestCase("any package")]
        public void ProfileExpressionEvaluatorSupportsPlatformSpecificPackagePathLocationReferencesThatIncludeSupportedNonAlphanumericCharactersOnUnixSystems(string packageName)
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath(packageName);
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "linux-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage(packageName).ReturnsAsync(new DependencyPath(packageName, packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { $"{{PackagePath/Platform:{packageName}}}", platformSpecificPackagePath },
                { $"--any-path={{PackagePath/Platform:{packageName}}}", $"--any-path={platformSpecificPackagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPlatformSpecificPackagePathLocationReferencesOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "win-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PackagePath/Platform:anyPackage}", platformSpecificPackagePath },
                { "--any-path={PackagePath/Platform:anyPackage}", $"--any-path={platformSpecificPackagePath}" }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPlatformSpecificPackagePathLocationReferencesOnWindowsSystems_MoreAdvancedScenarios()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "win-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                {
                    "{PackagePath/Platform:anyPackage}\\configure.cmd&&{PackagePath/Platform:anyPackage}\\build.exe&&{PackagePath/Platform:anyPackage}\\build.exe install",
                    $"{platformSpecificPackagePath}\\configure.cmd&&{platformSpecificPackagePath}\\build.exe&&{platformSpecificPackagePath}\\build.exe install"
                },
                {
                    "powershell -C \"{PackagePath/Platform:anyPackage}\\any.folder.1.2.3\\execute.exe\"",
                    $"powershell -C \"{platformSpecificPackagePath}\\any.folder.1.2.3\\execute.exe\""
                }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorThrowsOnEvaluationOfPlatformSpecificPackagePathsWhenThePackageDoesNotExist()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");

            // The package is NOT registered with VC (i.e. does not exist).
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            DependencyException error = Assert.Throws<DependencyException>(
                () => ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, "{PackagePath/Platform:anyPackage}"));

            Assert.AreEqual(ErrorReason.DependencyNotFound, error.Reason);
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsLogicalCoreCountReferences()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { 
                    "{LogicalCoreCount}",
                    Environment.ProcessorCount.ToString()
                },
                {
                    "--port=1234 --threads={LogicalCoreCount}",
                    $"--port=1234 --threads={Environment.ProcessorCount}"
                },
                {
                    "--port=1234 --threads={LogicalCoreCount} --someFlag --clients={LogicalCoreCount}",
                    $"--port=1234 --threads={Environment.ProcessorCount} --someFlag --clients={Environment.ProcessorCount}"
                }
            };

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsPhysicalCoreCountReferences()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            Dictionary<string, string> expressions = new Dictionary<string, string>
            {
                { "{PhysicalCoreCount}", "4" },
                { "--port=1234 --threads={PhysicalCoreCount}", $"--port=1234 --threads=4" },
                { "--port=1234 --threads={PhysicalCoreCount} --someFlag --clients={PhysicalCoreCount}", $"--port=1234 --threads=4 --someFlag --clients=4" }
            };

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Any CPU", "Any description", physicalCoreCount: 4, logicalCoreCount: 8, socketCount: 2, numaNodeCount: 1, hyperThreadingEnabled: true));

            foreach (var entry in expressions)
            {
                string expectedExpression = entry.Value;
                string actualExpression = ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, entry.Key);
                Assert.AreEqual(expectedExpression, actualExpression);
            }
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsExpressionReferencesInParameterSets()
        {
            this.SetupDefaults(PlatformID.Unix);
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { "CommandLine", "--port={Port} --threads={ThreadCount}" },
                { "Port" , 1234 },
                { "ThreadCount" , 8 }
            };

            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters);

            Assert.AreEqual("--port=1234 --threads=8", parameters["CommandLine"].ToString());
            Assert.AreEqual(1234, parameters["Port"]);
            Assert.AreEqual(8, parameters["ThreadCount"]);
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsExpressionReferencesInParameterSets_AllIConvertibles()
        {
            this.SetupDefaults(PlatformID.Unix);

            DateTime now = DateTime.UtcNow;
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { "CommandLine", "--int={Int} --double={Double} --decimal={Decimal} --byte={Byte} --char={Char} --bool={Boolean} --dateTime={DateTime}" },
                { "Int" , 1234 },
                { "Double" , 8.15 },
                { "Decimal", 10.55M },
                { "Byte", (byte)128 },
                { "Char", 'a' },
                { "Boolean", true },
                { "DateTime", now }
            };

            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters);

            Assert.AreEqual(
                $"--int=1234 --double=8.15 --decimal=10.55 --byte=128 --char=a --bool=True --dateTime={now.ToString()}",
                parameters["CommandLine"].ToString());
        }


        [Test]
        public void ProfileExpressionEvaluatorSupportsExpressionReferencesInParameterSets_WithDuplicateExpressions()
        {
            this.SetupDefaults(PlatformID.Unix);
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { "CommandLine", "--port={Port} --threads={ThreadCount} --serverPort={Port} --clients={ThreadCount}" },
                { "Port" , 1234 },
                { "ThreadCount" , 8 }
            };

            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters);

            Assert.AreEqual("--port=1234 --threads=8 --serverPort=1234 --clients=8", parameters["CommandLine"].ToString());
            Assert.AreEqual(1234, parameters["Port"]);
            Assert.AreEqual(8, parameters["ThreadCount"]);
        }

        [Test]
        public void ProfileExpressionEvaluatorSupportsWellKnownExpressionReferencesInParameterSets()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anyPackage");
            string platformSpecificPackagePath = this.mockFixture.Combine(packagePath, "linux-x64");

            // The package MUST be actually registered with VC.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anyPackage", packagePath));

            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { "CommandLine", "--port=1234 --threads={LogicalCoreCount} --package={PackagePath:anypackage} --package2={PackagePath/Platform:anypackage}" },
            };

            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters);

            Assert.AreEqual(
                $"--port=1234 --threads={Environment.ProcessorCount} --package={packagePath} --package2={packagePath}/linux-x64",
                parameters["CommandLine"].ToString());
        }

        [Test]
        public void ProfileExpressionEvaluatorParameterSetExpressionsFollowTheCaseSensitivityOfTheParameterDictionary()
        {
            this.SetupDefaults(PlatformID.Unix);

            // Case-sensitive
            IDictionary<string, IConvertible> parameters1 = new Dictionary<string, IConvertible>(StringComparer.Ordinal)
            {
                { "CommandLine", "--port={Port}" },
                { "Port" , 1234 }
            };

            IDictionary<string, IConvertible> parameters2 = new Dictionary<string, IConvertible>(StringComparer.Ordinal)
            {
                { "CommandLine", "--port={port}" },
                { "Port" , 1234 }
            };

            // Match
            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters1);
            Assert.AreEqual("--port=1234", parameters1["CommandLine"].ToString());

            // No Match because of the casing difference
            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters2);
            Assert.AreEqual("--port={port}", parameters2["CommandLine"].ToString());

            // Not case-sensitive
            parameters1 = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "CommandLine", "--port={Port}" },
                { "Port" , 1234 }
            };

            // Not case-sensitive
            parameters2 = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "CommandLine", "--port={port}" },
                { "Port" , 1234 }
            };

            // Match
            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters1);
            Assert.AreEqual("--port=1234", parameters1["CommandLine"].ToString());

            // Match
            ProfileExpressionEvaluator.Evaluate(this.mockFixture.Dependencies, parameters2);
            Assert.AreEqual("--port=1234", parameters2["CommandLine"].ToString());
        }
    }
}
