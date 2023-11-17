namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using Moq;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using VirtualClient.Actions.Wrathmark;
    using VirtualClient.Common;
    using VirtualClient.Contracts;


    public class WrathmarkExecutorTests : WrathmarkTests
    {
        [Test]
        public void ThrowsIfWrathmarkDependencyNotFound(
            [Values(PlatformID.Win32NT, PlatformID.Unix)] PlatformID platformId,
            [Values(Architecture.X64, Architecture.Arm64)] Architecture architecture)
        {
            // Arrange
            MockFixture mockFixture = SetupDefaultMockBehaviors(platformId, architecture);
            WrathmarkWorkloadExecutor sut = WorkloadExecutorFactory(mockFixture);

            // Remove the dependency for Wrathmark git repository
            mockFixture.PackageManager.OnGetPackage(Constants.Profile.Dependencies.WrathmarkPackageName).ReturnsAsync((DependencyPath)null);

            // Act & Assert
            Assert.ThrowsAsync<DependencyException>(() => sut.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        public void ThrowsIfDotNetSdkDependencyNotFound(
            [Values(PlatformID.Win32NT, PlatformID.Unix)] PlatformID platformId,
            [Values(Architecture.X64, Architecture.Arm64)] Architecture architecture)
        {
            // Arrange
            MockFixture mockFixture = SetupDefaultMockBehaviors(platformId, architecture);
            WrathmarkWorkloadExecutor sut = WorkloadExecutorFactory(mockFixture);

            // Remove the dependency for .NET
            mockFixture.PackageManager.OnGetPackage(Constants.Profile.Dependencies.DotNetSdkPackageName).ReturnsAsync((DependencyPath)null);

            // Act & Assert
            Assert.ThrowsAsync<DependencyException>(() => sut.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        public void ThrowsIfNoResults(
            [Values(PlatformID.Win32NT, PlatformID.Unix)] PlatformID platformId,
            [Values(Architecture.X64, Architecture.Arm64)] Architecture architecture)
        {
            // Arrange
            MockFixture mockFixture = SetupDefaultMockBehaviors(platformId, architecture);
            WrathmarkWorkloadExecutor sut = WorkloadExecutorFactory(mockFixture);

            mockFixture.ProcessManager.OnCreateProcess = (_, _, _) =>
            {
                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                // Can't be blank to satisfy the intermediate checks
                // Also testing that the executor fails when there are no results, so can't be a valid output either
                process.StandardOutput.AppendLine("This is not a valid result");

                return process;
            };

            // Act & Assert
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => sut.ExecuteAsync(CancellationToken.None));

            Assert.AreEqual(ErrorReason.InvalidResults,exception.Reason);
            Assert.AreEqual("WrathmarkMetricsParser could not parse results.", exception.Message);
        }

        [Test]
        public void ThrowsIfResultsNotEmptyButWhitespace(
            [Values(PlatformID.Win32NT, PlatformID.Unix)] PlatformID platformId,
            [Values(Architecture.X64, Architecture.Arm64)] Architecture architecture)
        {
            // Arrange
            MockFixture mockFixture = SetupDefaultMockBehaviors(platformId, architecture);
            WrathmarkWorkloadExecutor sut = WorkloadExecutorFactory(mockFixture);

            mockFixture.ProcessManager.OnCreateProcess = (_, _, _) =>
            {
                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            // Act & Assert
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => sut.ExecuteAsync(CancellationToken.None));

            Assert.AreEqual(ErrorReason.CriticalWorkloadFailure, exception.Reason);
            Assert.AreEqual("The command did not produce any results to standard output.", exception.Message);
        }

        [Test]
        public async Task WrathmarkRunsEndToEnd(
            [Values(PlatformID.Win32NT, PlatformID.Unix)] PlatformID platformId,
            [Values(Architecture.X64, Architecture.Arm64)] Architecture architecture)
        {
            // Arrange
            MockFixture mockFixture = SetupDefaultMockBehaviors(platformId, architecture);
            List<Tuple<string, string, string>> processesCreated = new List<Tuple<string, string, string>>();

            mockFixture.ProcessManager.OnCreateProcess = ((exe, args, workingDirectory) =>
            {
                processesCreated.Add(new Tuple<string, string, string>(exe, args, workingDirectory));

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                // From C:\users\any\tools\VirtualClient\packages\wrathmark\wrath-sharp-std-arrays\bin\Release\net8.0\win-x64\publish\wrath-sharp-std-arrays.exe
                // we only want wrath-sharp-std-arrays.exe
                var processImage = exe.Split(new[] { '/', '\\' }, StringSplitOptions.TrimEntries).Last();

                switch (processImage)
                {
                    case "dotnet.exe":
                    case "dotnet":
                    case "sudo":
                        // Does not matter what the output is, but it cannot be empty
                        process.StandardOutput.Append("NOT EMPTY");
                        break;
                    case "wrath-sharp-std-arrays.exe":
                    case "wrath-sharp-std-arrays":
                        process.StandardOutput.Append(File.ReadAllText(GetExampleFileForTests(Constants.BenchmarkResults)));
                        break;
                    default:
                        throw new InvalidOperationException($"Unrecognized process '{processImage}'.");
                }

                return process;
            });
            WrathmarkWorkloadExecutor sut = WorkloadExecutorFactory(mockFixture);

            // Act
            await sut.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.AreEqual(processesCreated.Count, 2, "Expected two processes to run: one to publish, one to run the compiled benchmark");

            // Lots of fighting against the system here to get the expected paths
            string dotNetPackagePath = await GetDependencyPath(mockFixture.PackageManager.Object, Constants.Profile.Dependencies.DotNetSdkPackageName, platformId, architecture);
            string wrathMarkPackagePath = await GetDependencyPath(mockFixture.PackageManager.Object, Constants.Profile.Dependencies.WrathmarkPackageName, platformId, architecture );
            string platformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(platformId, architecture);
            string expectedDotNetPackageCommand = null;

            switch (platformId)
            {
                case PlatformID.Win32NT:
                    expectedDotNetPackageCommand = mockFixture.PlatformSpecifics.Combine(dotNetPackagePath, "dotnet.exe");
                    break;
                case PlatformID.Unix:
                    // Must run elevated to avoid error MSB4018: The "CreateAppHost" task failed unexpectedly
                    expectedDotNetPackageCommand = "sudo " + mockFixture.PlatformSpecifics.Combine(dotNetPackagePath, "dotnet");
                    break;
            }

            expectedDotNetPackageCommand += " publish ";

            // Build up Wrathmark project path
            string wrathMarkCsProj = mockFixture.PlatformSpecifics.Combine(wrathMarkPackagePath, sut.Subfolder, sut.Subfolder + ".csproj");

            expectedDotNetPackageCommand += wrathMarkCsProj;
            expectedDotNetPackageCommand += $" -c Release -r {platformArchitecture} -f {sut.TargetFramework} /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true";

            Assert.AreEqual(expectedDotNetPackageCommand,  $"{processesCreated[0].Item1} {processesCreated[0].Item2}");

            // Assert the Wrathmark process is correct
            string expectedWrathmarkCommand = mockFixture.PlatformSpecifics.Combine(
                wrathMarkPackagePath,
                Constants.Profile.Parameters.Subfolder,
                "bin",
                "Release",
                Constants.Profile.Parameters.TargetFramework,
                platformArchitecture,
                "publish",
                platformId == PlatformID.Unix ? Constants.Profile.Parameters.Subfolder : Constants.Profile.Parameters.Subfolder + ".exe");
            expectedWrathmarkCommand += " " + Constants.Profile.Parameters.WrathmarkArgs;

            Assert.AreEqual(expectedWrathmarkCommand, $"{processesCreated[1].Item1} {processesCreated[1].Item2}");
        }

        private async Task<string> GetDependencyPath(IPackageManager packageManager, string packageName, PlatformID platformId, Architecture architecture)
        {
            DependencyPath package = await packageManager.GetPlatformSpecificPackageAsync(packageName, platformId, architecture, CancellationToken.None);

            // There is clean up that's done, but that's not how
            string path = package.Path;
            string platformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(platformId, architecture);

            switch (platformId)
            {
                case PlatformID.Win32NT:
                    path = path.Replace("\\" + platformArchitecture, String.Empty);
                    break;
                case PlatformID.Unix:
                    path = path.Replace("/" + platformArchitecture, String.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(platformId),
                        platformId,
                        $@"Unsupported {nameof(PlatformID)} value.");
            }

            return path;
        }

        private MockFixture SetupDefaultMockBehaviors(PlatformID platform, Architecture architecture)
        {
            MockFixture retVal = new MockFixture();
            retVal.Setup(platform, architecture);

            RegisterByNamePackage(Constants.Profile.Dependencies.WrathmarkPackageName);
            RegisterPackageByNameAndPath(Constants.Profile.Dependencies.DotNetSdkPackageName, "dotnet");

            retVal.File.Reset();
            retVal.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            retVal.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            retVal.FileSystem.SetupGet(fs => fs.File).Returns(retVal.File.Object);

            retVal.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(WrathmarkWorkloadExecutor.PackageName), Constants.Profile.Parameters.PackageName },
                { nameof(WrathmarkWorkloadExecutor.DotNetSdkPackageName), Constants.Profile.Parameters.DotNetSdkPackageName },
                { nameof(WrathmarkWorkloadExecutor.TargetFramework), Constants.Profile.Parameters.TargetFramework },
                { nameof(WrathmarkWorkloadExecutor.Subfolder), Constants.Profile.Parameters.Subfolder },
                { nameof(WrathmarkWorkloadExecutor.WrathmarkArgs), Constants.Profile.Parameters.WrathmarkArgs},
                { nameof(WrathmarkWorkloadExecutor.Scenario), Constants.Profile.Actions.Parameters.Scenario}
            };

            return retVal;


            void RegisterByNamePackage(string name) => RegisterPackageByNameAndPath(name, name);
            void RegisterPackageByNameAndPath(string name, params string[] additionalPathSegments)
            {
                DependencyPath mockPackage = new DependencyPath(name, retVal.PlatformSpecifics.GetPackagePath(additionalPathSegments));
                retVal.PackageManager.OnGetPackage(mockPackage.Name).ReturnsAsync(mockPackage);
            }
        }
    }
}
