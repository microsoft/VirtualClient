// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A Virtual Client component that creates a response file (e.g. a <c>*.rsp</c> file) containing a space-delimited
    /// list of command-line options.
    /// </summary>
    /// <remarks>
    /// Virtual Client can automatically consume response files, allowing users to pass fewer arguments directly on the
    /// command line (and keep long/complex option sets in a file instead).
    /// <para/>
    /// Options are provided via parameters whose keys start with <c>Option</c> (case-insensitive), for example:
    /// <c>Option1=--System="Testing"</c>
    /// <c>Option2=--KeyVaultUri="https://crc-partner-vault.vault.azure.net"</c>
    /// <para/>
    /// The file is written to <see cref="Environment.CurrentDirectory"/> unless <see cref="FileName"/> is an absolute path.
    /// </remarks>
    public class CreateResponseFile : VirtualClientComponent
    {
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResponseFile"/> class.
        /// </summary>
        /// <param name="dependencies">Dependency injection container.</param>
        /// <param name="parameters">Component parameters.</param>
        public CreateResponseFile(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.fileSystem.ThrowIfNull(nameof(this.fileSystem));
        }

        /// <summary>
        /// Gets the name (or path) of the response file to create.
        /// Defaults to `resource_access.rsp`.
        /// </summary>
        public string FileName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.FileName), "resource_access.rsp");
            }
        }

        /// <summary>
        /// Creates the response file when one or more `Option*` parameters are supplied.
        /// </summary>
        /// <param name="telemetryContext">Context information provided to telemetry events.</param>
        /// <param name="cancellationToken">Token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Preserve relative-path behavior, but allow callers to pass an absolute path too.
            string filePath = Path.IsPathRooted(this.FileName)
                ? this.FileName
                : this.Combine(Environment.CurrentDirectory, this.FileName);

            string[] optionValues = this.Parameters
                .Where(p => p.Key.StartsWith("Option", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value.ToString().Trim())
                .ToArray();

            telemetryContext.AddContext(nameof(filePath), filePath);
            telemetryContext.AddContext(nameof(optionValues), optionValues);

            if (optionValues.Length > 0)
            {
                if (this.fileSystem.File.Exists(filePath))
                {
                    this.fileSystem.File.Delete(filePath);
                }

                string content = string.Join(' ', optionValues);
                telemetryContext.AddContext(nameof(content), content);

                byte[] bytes = Encoding.UTF8.GetBytes(content);

                await using (FileSystemStream fileStream = this.fileSystem.FileStream.New(
                    filePath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite))
                {
                    await fileStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                    await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}