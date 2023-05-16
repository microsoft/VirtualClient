// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for editing expressions associated with profile 
    /// components and parameters
    /// </summary>
    public static class ProfileExpressionEvaluator
    {
        // e.g.
        // {Expression...}
        private static readonly Regex GeneralExpression = new Regex(
            @"\{([\x20-\x7A\x7c\x7E]+)\}+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {PackagePath:redis}
        private static readonly Regex PackagePathExpression = new Regex(
            @"\{PackagePath\:([a-z0-9-_\. ]+)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {PackagePath/Platform:fio}
        private static readonly Regex PackagePathForPlatformExpression = new Regex(
            @"\{PackagePath/Platform\:([a-z0-9-_\. ]+)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {LogicalCoreCount}
        private static readonly Regex LogicalCoreCountExpression = new Regex(
            @"\{LogicalCoreCount\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {PhysicalCoreCount}
        private static readonly Regex PhysicalCoreCountExpression = new Regex(
            @"\{PhysicalCoreCount\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The set of expressions and evaluators supported by the editor. Additional expressions
        /// and evaluators can be added (e.g. {PackagePath/Special:redis}).
        /// </summary>
        private static readonly IList<Func<IServiceCollection, string, string>> Evaluators = new List<Func<IServiceCollection, string, string>>
        {
            // Expression: {PackagePath:xyz}
            // Resolves to the path to the package folder location (e.g. /home/users/virtualclient/packages/redis).
            new Func<IServiceCollection, string, string>((dependencies, expression) =>
            {
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PackagePathExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    foreach (Match match in matches)
                    {
                        DependencyPath package = systemManagement.PackageManager.GetPackageAsync(match.Groups[1].Value, CancellationToken.None)
                            .GetAwaiter().GetResult();

                        if (package == null)
                        {
                            throw new DependencyException(
                                $"Cannot evaluate expression {{PackagePath:{match.Value}}}. A package with the name '{match.Value}' does not " +
                                $"exist on system or is not registered with Virtual Client.",
                                ErrorReason.DependencyNotFound);
                        }

                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, package.Path);
                    }
                }

                return evaluatedExpression;
            }),
            // Expression: {PackagePath/Platform:xyz}
            // Resolves to the path to the package platform-specific folder location (e.g. /home/users/virtualclient/packages/fio/linux-x64).
            new Func<IServiceCollection, string, string>((dependencies, expression) =>
            {
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PackagePathForPlatformExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    PlatformSpecifics platformSpecifics = systemManagement.PlatformSpecifics;

                    foreach (Match match in matches)
                    {
                        DependencyPath package = systemManagement.PackageManager.GetPackageAsync(match.Groups[1].Value, CancellationToken.None)
                            .GetAwaiter().GetResult();

                        if (package == null)
                        {
                            throw new DependencyException(
                                $"Cannot evaluate expression {{PackagePath/Platform:{match.Value}}}. A package with the name '{match.Value}' does not " +
                                $"exist on system or is not registered with Virtual Client.",
                                ErrorReason.DependencyNotFound);
                        }

                        evaluatedExpression = Regex.Replace(
                            evaluatedExpression,
                            match.Value,
                            platformSpecifics.ToPlatformSpecificPath(package, systemManagement.Platform, systemManagement.CpuArchitecture).Path);
                    }
                }

                return evaluatedExpression;
            }),
            // Expression: {LogicalCoreCount}
            // Resolves to the count of logical cores on the system (e.g. Environment.ProcessorCount)
            new Func<IServiceCollection, string, string>((dependencies, expression) =>
            {
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.LogicalCoreCountExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    CpuInfo cpuInfo = systemManagement.GetCpuInfoAsync(CancellationToken.None)
                        .GetAwaiter().GetResult();

                    foreach (Match match in matches)
                    {
                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, cpuInfo.LogicalCoreCount.ToString());
                    }
                }

                return evaluatedExpression;
            }),
            // Expression: {PhysicalCoreCount}
            // Resolves to the count of the physical cores on the system.
            new Func<IServiceCollection, string, string>((dependencies, expression) =>
            {
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PhysicalCoreCountExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    ISystemInfo systemInfo = dependencies.GetService<ISystemInfo>();
                    CpuInfo cpuInfo = systemInfo.GetCpuInfoAsync(CancellationToken.None)
                        .GetAwaiter().GetResult();

                    foreach (Match match in matches)
                    {
                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, cpuInfo.PhysicalCoreCount.ToString());
                    }
                }

                return evaluatedExpression;
            })
        };

        /// <summary>
        /// Returns true/false whether the text contains an expression reference (e.g. --port={Port} --threads={LogicalCoreCount}).
        /// </summary>
        /// <param name="text">The text to check for expressions.</param>
        /// <returns>True if the text contains expressions to evaluate.</returns>
        public static bool ContainsReference(string text)
        {
            return ProfileExpressionEvaluator.GeneralExpression.IsMatch(text);
        }

        /// <summary>
        /// Evaluates the expression and returns the results.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="text">The text having expressions to evaluate.</param>
        /// <returns>Text having any expressions evaluated and replaced with values.</returns>
        public static string Evaluate(IServiceCollection dependencies, string text)
        {
            return ProfileExpressionEvaluator.EvaluateAsync(dependencies, text, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Evaluates any expressions within the set of parameters provided.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="parameters">A set of parameters that may have expressions to evaluate.</param>
        public static void Evaluate(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
        {
            ProfileExpressionEvaluator.EvaluateAsync(dependencies, parameters, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Evaluates the expression and returns the results.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="text">The text having expressions to evaluate.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns>Text having any expressions evaluated and replaced with values.</returns>
        public static Task<string> EvaluateAsync(IServiceCollection dependencies, string text, CancellationToken cancellationToken)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            return Task.Run(() =>
            {
                string outcome = text;
                if (!string.IsNullOrWhiteSpace(text) && !cancellationToken.IsCancellationRequested)
                {
                    if (ProfileExpressionEvaluator.GeneralExpression.IsMatch(text))
                    {
                        foreach (var evaluator in ProfileExpressionEvaluator.Evaluators)
                        {
                            outcome = evaluator.Invoke(dependencies, outcome);
                        }
                    }
                }

                return outcome;
            });
        }

        /// <summary>
        /// Evaluates any expressions within the set of parameters provided.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="parameters">A set of parameters that may have expressions to evaluate.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static Task EvaluateAsync(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                // Priority of Parameter evaluation
                // 1) Parameter replacements that are defined within the parameter set itself.
                //
                //    e.g.
                //    Parameters: {
                //        CommandLine: --port={Port},
                //       Port: 6379
                //    }
                //
                // 2) Parameter replacements for well-known expressions.
                //
                //    e.g.
                //    Parameters: {
                //       CommandLine: --port=6389 --threads={LogicalCoreCount}
                //    }
                //
                //    LogicalCoreCount = # of logical cores on system.

                // 1) Local Parameter Definitions
                ProfileExpressionEvaluator.EvaluateParameterSpecificExpressions(parameters, cancellationToken);

                // 2) Well-known expressions
                ProfileExpressionEvaluator.EvaluateWellKnownExpressions(dependencies, parameters, cancellationToken);
            });
        }

        private static void EvaluateParameterSpecificExpressions(IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken)
        {
            foreach (var parameter in parameters)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (parameter.Value is string)
                    {
                        MatchCollection expressionMatches = ProfileExpressionEvaluator.GeneralExpression.Matches(parameter.Value.ToString());

                        if (expressionMatches?.Any() == true)
                        {
                            foreach (Match match in expressionMatches)
                            {
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    // Groups[0] = the full matched text (e.g. {Port})
                                    // Groups]1] = the capture group (e.g. 1234).
                                    if (parameters.TryGetValue(match.Groups[1].Value, out IConvertible referencedParameter))
                                    {
                                        // Parameters:
                                        //    CommandLine: --port={Port} --threads={ThreadCount}
                                        //    Port: 1234
                                        //    Threads: 8
                                        //
                                        // Desired Outcome
                                        // Parameters:
                                        //    CommandLine: --port=1234 --threads=8
                                        //    Port: 1234
                                        //    Threads: 8
                                        parameters[parameter.Key] = parameters[parameter.Key].ToString().Replace(match.Groups[0].Value, referencedParameter.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void EvaluateWellKnownExpressions(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken)
        {
            foreach (var parameter in parameters)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (parameter.Value is string)
                    {
                        // Parameters:
                        //    CommandLine: --port={Port} --threads={LogicalCoreCount} --package={PackagePath}
                        //    Port: 1234
                        //
                        // And given logical core count = 8, a package path of /home/users/virtualclient/packages/anypackage
                        //
                        // Desired Outcome
                        // Parameters:
                        //    CommandLine: --port=1234 --threads=8 --package=/home/users/virtualclient/packages/anypackage
                        //    Port: 1234
                        string outcome = ProfileExpressionEvaluator.Evaluate(dependencies, parameter.Value.ToString());
                        parameters[parameter.Key] = outcome;
                    }
                }
            }
        }
    }
}
