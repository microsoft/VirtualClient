// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;

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
                    this.StartElasticSearch(telemetryContext);

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

        private void StartElasticSearch(EventContext telemetryContext)
        {
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath(this.PackageName.ToLower());
            int port = this.Port;

            this.RunCommandAsRoot(telemetryContext, "SetVmMaxMapCount", "sysctl -w vm.max_map_count=262144");

            // make the change persistent
            this.RunCommandScript(telemetryContext, "VmMaxMapCountPersist", "echo \"vm.max_map_count=262144\" | sudo tee /etc/sysctl.d/99-elasticsearch.conf");
            this.RunCommandAsRoot(telemetryContext, "VmMaxMapCountSysCtl", "sysctl --system");
            this.RunCommandAsRoot(telemetryContext, "VmMaxMapCountVerify", "sysctl vm.max_map_count");

            // LimitMEMLOCKinfinity
            this.RunCommandScript(telemetryContext, "LimitMEMLOCKinfinityMkdir", "sudo mkdir -p /etc/systemd/system/elasticsearch.service.d");
            this.RunCommandScript(telemetryContext, "LimitMEMLOCKinfinityPersist", "printf \"[Service]\nLimitMEMLOCK=infinity\nLimitMEMLOCKSoft=infinity\n\" | sudo tee /etc/systemd/system/elasticsearch.service.d/override.conf");

            // install elasticsearch
            // I tried VirtualClient profile script using LinuxPackageInstallation, but I got a reachability error from Juno deployment: Unable to locate package elasticsearch
            // So, I followed Elasticsearch documentation here https://www.elastic.co/guide/en/elasticsearch/reference/current/deb.html
            this.RunCommandScript(telemetryContext, "ElasticsearchImport", "curl -fsSL https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /usr/share/keyrings/elasticsearch-keyring.gpg");
            this.RunCommandScript(telemetryContext, "ElasticsearchAdd", "echo \"deb [signed-by=/usr/share/keyrings/elasticsearch-keyring.gpg] https://artifacts.elastic.co/packages/8.x/apt stable main\" | sudo tee /etc/apt/sources.list.d/elastic-8.x.list");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchUpdate", "apt update");
            this.RunCommandAsRoot(telemetryContext, "ElasticsearchInstall", "apt install elasticsearch -y");

            // create elasticsearch.yml
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
    }
}
