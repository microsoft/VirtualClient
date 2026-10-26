// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides PostgreSQL credential resolution for database workloads and dependencies.
    /// </summary>
    public static class PostgreSQLCredentials
    {
        /// <summary>
        /// Reads PostgreSQL credentials from an explicit file, the process environment, or the platform-specific PostgreSQL package.
        /// </summary>
        /// <param name="component">The component requesting the credential.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The password to supply only to the database child process environment.</returns>
        public static async Task<string> GetPasswordAsync(VirtualClientComponent component, CancellationToken cancellationToken)
        {
            component.ThrowIfNull(nameof(component));

            string passwordFile = component.Parameters.GetValue<string>("PostgreSQLPasswordFile", string.Empty);
            if (string.IsNullOrWhiteSpace(passwordFile))
            {
                string environmentPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
                if (!string.IsNullOrEmpty(environmentPassword))
                {
                    return environmentPassword;
                }

                DependencyPath package = await component.GetPlatformSpecificPackageAsync("postgresql", cancellationToken).ConfigureAwait(false);
                passwordFile = component.Combine(package.Path, "Password.json");
            }

            IFileSystem fileSystem = component.Dependencies.GetService<ISystemManagement>().FileSystem;
            string password = null;
            try
            {
                if (fileSystem.File.Exists(passwordFile))
                {
                    string contents = await fileSystem.File.ReadAllTextAsync(passwordFile, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(contents))
                    {
                        JToken value = JObject.Parse(contents)["Password"];
                        if (value?.Type == JTokenType.String)
                        {
                            password = value.Value<string>();
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is JsonException || exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new DependencyException(
                    "Unable to read the PostgreSQL password file. Check its permissions and JSON format.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new DependencyException(
                    "The PostgreSQL password file must contain a non-empty string named 'Password'. " +
                    "Provision Password.json in the platform-specific postgresql package, set PostgreSQLPasswordFile, or supply PGPASSWORD.",
                    ErrorReason.InvalidProfileDefinition);
            }

            return password;
        }
    }
}
