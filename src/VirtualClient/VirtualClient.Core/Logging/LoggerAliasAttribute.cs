// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines one or more aliases for ILoggerProvider class.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class LoggerAliasAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerAliasAttribute"/> class.
        /// </summary>
        /// <param name="alias">Alias or aliases delimited by comma.</param>
        public LoggerAliasAttribute(string alias)
        {
            alias.ThrowIfNullOrEmpty(nameof(alias));
            this.Aliases = alias.Split(',');
        }

        /// <summary>
        /// The aliases for the logger provider.
        /// </summary>
        public IEnumerable<string> Aliases { get; }
    }
}