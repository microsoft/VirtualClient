// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Newtonsoft.Json;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command converts the profile(s) specified from 1 format to another
    /// (e.g. JSON to YAML, YAML to JSON).
    /// </summary>
    internal class ConvertProfileCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// Serializer settings to use when serializing/deserializing profiles into readable
        /// JSON files
        /// </summary>
        private static JsonSerializerSettings JsonSerializationSettings { get; } = new JsonSerializerSettings
        {
            // Format: 2012-03-21T05:40:12.340Z
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,

            // We tried using PreserveReferenceHandling.All and Object, but ran into issues
            // when deserializing string arrays and read only dictionaries
            ReferenceLoopHandling = ReferenceLoopHandling.Error,

            // This is the default setting, but to avoid remote code execution bugs do NOT change
            // this to any other setting.
            TypeNameHandling = TypeNameHandling.None,

            // By default, serialize enum values to their string representation.
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        /// <summary>
        /// The directory to which the converted profiles should be written.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Executes the operations to reset the environment.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IServiceCollection dependencies = this.InitializeDependencies(args);
            IEnumerable<string> profiles = await this.EvaluateProfilesAsync(dependencies);

            if (profiles?.Any() == true)
            {
                foreach (string filePath in profiles)
                {
                    string profileName = Path.GetFileName(filePath);
                    ExecutionProfile profile = await this.ReadExecutionProfileAsync(filePath, dependencies, cancellationToken);
                    if (profile.ProfileFormat == "JSON")
                    {
                        await this.SerializeToYamlAsync(dependencies, profileName, profile);
                    }
                    else if (profile.ProfileFormat == "YAML")
                    {
                        await this.SerializeToJsonAsync(dependencies, profileName, profile);
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args)
        {
            IServiceCollection dependencies = new ServiceCollection();
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

            dependencies.AddSingleton<PlatformSpecifics>(platformSpecifics);
            dependencies.AddSingleton<IFileSystem>(new FileSystem());
            dependencies.AddSingleton<ILogger>(NullLogger.Instance);

            return dependencies;
        }

        private async Task SerializeToJsonAsync(IServiceCollection dependencies, string profileName, ExecutionProfile profile)
        {
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();

            foreach (ExecutionProfileElement component in profile.Actions.Union(profile.Monitors).Union(profile.Dependencies))
            {
                ExecutionProfileYamlShim.StandardizeParameterReferences(component.Parameters, yamlToJson: true);
            }

            string profileJson = profile.ToJson(ConvertProfileCommand.JsonSerializationSettings);
            string profileFilePath = platformSpecifics.Combine(this.OutputPath, $"{Path.GetFileNameWithoutExtension(profileName)}.json");

            await fileSystem.File.WriteAllTextAsync(profileFilePath, profileJson);
        }

        private async Task SerializeToYamlAsync(IServiceCollection dependencies, string profileName, ExecutionProfile profile)
        {
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();

            foreach (ExecutionProfileElement component in profile.Actions.Union(profile.Monitors).Union(profile.Dependencies))
            {
                ExecutionProfileYamlShim.StandardizeParameterReferences(component.Parameters, jsonToYaml: true);
            }

            var yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder()
                .Build();

            ExecutionProfileYamlShim profileShim = new ExecutionProfileYamlShim(profile);

            string profileYaml = yamlSerializer.Serialize(profileShim);
            string profileFilePath = platformSpecifics.Combine(this.OutputPath, $"{Path.GetFileNameWithoutExtension(profileName)}.yml");

            await fileSystem.File.WriteAllTextAsync(profileFilePath, profileYaml);
        }
    }
}