// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides methods for editing expressions associated with Virtual Client
    /// components.
    /// </summary>
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Returns true/false whether the text contains an expression reference (e.g. --port={Port} --threads={LogicalCoreCount}).
        /// </summary>
        /// <param name="text">The text to check for expressions.</param>
        /// <returns>True if the text contains expressions to evaluate.</returns>
        bool ContainsReference(string text);

        /// <summary>
        /// Returns true/false whether the parameters contain expression references.
        /// </summary>
        /// <param name="parameters">A set of parameters to check for expression references.</param>
        /// <returns>True if the parameters contain at least 1 expression to evaluate.</returns>
        bool ContainsReferences(IDictionary<string, IConvertible> parameters);

        /// <summary>
        /// Evaluates the expression and returns the results.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="text">The text having expressions to evaluate.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns>Text having any expressions evaluated and replaced with values.</returns>
        Task<string> EvaluateAsync(IServiceCollection dependencies, string text, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Evaluates any expressions within the set of parameters provided.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="parameters">A set of parameters that may have expressions to evaluate.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        Task EvaluateAsync(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken = default(CancellationToken));
    }
}
