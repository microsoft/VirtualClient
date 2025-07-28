// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides context information for a file produced by a Virtual Client
    /// component or related toolset.
    /// </summary>
    public class FileContext
    {
        private static readonly Regex TemplatePlaceholderExpression = new Regex(@"\{(.*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="FileContext"/> class.
        /// </summary>
        /// <param name="file">The file for which the context is associated.</param>
        /// <param name="contentType">The web content type of the file (e.g. application/json, application/octet-stream, text/plain).</param>
        /// <param name="contentEncoding">The web content encoding for the file (e.g. utf-8).</param>
        /// <param name="experimentId">The ID of the experiment running that produced the file.</param>
        /// <param name="agentId">The ID of the agent running that produced the file.</param>
        /// <param name="toolname">The name of the tool/toolset that produced the file (e.g. FioExecutor, FIO).</param>
        /// <param name="scenario">The scenario in which the tool/toolset running is related (e.g. fio_randwrite_128g_4k_d32_th4, NTttcp_TCP_4K_Buffer_T1). This is often defined in the profile component parameters.</param>
        /// <param name="commandArguments">Arguments supplied to the toolset in the scenario that produced the file.</param>
        /// <param name="role">The role for the current instance of the application (e.g. Client, Server).</param>
        public FileContext(IFileInfo file, string contentType, string contentEncoding, string experimentId, string agentId = null, string toolname = null, string scenario = null, string commandArguments = null, string role = null)
        {
            file.ThrowIfNull(nameof(file));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            contentEncoding.ThrowIfNullOrWhiteSpace(nameof(contentEncoding));
            experimentId.ThrowIfNullOrWhiteSpace(nameof(experimentId));

            this.File = file;
            this.ContentType = contentType;
            this.ContentEncoding = contentEncoding;
            this.ExperimentId = experimentId;
            this.AgentId = agentId;
            this.ToolName = toolname;
            this.Scenario = scenario;
            this.CommandArguments = commandArguments;
            this.Role = role;
        }

        /// <summary>
        /// The ID of the agent running that produced the file.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Arguments supplied to the toolset in the scenario that produced the file.
        /// </summary>
        public string CommandArguments { get; }

        /// <summary>
        /// The web content type of the file (e.g. application/json, application/octet-stream, text/plain).
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The web content encoding for the file (e.g. utf-8).
        /// </summary>
        public string ContentEncoding { get; }

        /// <summary>
        /// The ID of the experiment running that produced the file.
        /// </summary>
        public string ExperimentId { get; }

        /// <summary>
        /// The file for which the context is associated.
        /// </summary>
        public IFileInfo File { get; }

        /// <summary>
        /// The role for the current instance of the application (e.g. Client, Server)
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// The scenario in which the tool/toolset running is related (e.g. fio_randwrite_128g_4k_d32_th4, NTttcp_TCP_4K_Buffer_T1). 
        /// This is often defined in the profile component parameters.
        /// </summary>
        public string Scenario { get; }

        /// <summary>
        /// The name of the tool/toolset that produced the file (e.g. FioExecutor, FIO).
        /// </summary>
        public string ToolName { get; }

        /// <summary>
        /// Resolves placeholders in the path template provided.
        /// </summary>
        /// <param name="pathTemplate">A path template containing placeholders to resolve (e.g. {experimentId}-summary.txt).</param>
        /// <param name="replacements">Provides the replacement values for the placeholders in the path template.</param>
        /// <returns>
        /// A path having matching placeholders replaced with actual values 
        /// (e.g. {experimentId}-summary.txt -> afda108a-4be9-4fe2-a9ef-7b787150896a-summary.txt).
        /// </returns>
        public static string ResolvePathTemplate(string pathTemplate, IDictionary<string, IConvertible> replacements)
        {
            string resolvedTemplate = pathTemplate;
            MatchCollection matches = FileContext.TemplatePlaceholderExpression.Matches(pathTemplate);

            if (matches?.Any() == true)
            {
                string resolvedValue;
                foreach (Match match in matches)
                {
                    string[] effectivePlaceholders = null;
                    string templatePlaceholder = match.Groups[1].Value;
                    if (templatePlaceholder.IndexOf('|') < 0)
                    {
                        effectivePlaceholders = new string[] { templatePlaceholder };
                    }
                    else
                    {
                        effectivePlaceholders = templatePlaceholder.Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    }

                    bool placeholderMatched = false;
                    foreach (string placeholder in effectivePlaceholders)
                    {
                        // Order of placeholder resolution:
                        // 1) Metadata known by the VC runtime is applied first because it is definitive.
                        // 2) Component metadata supplied to the factory.
                        // 3) Component parameters supplied to the factory.
                        if (replacements?.Any() == true && FileContext.TryResolvePlaceholder(replacements, placeholder, out resolvedValue))
                        {
                            placeholderMatched = true;
                            resolvedTemplate = resolvedTemplate.Replace(match.Value, resolvedValue);
                            break;
                        }
                    }

                    if (!placeholderMatched)
                    {
                        resolvedTemplate = resolvedTemplate.Replace(match.Value, string.Empty);
                    }
                }
            }

            return resolvedTemplate;
        }

        private static bool TryResolvePlaceholder(IDictionary<string, IConvertible> metadata, string propertyName, out string resolvedValue)
        {
            resolvedValue = null;
            if (!string.IsNullOrWhiteSpace(propertyName) && metadata.TryGetValue(propertyName, out IConvertible propertyValue) && propertyValue != null)
            {
                resolvedValue = propertyValue.ToString();
            }

            return resolvedValue != null;
        }
    }
}
