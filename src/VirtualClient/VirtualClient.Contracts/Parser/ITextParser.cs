// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    /// <summary>
    /// Provides features for parsing interesting information from the results of a
    /// toolset/command execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITextParser<T>
    {
        /// <summary>
        /// Override if special parsing of the text is needed.
        /// </summary>
        bool TryParse(string text, out T results);
    }
}
