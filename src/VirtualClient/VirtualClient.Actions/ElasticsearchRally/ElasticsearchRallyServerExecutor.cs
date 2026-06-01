// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Elasticsearch Rally Server workload executor.
    /// </summary>
    public class ElasticsearchRallyServerExecutor : ElasticsearchRallyExecutor
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

        private async Task StartElasticsearch(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string mountPoint = await this.GetDataDirectoryAsync(cancellationToken);

            if (this.Platform == PlatformID.Unix)
            {
                this.StartElasticsearchLinux(telemetryContext, cancellationToken, mountPoint);
            }
            else
            {
                throw new NotSupportedException($"The {this.TypeName} does not support execution on {this.Platform} operating system.");
            }
        }

        private void StartElasticsearchLinux(EventContext telemetryContext, CancellationToken cancellationToken, string mountPoint)
        {
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath(this.PackageName.ToLower());
            int port = this.Port;

            telemetryContext.AddContext(nameof(mountPoint), mountPoint);
            telemetryContext.AddContext(nameof(scriptsDirectory), scriptsDirectory);
            telemetryContext.AddContext(nameof(port), port);

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "SetVmMaxMapCount", "sysctl -w vm.max_map_count=262144");

            // make the change persistent
            this.RunCommandScript(telemetryContext, cancellationToken, "VmMaxMapCountPersist", "echo \"vm.max_map_count=262144\" | sudo tee /etc/sysctl.d/99-elasticsearch.conf");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "VmMaxMapCountSysCtl", "sysctl --system");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "VmMaxMapCountVerify", "sysctl vm.max_map_count");

            // LimitMEMLOCKinfinity
            this.RunCommandScript(telemetryContext, cancellationToken, "LimitMEMLOCKinfinityMkdir", "sudo mkdir -p /etc/systemd/system/elasticsearch.service.d");
            this.RunCommandScript(telemetryContext, cancellationToken, "LimitMEMLOCKinfinityPersist", "printf \"[Service]\nLimitMEMLOCK=infinity\nLimitMEMLOCKSoft=infinity\n\" | sudo tee /etc/systemd/system/elasticsearch.service.d/override.conf");

            // download and install elasticsearch
            this.InstallElasticsearchLinux(telemetryContext, cancellationToken, scriptsDirectory);

            (string rootPath, string dataPath, string logPath) = GetElasticsearchPathsLinux(mountPoint);

            if (!string.IsNullOrEmpty(mountPoint))
            {
                // create data and log directories
                this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchRootPathMkdir", $"mkdir -p {rootPath}");

                // check if mounted filesystem is read‑only
                this.RunCommandScript(telemetryContext, cancellationToken, "CheckMountedFileSystem", $"mount | grep {mountPoint}");

                // set ownership
                this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchRepoChown", $"chmod -R 777 {rootPath}", true);
            }

            // create elasticsearch.yml
            string elasticsearchPathYml = this.CreateElasticsearchYml(telemetryContext, scriptsDirectory, port, dataPath, logPath);

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchYmlCopy", $"cp {elasticsearchPathYml} /etc/elasticsearch/elasticsearch.yml");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchYml", $"tail -n 10000 /etc/elasticsearch/elasticsearch.yml");

            // set limits.conf
            string limitsConfPath = this.PlatformSpecifics.Combine(scriptsDirectory, "limits.ini");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchLimitsCopy", $"cp {limitsConfPath} /etc/security/limits.conf");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchLimits", $"tail -n 10000 /etc/security/limits.conf");

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchRemoveKeystore", "/usr/share/elasticsearch/bin/elasticsearch-keystore remove xpack.security.transport.ssl.keystore.secure_password");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchRemoveTruestore", "/usr/share/elasticsearch/bin/elasticsearch-keystore remove xpack.security.transport.ssl.truststore.secure_password");

            // run elasticsearch
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchDaemonReexec", "systemctl daemon-reexec");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchEnable", "systemctl enable elasticsearch");
            bool ok = this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchStart", "systemctl start elasticsearch.service");
            Thread.Sleep(30000); // wait for elasticsearch to start

            if (!ok)
            {
                this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchJournal", "journalctl -xeu elasticsearch.service");

                throw new WorkloadException(
                    $"Elasticsearch failed to start.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            // verify elasticsearch is running
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchStatus", "systemctl status elasticsearch.service");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchSocket", "ss -lnt");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchUrlCall", $"curl localhost:{port}");
        }

        private void InstallElasticsearchLinux(EventContext telemetryContext, CancellationToken cancellationToken, string scriptsDirectory)
        {
            // VirtualClient profile script using LinuxPackageInstallation is throwing a reachability error from Juno deployment: Unable to locate package elasticsearch
            // manual installation is not working via wget in VMs deployed by Juno, using Apt instead.

            string elasticsearchMajorVersionKey = $"{this.ElasticsearchVersion.Substring(0, 1)}.x";
            telemetryContext.AddContext(nameof(elasticsearchMajorVersionKey), elasticsearchMajorVersionKey);

            // Elasticsearch documentation here https://www.elastic.co/guide/en/elasticsearch/reference/current/deb.html
            this.RunCommandScript(telemetryContext, cancellationToken, "ElasticsearchImport", "curl -fsSL https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /usr/share/keyrings/elasticsearch-keyring.gpg");
            this.RunCommandScript(telemetryContext, cancellationToken, "ElasticsearchAdd", $"echo \"deb [signed-by=/usr/share/keyrings/elasticsearch-keyring.gpg] https://artifacts.elastic.co/packages/{elasticsearchMajorVersionKey}/apt stable main\" | sudo tee /etc/apt/sources.list.d/elastic-{elasticsearchMajorVersionKey}.list");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchUpdate", "apt update");
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "ElasticsearchInstall", "apt install elasticsearch -y");
        }

        private static (string root, string data, string log) GetElasticsearchPathsLinux(string mountPoint)
        {
            string rootPath;
            if (mountPoint == null)
            {
                rootPath = "/var/elasticsearch";
            }
            else
            {
                rootPath = Path.Combine(mountPoint, "elasticsearch");
            }

            return (rootPath, $"{rootPath}/data", $"{rootPath}/log");
        }

        private string CreateElasticsearchYml(EventContext telemetryContext, string scriptsDirectory, int port, string dataPath, string logPath)
        {
            string elasticsearchPath = this.PlatformSpecifics.Combine(scriptsDirectory, "elasticsearch.ini");
            if (!this.CheckFileExists(elasticsearchPath))
            {
                throw new WorkloadException(
                    $"The Elasticsearch configuration file (yml) could not be found at the expected path: {elasticsearchPath}",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            Dictionary<string, string> ymlparameters = new Dictionary<string, string>()
            {
                { "port", port.ToString() },
                { "path.data", dataPath },
                { "path.logs", logPath },
            };

            telemetryContext.AddContext($"{this.TypeName}.{nameof(elasticsearchPath)}", elasticsearchPath);
            this.Logger.LogMessage($"{this.TypeName}.ElasticsearchIniRead", telemetryContext);
            string elasticsearchYmlContent = this.ReadAllText(elasticsearchPath);

            foreach (KeyValuePair<string, string> p in ymlparameters)
            {
                telemetryContext.AddContext($"{this.TypeName}.YmlParameter.{p.Key}", p.Value);

                elasticsearchYmlContent = elasticsearchYmlContent.Replace($"$.parameters.{p.Key}", p.Value);
            }

            elasticsearchYmlContent = elasticsearchYmlContent.Replace("$.parameters.port", port.ToString());

            string elasticsearchPathYml = elasticsearchPath.Replace(".ini", ".yml");
            this.Logger.LogMessage($"{this.TypeName}.ElasticsearchYmlWrite", telemetryContext);
            this.WriteAllText(elasticsearchPathYml, elasticsearchYmlContent);

            return elasticsearchPathYml;
        }
    }
}
