// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using static CRC.VirtualClient.Actions.ParallelDownloadHandler;

    /// <summary>
    /// The Elasticsearch Rally Server workload executor.
    /// </summary>
    public class ElasticsearchRallyServerExecutor : ElasticsearchRallyBaseExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElasticsearchRallyServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public ElasticsearchRallyServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// This flag enables the use of Wget specifically on Linux systems instead of the 
        /// default method which is Apt installation.
        /// </summary>
        /// <remarks> Wget is taking too long downloading in VMs deployed by Juno.
        /// Use this flag to switch to Wget if Apt installation is failing or you need a specific Elasticsearch version.
        /// </remarks>
        public bool UseWgetForElasticsearhDownloadOnLinux => this.Parameters.GetValue<bool>(nameof(ElasticsearchRallyServerExecutor.UseWgetForElasticsearhDownloadOnLinux), false);

        /// <summary>
        /// Gets a value indicating whether the download of Elasticsearch on Windows uses the WebRequest API instead of the .Net HttpClient
        /// the default method.
        /// </summary>
        /// <remarks>Use this property to control the download mechanism for Elasticsearch on Windows
        /// platforms. This may be necessary for compatibility with certain network environments or when the default
        /// method is unavailable.</remarks>
        public bool UseWebRequestForElasticsearchDownloadOnWindows => this.Parameters.GetValue<bool>(nameof(ElasticsearchRallyServerExecutor.UseWebRequestForElasticsearchDownloadOnWindows), false);

        /// <summary>
        /// The timeout duration (in milliseconds) to wait for Elasticsearch to become available after starting.
        /// </summary>
        protected int WaitForElasticsearchAvailabilityTimeout { get; set; } = 30000;

        /// <summary>
        /// Initializes the environment for execution of the Rally workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.Logger.LogMessageAsync($"{this.TypeName}.ConfigureServer", telemetryContext.Clone(), async () =>
            {
                ElasticsearchRallyState state = await this.StateManager.GetStateAsync<ElasticsearchRallyState>(nameof(ElasticsearchRallyState), cancellationToken)
                    ?? new ElasticsearchRallyState();

                if (!state.ElasticsearchStarted)
                {
                    await this.StartElasticsearch(telemetryContext, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        state.ElasticsearchStarted = true;
                        await this.StateManager.SaveStateAsync<ElasticsearchRallyState>(nameof(ElasticsearchRallyState), state, cancellationToken);
                    }
                }
            });
        }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(ElasticsearchRallyServerExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                try
                {
                    this.SetServerOnline(true);

                    if (this.IsMultiRoleLayout())
                    {
                        using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                        {
                            await this.WaitAsync(cancellationToken);
                        }
                    }
                }
                finally
                {
                    this.SetServerOnline(false);
                }
            });
        }

        /// <summary>
        /// Write all text to a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="content">The content to write to the file.</param>
        protected virtual void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Read all text from a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The content of the file.</returns>
        protected virtual string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Downloads a file in parallel.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="destinationPath">The destination path where the file will be saved.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        protected virtual async Task ParallelDownloadFile(
            string url,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            await ParallelDownloadHandler.DownloadFile(url, destinationPath, cancellationToken);

            return;
        }

        private async Task StartElasticsearch(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                this.StartElasticsearchLinux(telemetryContext);
            }
            else
            {
                await this.StartElasticsearchWin(telemetryContext, cancellationToken);
            }
        }

        private void StartElasticsearchLinux(EventContext telemetryContext)
        {
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath(this.PackageName.ToLower());
            int port = this.Port;

            telemetryContext.AddContext(nameof(scriptsDirectory), scriptsDirectory);
            telemetryContext.AddContext(nameof(port), port);

            this.RunCommandAsRoot(telemetryContext, "SetVmMaxMapCount", "sysctl -w vm.max_map_count=262144");

            // make the change persistent
            this.RunCommandScript(telemetryContext, "VmMaxMapCountPersist", "echo \"vm.max_map_count=262144\" | sudo tee /etc/sysctl.d/99-elasticsearch.conf");
            this.RunCommandAsRoot(telemetryContext, "VmMaxMapCountSysCtl", "sysctl --system");
            this.RunCommandAsRoot(telemetryContext, "VmMaxMapCountVerify", "sysctl vm.max_map_count");

            // LimitMEMLOCKinfinity
            this.RunCommandScript(telemetryContext, "LimitMEMLOCKinfinityMkdir", "sudo mkdir -p /etc/systemd/system/elasticsearch.service.d");
            this.RunCommandScript(telemetryContext, "LimitMEMLOCKinfinityPersist", "printf \"[Service]\nLimitMEMLOCK=infinity\nLimitMEMLOCKSoft=infinity\n\" | sudo tee /etc/systemd/system/elasticsearch.service.d/override.conf");

            // download and install elasticsearch
            this.InstallElasticsearchLinux(telemetryContext, scriptsDirectory);

            // create elasticsearch.yml
            string elasticsearchPathYml = this.CreateElasticsearchYml(telemetryContext, scriptsDirectory, port);

            this.RunCommandAsRoot(telemetryContext, "ElasticsearchYmlCopy", $"cp {elasticsearchPathYml} /etc/elasticsearch/elasticsearch.yml");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchYml", $"tail -n 10000 /etc/elasticsearch/elasticsearch.yml");

            // set limits.conf
            string limitsConfPath = this.PlatformSpecifics.Combine(scriptsDirectory, "limits.ini");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchLimitsCopy", $"cp {limitsConfPath} /etc/security/limits.conf");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchLimits", $"tail -n 10000 /etc/security/limits.conf");

            this.RunCommandAsRoot(telemetryContext, "ElasticsearchRemoveKeystore", "/usr/share/elasticsearch/bin/elasticsearch-keystore remove xpack.security.transport.ssl.keystore.secure_password");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchRemoveTruestore", "/usr/share/elasticsearch/bin/elasticsearch-keystore remove xpack.security.transport.ssl.truststore.secure_password");

            // run elasticsearch
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchDaemonReexec", "systemctl daemon-reexec");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchEnable", "systemctl enable elasticsearch");
            bool ok = this.RunCommandAsRoot(telemetryContext, "ElasticsearchStart", "systemctl start elasticsearch.service");
            Thread.Sleep(30000); // wait for elasticsearch to start

            if (!ok)
            {
                this.RunCommandAsRoot(telemetryContext, "ElasticsearchRallyClusterLog", $"tail -n 10000 /var/log/elasticsearch/rally-cluster.log");

                this.RunCommandAsRoot(telemetryContext, "ElasticsearchJournal", "journalctl -xeu elasticsearch.service");

                throw new WorkloadException(
                    $"Elasticsearch failed to start.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            // verify elasticsearch is running
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchStatus", "systemctl status elasticsearch.service");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchSocket", "ss -lnt");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchUrlCall", $"curl localhost:{port}");
        }

        private void InstallElasticsearchLinux(EventContext telemetryContext, string scriptsDirectory)
        {
            // VirtualClient profile script using LinuxPackageInstallation is throwing a reachability error from Juno deployment: Unable to locate package elasticsearch
            // manual installation is not working via wget in VMs deployed by Juno

            if (this.UseWgetForElasticsearhDownloadOnLinux)
            {
                this.InstallElasticsearchLinuxUsingWget(telemetryContext, scriptsDirectory);
            }
            else
            {
                this.InstallElasticsearchLinuxUsingApt(telemetryContext);
            }
        }

        private void InstallElasticsearchLinuxUsingApt(EventContext telemetryContext)
        {
            string elasticsearchMajorVersionKey = $"{this.ElasticsearchVersion.Substring(0, 1)}.x";
            telemetryContext.AddContext(nameof(elasticsearchMajorVersionKey), elasticsearchMajorVersionKey);

            // Elasticsearch documentation here https://www.elastic.co/guide/en/elasticsearch/reference/current/deb.html
            this.RunCommandScript(telemetryContext, "ElasticsearchImport", "curl -fsSL https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /usr/share/keyrings/elasticsearch-keyring.gpg");
            this.RunCommandScript(telemetryContext, "ElasticsearchAdd", $"echo \"deb [signed-by=/usr/share/keyrings/elasticsearch-keyring.gpg] https://artifacts.elastic.co/packages/{elasticsearchMajorVersionKey}/apt stable main\" | sudo tee /etc/apt/sources.list.d/elastic-{elasticsearchMajorVersionKey}.list");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchUpdate", "apt update");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchInstall", "apt install elasticsearch -y");
        }

        private void InstallElasticsearchLinuxUsingWget(EventContext telemetryContext, string scriptsDirectory)
        {
            // manual installation is not working via wget in VMs deployed by Juno

            string elasticsearchVersion = this.ElasticsearchVersion;
            string platformArchitecture = this.PlatformArchitectureName;
            string architecture = platformArchitecture.EndsWith("arm64") ? "arm64" : "amd64";
            string distroVersion = $"{elasticsearchVersion}-{architecture}";
            string downloadCommand = $"wget --no-check-certificate -t=10 --connect-timeout=30 --dns-timeout=30 -P {scriptsDirectory} https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{distroVersion}.deb";

            telemetryContext.AddContext(nameof(elasticsearchVersion), elasticsearchVersion);
            telemetryContext.AddContext(nameof(platformArchitecture), platformArchitecture);
            telemetryContext.AddContext(nameof(distroVersion), distroVersion);
            telemetryContext.AddContext(nameof(downloadCommand), downloadCommand);

            this.Logger.LogMessage($"{this.TypeName}.ElasticsearchDownloadStart", telemetryContext);

            // I followed Elasticsearch documentation here https://www.elastic.co/docs/deploy-manage/deploy/self-managed/install-elasticsearch-with-debian-package
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchDownload", downloadCommand, true);
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchDownloadSHA", $"{downloadCommand}.sha512", true);
            this.RunCommandScript(telemetryContext, "ElasticsearchCheckSHA", $"cd {scriptsDirectory} && shasum -a 512 -c elasticsearch-{distroVersion}.sha512", true);
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchInstall", $"dpkg -i {scriptsDirectory}/elasticsearch-{distroVersion}", true);
        }

        private string CreateElasticsearchYml(EventContext telemetryContext, string scriptsDirectory, int port)
        {
            string elasticsearchPath = this.PlatformSpecifics.Combine(scriptsDirectory, "elasticsearch.ini");
            if (!this.CheckFileExists(elasticsearchPath))
            {
                throw new WorkloadException(
                    $"The Elasticsearch configuration file (yml) could not be found at the expected path: {elasticsearchPath}",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            telemetryContext.AddContext($"{this.TypeName}.{nameof(elasticsearchPath)}", elasticsearchPath);
            this.Logger.LogMessage($"{this.TypeName}.ElasticsearchIniRead", telemetryContext);
            string elasticsearchYmlContent = this.ReadAllText(elasticsearchPath);
            elasticsearchYmlContent = elasticsearchYmlContent.Replace("$.parameters.port", port.ToString());

            string elasticsearchPathYml = elasticsearchPath.Replace(".ini", ".yml");
            this.Logger.LogMessage($"{this.TypeName}.ElasticsearchYmlWrite", telemetryContext);
            this.WriteAllText(elasticsearchPathYml, elasticsearchYmlContent);

            return elasticsearchPathYml;
        }

        private async Task StartElasticsearchWin(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int port = this.Port;
            string elasticsearchVersion = this.ElasticsearchVersion;
            telemetryContext.AddContext(nameof(elasticsearchVersion), elasticsearchVersion);
            telemetryContext.AddContext(nameof(port), port);

            // create install directory
            string installDir = await this.GetDataDirectoryAsync(cancellationToken);

            if (!this.CheckDirectoryExists(installDir))
            {
                throw new WorkloadException(
                    $"The Elasticsearch data directory could not be found at the expected path: {installDir}",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            installDir = System.IO.Path.Combine(installDir, "elasticsearch");
            Directory.CreateDirectory(installDir);

            // download elasticsearch
            string elasticsearchBase = $"elasticsearch-{elasticsearchVersion}";
            string platformArchitecture = this.PlatformArchitectureName;
            string architecture = platformArchitecture.EndsWith("arm64") ? "arm_64" : "x86_64";
            string distroVersion = $"{elasticsearchBase}-windows-{architecture}.zip";
            telemetryContext.AddContext(nameof(elasticsearchVersion), elasticsearchVersion);
            telemetryContext.AddContext(nameof(platformArchitecture), platformArchitecture);
            telemetryContext.AddContext(nameof(distroVersion), distroVersion);

            string downloadUrl = $"https://artifacts.elastic.co/downloads/elasticsearch/{distroVersion}";
            string zipPath = System.IO.Path.Combine(installDir, $"{elasticsearchBase}.zip");

            await this.DownloadFileAsync(telemetryContext, downloadUrl, zipPath, cancellationToken);

            // extract
            string psExtract = $@"
if (-not (Test-Path '{installDir}')) {{ New-Item -ItemType Directory -Path '{installDir}' | Out-Null; }}
Expand-Archive -Path '{zipPath}' -DestinationPath '{installDir}' -Force;
";
            this.RunCommandWindowsScript(telemetryContext, "ElasticsearchExtract", psExtract);

            string dirContents = string.Join(';', Directory.GetDirectories(installDir).Select(x => x.Split('\\').Last()));
            telemetryContext.AddContext($"{this.TypeName}.{nameof(dirContents)}", dirContents);

            string homeDir = System.IO.Path.Combine(installDir, elasticsearchBase);
            telemetryContext.AddContext($"{this.TypeName}.{nameof(homeDir)}", homeDir);

            // create elasticsearch.yml
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath(this.PackageName.ToLower());
            string elasticsearchPathYml = this.CreateElasticsearchYml(telemetryContext, scriptsDirectory, port);

            this.FileCopy(elasticsearchPathYml, Path.Combine(homeDir, "config", "elasticsearch.yml"), true);

            // open firewall
            // string psFirewall = $"New-NetFirewallRule -DisplayName ElasticsearchInbound -Direction Inbound -Protocol TCP -LocalPort {port} -Action Allow -Profile Any";
            string psFirewall = "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False";
            this.RunCommandWindowsScript(telemetryContext, "ElasticsearchOpenFirewall", psFirewall);

            // start detached
            string args = $"-E http.port={port}";
            string elasticBat = Path.Combine(homeDir, "bin", "elasticsearch.bat");
            string startDetached = $"/c start {elasticBat} {args}";

            this.RunCommandWindowsScriptDetached(telemetryContext, "ElasticsearchStart", startDetached);

            await Task.Delay(this.WaitForElasticsearchAvailabilityTimeout); // wait for elasticsearch to start

            // verify elasticsearch is running

            string psPing = @"
try {
    $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:" + port + @"' -TimeoutSec 15
    Write-Output ($r.StatusCode)
} catch {
    Write-Output ('ERROR: ' + $_.Exception.Message)
}
";
            this.RunCommandWindowsScript(telemetryContext, "ElasticsearchPing", psPing);
        }

        private async Task DownloadFileAsync(EventContext telemetryContext, string downloadUrl, string zipPath, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext(nameof(downloadUrl), downloadUrl);
            telemetryContext.AddContext(nameof(zipPath), zipPath);
            await this.Logger.LogMessageAsync($"{this.TypeName}.DownloadFile", telemetryContext.Clone(), async () =>
            {
                // WebRequest is taking too long to download the file, using parallel download handler as default.
                if (this.UseWebRequestForElasticsearchDownloadOnWindows)
                {
                    this.RunCommandWindowsScript(telemetryContext, "ElasticsearchDownload", $"Invoke-WebRequest -Uri '{downloadUrl}' -OutFile '{zipPath}' -UseBasicParsing");
                }
                else
                {
                    // .Net HttpClient based parallel download
                    await this.ParallelDownloadFile(downloadUrl, zipPath, cancellationToken);
                }
            });

            return;
        }
    }
}
