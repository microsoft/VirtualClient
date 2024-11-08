// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Metadata;

    /// <summary>
    /// Provides functionality for installing compilers on the system (GCC, AOCC, etc).
    /// </summary>
    public class CompilerInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private int[] successCodes = { 0, 2 };

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public CompilerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The name of the compiler (e.g. gcc).
        /// </summary>
        public string CompilerName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(CompilerInstallation.CompilerName), "gcc");
            }

            set
            {
                this.Parameters[nameof(CompilerInstallation.CompilerName)] = value;
            }
        }

        /// <summary>
        /// The version of the compiler (e.g. 10).
        /// </summary>
        public string CompilerVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(CompilerInstallation.CompilerVersion), string.Empty);
            }

            set
            {
                this.Parameters[nameof(CompilerInstallation.CompilerVersion)] = value;
            }
        }

        /// <summary>
        /// List of pacakges separated by comma that needs to be installed with cygwin (e.g. make,gcc-fortran,python3).
        /// </summary>
        public string CygwinPackages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(CompilerInstallation.CygwinPackages), string.Empty);
            }

            set
            {
                this.Parameters[nameof(CompilerInstallation.CygwinPackages)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes the compiler installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string compiler = this.CompilerName.ToLowerInvariant();
            switch (this.CompilerName.ToLowerInvariant())
            {
                case "gcc":
                    if (this.Platform == PlatformID.Unix)
                    {
                        await this.InstallGccAsync(this.CompilerVersion, telemetryContext, cancellationToken);

                        if (!await this.ConfirmGccVersionInstalledAsync(cancellationToken))
                        {
                            throw new DependencyException($"'{this.CompilerName.ToLowerInvariant()}' compiler version '{this.CompilerVersion}' not confirmed.", ErrorReason.DependencyInstallationFailed);
                        }
                    }
                    else if (this.Platform == PlatformID.Win32NT)
                    {
                        string chocolateyToolsLocation = this.GetEnvironmentVariable("ChocolateyToolsLocation", EnvironmentVariableTarget.User);
                        string cygwinInstallationPath = this.PlatformSpecifics.Combine(chocolateyToolsLocation, "cygwin");

                        DependencyPath cygwinPackage = new DependencyPath("cygwin", cygwinInstallationPath);
                        await this.systemManager.PackageManager.RegisterPackageAsync(cygwinPackage, cancellationToken);

                        await this.InstallCygwinAsync(cygwinPackage, telemetryContext, cancellationToken);
                    }

                    break;

                case "aocc":
                    if (this.Platform == PlatformID.Unix)
                    {
                        await this.InstallAoccAsync(this.CompilerVersion, telemetryContext, cancellationToken);
                    }

                    break;

                case Compilers.Charmplusplus:
                    if (this.Platform == PlatformID.Unix)
                    {
                        await this.InstallCharmplusplusAsync(this.CompilerVersion, telemetryContext, cancellationToken);
                    }

                    break;

                default:
                    throw new NotSupportedException($"Compiler '{this.CompilerName}' is not supported.");
            }

            // The compiler + version installed is an important part of the metadata
            // contract for VC scenarios.
            IDictionary<string, object> compilerMetadata = await this.systemManager.GetInstalledCompilerMetadataAsync(this.Logger, cancellationToken);
            MetadataContract.Persist(compilerMetadata, MetadataContractCategory.Dependencies, true);
        }

        /// <summary>
        /// Waits for the expected version of the compiler to be confirmed installed on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task<bool> ConfirmGccVersionInstalledAsync(CancellationToken cancellationToken)
        {
            List<string> compilersToCheck = new List<string>() { Compilers.Gcc, Compilers.Cc };
            int confirmedCompilers = 0;
            foreach (string compiler in compilersToCheck)
            {
                Regex versionConfirmationExpression = new Regex($@"{compiler}\s*\([\x20-\x7F]+\)\s*{this.CompilerVersion}", RegexOptions.Multiline);
                using (IProcessProxy process = this.systemManager.ProcessManager.CreateProcess(compiler, "--version"))
                {
                    this.Logger.LogTraceMessage($"Confirming expected compiler version installed...");

                    await process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, EventContext.Persisted(), "CompilerVersion");

                        ConsoleLogger.Default.LogTraceMessage(compiler + "2" + process.StandardOutput.ToString());

                        if (versionConfirmationExpression.IsMatch(process.StandardOutput.ToString()))
                        {
                            confirmedCompilers++;
                            this.Logger.LogTraceMessage($"Compiler {compiler} confirmed for version {this.CompilerVersion}.");
                        }
                    }
                }
            }

            return (confirmedCompilers == compilersToCheck.Count);
        }

        private Task InstallCygwinAsync(DependencyPath cygwinInstallationPath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string cygwinCommandArguments;
            string cygwinInstallerPath = this.Combine(cygwinInstallationPath.Path, "cygwinsetup.exe");

            if (!string.IsNullOrEmpty(this.CygwinPackages))
            {
                cygwinCommandArguments = @$"--quiet-mode --root {cygwinInstallationPath.Path} --site http://cygwin.mirror.constant.com --packages make,cmake,{this.CygwinPackages}";
            }
            else
            {
                cygwinCommandArguments = @$"--quiet-mode --root {cygwinInstallationPath.Path} --site http://cygwin.mirror.constant.com --packages make,cmake";
            }

            return this.ExecuteCommandAsync(cygwinInstallerPath, cygwinCommandArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken);
        }

        private async Task InstallGccAsync(string gccVersion, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            LinuxDistributionInfo distro = await this.systemManager.GetLinuxDistributionAsync(cancellationToken);
            switch (distro.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                case LinuxDistribution.Debian:
                    // default to 10
                    await this.RemoveAlternativesAsync(telemetryContext, cancellationToken);
                    gccVersion = (string.IsNullOrEmpty(gccVersion)) ? "10" : gccVersion;
                    await this.ExecuteCommandAsync("add-apt-repository", $"ppa:ubuntu-toolchain-r/test -y", Environment.CurrentDirectory, telemetryContext, cancellationToken);
                    await this.ExecuteCommandAsync("apt", $"update", Environment.CurrentDirectory, telemetryContext, cancellationToken);
                    await this.ExecuteCommandAsync("apt", @$"install build-essential gcc-{gccVersion} g++-{gccVersion} gfortran-{gccVersion} -y --quiet", Environment.CurrentDirectory, telemetryContext, cancellationToken);
                    await this.SetGccPriorityAsync(gccVersion, telemetryContext, cancellationToken);

                    break;

                case LinuxDistribution.CentOS8:
                case LinuxDistribution.RHEL8:
                case LinuxDistribution.AzLinux:
                    await this.RemoveAlternativesAsync(telemetryContext, cancellationToken);
                    await this.ExecuteCommandAsync("dnf", @$"install make gcc-toolset-{gccVersion} gcc-toolset-{gccVersion}-gcc-gfortran -y --quiet", Environment.CurrentDirectory, telemetryContext, cancellationToken);
                    await this.SetGccPriorityAsync(gccVersion, telemetryContext, cancellationToken);

                    break;

                default:
                    throw new PlatformNotSupportedException($"This Linux distribution '{distro}' is not supported for this profile.");
            }
        }

        private async Task RemoveAlternativesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string[] packages =
            {
                "gcc",
                "gfortran"
            };

            // due to the following error:
            //      update-alternatives: error: alternative g++ can't be slave of gcc: it is a master alternative
            // must remove alternatives from the VM to avoid errors, then set all of them together

            foreach (string package in packages)
            {
                try
                {
                    await this.ExecuteCommandAsync("update-alternatives", $"--remove-all {package}", Environment.CurrentDirectory, telemetryContext, cancellationToken, this.successCodes);
                }
                catch
                {
                    // the message is:
                    //      "error: no alternatives for g++"
                    // so we can continue as normal; non-breaking
                }
            }
        }
 
        private async Task SetGccPriorityAsync(string gccVersion, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string updateAlternativeArgument = $"--install /usr/bin/gcc gcc /usr/bin/gcc-{gccVersion} {gccVersion}0 " +
                        $"--slave /usr/bin/g++ g++ /usr/bin/g++-{gccVersion} " +
                        $"--slave /usr/bin/gcov gcov /usr/bin/gcov-{gccVersion} " +
                        $"--slave /usr/bin/gcc-ar gcc-ar /usr/bin/gcc-ar-{gccVersion} " +
                        $"--slave /usr/bin/gcc-ranlib gcc-ranlib /usr/bin/gcc-ranlib-{gccVersion} " +
                        $"--slave /usr/bin/gfortran gfortran /usr/bin/gfortran-{gccVersion}";

            await this.ExecuteCommandAsync("update-alternatives", updateAlternativeArgument, Environment.CurrentDirectory, telemetryContext, cancellationToken);

            // Remove all existing alternatives for cpp before the subsequent "update-alternatives" of cpp
            await this.ExecuteCommandAsync("update-alternatives", "--remove-all cpp", Environment.CurrentDirectory, telemetryContext, cancellationToken);

            // For some update path, the cpp can't be update-alternative by a gcc, so needs a separate call.
            string updateAlternativeArgumentCpp = $"--install /usr/bin/cpp cpp /usr/bin/cpp-{gccVersion} {gccVersion}0";

            await this.ExecuteCommandAsync("update-alternatives", updateAlternativeArgumentCpp, Environment.CurrentDirectory, telemetryContext, cancellationToken);
        }

        private async Task InstallAoccAsync(string aoccVersion, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // default to 3.2.0
            aoccVersion = (string.IsNullOrEmpty(aoccVersion)) ? "3.2.0" : aoccVersion;
            await this.ExecuteCommandAsync("wget", $"https://developer.amd.com/wordpress/media/files/aocc-compiler-{aoccVersion}.tar", Environment.CurrentDirectory, telemetryContext, cancellationToken);
            await this.ExecuteCommandAsync("tar", $"-xvf aocc-compiler-{aoccVersion}.tar", Environment.CurrentDirectory, telemetryContext, cancellationToken);
            await this.ExecuteCommandAsync("bash", @$"install.sh", Environment.CurrentDirectory, telemetryContext, cancellationToken);
        }

        private async Task InstallCharmplusplusAsync(string charmplusplusVersion, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // default latest
            charmplusplusVersion = (string.IsNullOrEmpty(charmplusplusVersion)) ? "latest" : charmplusplusVersion;

            await this.ExecuteCommandAsync("wget", $"https://charm.cs.illinois.edu/distrib/charm-{charmplusplusVersion}.tar.gz -O charm.tar.gz", Environment.CurrentDirectory, telemetryContext, cancellationToken);
            await this.ExecuteCommandAsync("tar", $"-xzf charm.tar.gz", Environment.CurrentDirectory, telemetryContext, cancellationToken);
            string charmPath = Directory.GetDirectories(Environment.CurrentDirectory, "charm-v*").FirstOrDefault();

            if (this.CpuArchitecture == System.Runtime.InteropServices.Architecture.X64)
            {
                await this.ExecuteCommandAsync("./build", "charm++ netlrts-linux-x86_64 --with-production -j4", workingDirectory: charmPath, telemetryContext, cancellationToken);
            }

            if (this.CpuArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
            {
                await this.ExecuteCommandAsync("./build", "charm++ netlrts-linux-arm8 --with-production -j4", workingDirectory: charmPath, telemetryContext, cancellationToken);
            }
        }

        private Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken, int[] successCodes = null)
        {
            return this.RetryPolicy.ExecuteAsync(async () =>
             {
                 string output = string.Empty;
                 using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                 {
                     this.CleanupTasks.Add(() => process.SafeKill());
                     this.LogProcessTrace(process);

                     await process.StartAndWaitAsync(cancellationToken);

                     if (!cancellationToken.IsCancellationRequested)
                     {
                         await this.LogProcessDetailsAsync(process, telemetryContext);

                         process.ThrowIfErrored<DependencyException>(successCodes: this.successCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                     }
                 }
             });
        }
    }

    /// <summary>
    /// Compiler constants
    /// </summary>
    public class Compilers
    {
        /// <summary>
        /// Gcc compiler.
        /// </summary>
        public const string Gcc = "gcc";

        /// <summary>
        /// Cc compiler.
        /// </summary>
        public const string Cc = "cc";

        /// <summary>
        /// C++ compiler.
        /// </summary>
        public const string Cplusplus = "g++";

        /// <summary>
        /// Gfortran compiler.
        /// </summary>
        public const string Gfortran = "gfortran";

        /// <summary>
        /// Aocc.
        /// </summary>
        public const string Aocc = "aocc";

        /// <summary>
        /// Charm++ compiler.
        /// </summary>
        public const string Charmplusplus = "charm++";
    }
}