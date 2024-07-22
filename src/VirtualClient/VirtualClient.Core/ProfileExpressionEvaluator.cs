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
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for editing expressions associated with profile 
    /// components and parameters
    /// </summary>
    public class ProfileExpressionEvaluator : IExpressionEvaluator
    {
        // e.g.
        // {fn(512 / 16)]}
        // {fn(512 / {LogicalThreadCount})}
        private static readonly Regex CalculateExpression = new Regex(
            @"\{calculate\(([0-9\*\/\+\-\(\)\s]+)\)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {calculate({IsTLSEnabled} ? "Yes" : "No")}
        // (([^?]+)\s*\?\s*([^:]+)\s*:\s*([^)]+))
        private static readonly Regex CalculateTernaryExpression = new Regex(
            @"\{calculate\((([^?]+)\s*\?\s*([^:]+)\s*:\s*([^)]+))\)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // Expression: {calculate(512 == 4)}
        // Expression: {calculate(512 > 2)}
        // Expression: {calculate(512 != {LogicalCoreCount})}
        private static readonly Regex CalculateComparisionExpression = new Regex(
            @"\{calculate\((\d+\s*(?:==|!=|<|>|<=|>=|&&|\|\|)\s*\d+)\)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        // {ScriptPath:redis}
        private static readonly Regex ScriptPathExpression = new Regex(
            @"\{ScriptPath\:([a-z0-9-_\. ]+)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {PackagePath/Platform:fio}
        private static readonly Regex PackagePathForPlatformExpression = new Regex(
            @"\{PackagePath/Platform\:([a-z0-9-_\. ]+)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {Platform}
        private static readonly Regex PlatformExpression = new Regex(
            @"\{Platform\}",
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

        // e.g.
        // {SystemMemoryBytes}
        // {SystemMemoryKilobytes}
        // {SystemMemoryMegabytes}
        // {SystemMemoryGigabytes}
        private static readonly Regex SystemMemoryInBytesExpression = new Regex(
            @"\{SystemMemoryBytes\}|\{SystemMemoryKilobytes\}|\{SystemMemoryMegabytes\}|\{SystemMemoryGigabytes\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // {Duration.TotalDays}
        // {Duration.TotalHours}
        // {Duration.TotalMilliseconds}
        // {Duration.TotalMinutes}
        // {Duration.TotalSeconds}
        private static readonly Regex TimeSpanExpression = new Regex(
            @"\{([a-z0-9_-]+)\.(TotalDays|TotalHours|TotalMilliseconds|TotalMinutes|TotalSeconds)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The set of expressions and evaluators supported by the editor. Additional expressions
        /// and evaluators can be added (e.g. {PackagePath/Special:redis}).
        /// </summary>
        private static readonly IList<Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>> Evaluators = new List<Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>>
        {
            // Expression: {ScriptPath:xyz}
            // this.PlatformSpecifics.GetScriptPath("a","b");
            // Resolves to the path to the Script folder location (e.g. /home/users/virtualclient/scripts/redis).
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>((dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.ScriptPathExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    foreach (Match match in matches)
                    {
                        string scriptFolderPath = systemManagement.PlatformSpecifics.GetScriptPath(match.Groups[1].Value);

                        if (scriptFolderPath == null)
                        {
                            throw new DependencyException(
                                $"Cannot evaluate expression {{ScriptPath:{match.Value}}}. A scipt with the name '{match.Value}' does not " +
                                $"exist on system or is not registered with Virtual Client.",
                                ErrorReason.DependencyNotFound);
                        }

                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, scriptFolderPath);
                    }
                }

                return Task.FromResult(new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                });
            }),
            // Expression: {Platform}
            // Resolves to the current platform-architecture for the system (e.g. linux-arm64, linux-x64, win-arm64, win-x64).
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>((dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PlatformExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    PlatformSpecifics platformSpecifics = systemManagement.PlatformSpecifics;

                    foreach (Match match in matches)
                    {
                        evaluatedExpression = Regex.Replace(
                            evaluatedExpression,
                            match.Value,
                            platformSpecifics.PlatformArchitectureName);
                    }
                }

                return Task.FromResult(new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                });
            }),
            // Expression: {PackagePath:xyz}
            // Resolves to the path to the package folder location (e.g. /home/users/virtualclient/packages/redis).
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PackagePathExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    foreach (Match match in matches)
                    {
                        DependencyPath package = await systemManagement.PackageManager.GetPackageAsync(match.Groups[1].Value, CancellationToken.None);

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

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // Expression: {PackagePath/Platform:xyz}
            // Resolves to the path to the package platform-specific folder location (e.g. /home/users/virtualclient/packages/fio/linux-x64).
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PackagePathForPlatformExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    PlatformSpecifics platformSpecifics = systemManagement.PlatformSpecifics;

                    foreach (Match match in matches)
                    {
                        DependencyPath package = await systemManagement.PackageManager.GetPackageAsync(match.Groups[1].Value, CancellationToken.None);

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

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // e.g.
            // {Duration.TotalDays}
            // {Duration.TotalHours}
            // {Duration.TotalMilliseconds}
            // {Duration.TotalMinutes}
            // {Duration.TotalSeconds}
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>((dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;

                if (parameters?.Any() == true)
                {
                    MatchCollection matches = ProfileExpressionEvaluator.TimeSpanExpression.Matches(expression);

                    if (matches?.Any() == true)
                    {
                        string timespanParameterName = null;
                        string unitOfTime = null;

                        foreach (Match match in matches)
                        {
                            timespanParameterName = match.Groups[1].Value?.Trim();
                            if (parameters.TryGetValue(timespanParameterName, out IConvertible value) == true)
                            {
                                if (!TimeSpan.TryParse(value.ToString(), out TimeSpan duration))
                                {
                                    throw new DependencyException(
                                        $"Invalid '{timespanParameterName}' parameter value. The value of the '{timespanParameterName}' parameter is expected to be formatted as a time span (e.g. 00:30:00).",
                                        ErrorReason.InvalidProfileDefinition);
                                }

                                isMatched = true;
                                unitOfTime = match.Groups[2].Value;

                                switch (unitOfTime.ToLowerInvariant())
                                {
                                    case "totaldays":
                                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, duration.TotalDays.ToString());
                                        break;

                                    case "totalhours":
                                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, duration.TotalHours.ToString());
                                        break;

                                    case "totalmilliseconds":
                                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, duration.TotalMilliseconds.ToString());
                                        break;

                                    case "totalminutes":
                                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, duration.TotalMinutes.ToString());
                                        break;

                                    case "totalseconds":
                                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, duration.TotalSeconds.ToString());
                                        break;

                                    default:
                                        throw new DependencyException(
                                            $"Invalid duration parameter reference value. The parameter reference '{timespanParameterName}.{unitOfTime}' is not a supported duration reference. The following " +
                                            $"duration references are valid: Duration.TotalDays, Duration.TotalHours, Duration.TotalMilliseconds, Duration.TotalMinutes, Duration.TotalSeconds",
                                            ErrorReason.InvalidProfileDefinition);
                                }
                            }
                        }
                    }
                }

                return Task.FromResult(new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                });
            }),
            // Expression: {LogicalCoreCount}
            // Resolves to the count of logical cores on the system (e.g. Environment.ProcessorCount)
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.LogicalCoreCountExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    CpuInfo cpuInfo = await systemManagement.GetCpuInfoAsync(CancellationToken.None);

                    foreach (Match match in matches)
                    {
                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, cpuInfo.LogicalProcessorCount.ToString());
                    }
                }

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // Expression: {PhysicalCoreCount}
            // Resolves to the count of the physical cores on the system.
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.PhysicalCoreCountExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemInfo systemInfo = dependencies.GetService<ISystemInfo>();
                    CpuInfo cpuInfo = await systemInfo.GetCpuInfoAsync(CancellationToken.None);

                    foreach (Match match in matches)
                    {
                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, cpuInfo.PhysicalCoreCount.ToString());
                    }
                }

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // Expression: {SystemMemoryBytes}
            // Expression: {SystemMemoryKilobytes}
            // Expression: {SystemMemoryMegabytes}
            // Expression: {SystemMemoryGigabytes}
            // Resolves to the total system memory/RAM (in kilobytes).
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.SystemMemoryInBytesExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    ISystemInfo systemInfo = dependencies.GetService<ISystemInfo>();
                    MemoryInfo memoryInfo = await systemInfo.GetMemoryInfoAsync(CancellationToken.None);

                    foreach (Match match in matches)
                    {
                        // Memory in kilobytes (using 1024 bytes as a kilobyte for memory standard). This is sometimes
                        // called a kibibyte, but no one uses this term as it was an after thought in the earlier days
                        // of defining what a kilobyte actually means (i.e. 1024 bytes).
                        long memory = memoryInfo.TotalMemory;

                        switch (match.Value.ToLowerInvariant())
                        {
                            case "{systemmemorybytes}":
                                memory = memory * 1024; // bytes = kilobytes * 1024
                                break;

                            case "{systemmemorymegabytes}":
                                memory = memory / 1024; // megabytes = kilobytes / 2014
                                break;

                            case "{systemmemorygigabytes}":
                                memory = (memory / 1024) / 1024; // gigabytes = (kilobytes / 1024) / 2014
                                break;
                        }

                        evaluatedExpression = Regex.Replace(evaluatedExpression, match.Value, memory.ToString());
                    }
                }

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // Expression: {calculate(512 * 4)}
            // Expression: {calculate(512 / (4 / 2))}
            // Expression: {calculate(512 / {LogicalCoreCount})}
            //
            // **IMPORTANT**
            // This expression evaluation MUST come last after ALL other expression evaluators.
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.CalculateExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    foreach (Match match in matches)
                    {
                        string function = match.Groups[1].Value;
                        int result = await Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<int>(function);

                        evaluatedExpression = evaluatedExpression.Replace(match.Value, result.ToString());
                    }
                }

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // Expression: {calculate(512 == 4)}
            // Expression: {calculate(512 > 2)}
            // Expression: {calculate(512 != {LogicalCoreCount})}
            // **IMPORTANT**
            // This expression evaluation MUST come last after arthematic caluculation evaluators.
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.CalculateComparisionExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    foreach (Match match in matches)
                    {
                        string function = match.Groups[1].Value;
                        bool result = await Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<bool>(function);

                        evaluatedExpression = evaluatedExpression.Replace(match.Value, result.ToString());
                    }
                }

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            }),
            // Expression: {calculate({IsTLSEnabled} ? "Yes" : "No")}
            // Expression: {calculate(calculate(512 == 2) ? "Yes" : "No")}
            // **IMPORTANT**
            // This expression evaluation MUST come last after arthematic/logical/comparative caluculation evaluators.
            new Func<IServiceCollection, IDictionary<string, IConvertible>, string, Task<EvaluationResult>>(async (dependencies, parameters, expression) =>
            {
                bool isMatched = false;
                string evaluatedExpression = expression;
                MatchCollection matches = ProfileExpressionEvaluator.CalculateTernaryExpression.Matches(expression);

                if (matches?.Any() == true)
                {
                    isMatched = true;
                    foreach (Match match in matches)
                    {
                        string function = match.Groups[1].Value;

                        function = Regex.Replace(function, @"(?<=\b)(True|False)(?=\s*\?)", m =>
                        {
                            return m.Value.ToLower();
                        });

                        string result = await Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<string>(function);
                        evaluatedExpression = evaluatedExpression.Replace(match.Value, result.ToString());
                    }
                }

                return new EvaluationResult
                {
                    IsMatched = isMatched,
                    Outcome = evaluatedExpression
                };
            })
        };

        private ProfileExpressionEvaluator()
        {
        }

        /// <summary>
        /// The singleton instance of the <see cref="ProfileExpressionEvaluator"/> class.
        /// </summary>
        public static ProfileExpressionEvaluator Instance { get; } = new ProfileExpressionEvaluator();

        /// <summary>
        /// Returns true/false whether the text contains an expression reference (e.g. --port={Port} --threads={LogicalCoreCount}).
        /// </summary>
        /// <param name="text">The text to check for expressions.</param>
        /// <returns>True if the text contains expressions to evaluate.</returns>
        public bool ContainsReference(string text)
        {
            return ProfileExpressionEvaluator.GeneralExpression.IsMatch(text);
        }

        /// <summary>
        /// Returns true/false whether the parameters contain expression references.
        /// </summary>
        /// <param name="parameters">A set of parameters to check for expression references.</param>
        /// <returns>True if the parameters contain at least 1 expression to evaluate.</returns>
        public bool ContainsReferences(IDictionary<string, IConvertible> parameters)
        {
            bool containsReferences = false;
            if (parameters?.Any() == true)
            {
                foreach (var entry in parameters)
                {
                    if (entry.Value != null && entry.Value is string)
                    {
                        if (ProfileExpressionEvaluator.GeneralExpression.IsMatch(entry.Value.ToString()))
                        {
                            containsReferences = true;
                            break;
                        }
                    }
                }
            }

            return containsReferences;
        }

        /// <summary>
        /// Evaluates the expression and returns the results.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="text">The text having expressions to evaluate.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns>Text having any expressions evaluated and replaced with values.</returns>
        public async Task<string> EvaluateAsync(IServiceCollection dependencies, string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            EvaluationResult evaluation = await ProfileExpressionEvaluator.EvaluateExpressionAsync(dependencies, null, text, cancellationToken);
            return evaluation.Outcome;
        }

        /// <summary>
        /// Evaluates any expressions within the set of parameters provided.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required for evaluating expressions.</param>
        /// <param name="parameters">A set of parameters that may have expressions to evaluate.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public async Task EvaluateAsync(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken = default(CancellationToken))
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

            int maxIterations = 5;
            int iterations = 0;
            while (this.ContainsReferences(parameters) && iterations < maxIterations)
            {
                iterations++;

                // We take as many passes through to ensure that all placeholders/expressions have been evaluated. This allows
                // placeholders that are themselves contained/nested within parent placeholders to be successfully resolved.
                ProfileExpressionEvaluator.EvaluateParameterSpecificExpressions(dependencies, parameters, cancellationToken);
            }

            iterations = 0;
            while (this.ContainsReferences(parameters) && iterations < maxIterations)
            {
                iterations++;
                await ProfileExpressionEvaluator.EvaluateWellKnownExpressionsAsync(dependencies, parameters, cancellationToken);
            }
        }

        private static async Task<EvaluationResult> EvaluateExpressionAsync(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, string text, CancellationToken cancellationToken)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            bool isMatched = false;
            string evaluatedExpression = text;
            if (!string.IsNullOrWhiteSpace(text) && !cancellationToken.IsCancellationRequested)
            {
                if (ProfileExpressionEvaluator.GeneralExpression.IsMatch(text))
                {
                    foreach (var evaluator in ProfileExpressionEvaluator.Evaluators)
                    {
                        EvaluationResult evaluation = await evaluator.Invoke(dependencies, parameters, evaluatedExpression);
                        if (evaluation.IsMatched)
                        {
                            isMatched = true;
                            evaluatedExpression = evaluation.Outcome;
                        }
                    }
                }
            }

            return new EvaluationResult
            {
                IsMatched = isMatched,
                Outcome = evaluatedExpression
            };
        }

        private static bool EvaluateParameterSpecificExpressions(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken)
        {
            bool matchesFound = false;
            foreach (var parameter in parameters)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (parameter.Value != null && parameter.Value is string)
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
                                    if (parameters.TryGetValue(match.Groups[1].Value, out IConvertible value))
                                    {
                                        matchesFound = true;
                                        string referencedParameterValue = value?.ToString();

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

                                        parameters[parameter.Key] = parameters[parameter.Key].ToString().Replace(match.Groups[0].Value, referencedParameterValue.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return matchesFound;
        }

        private static async Task<bool> EvaluateWellKnownExpressionsAsync(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, CancellationToken cancellationToken)
        {
            bool expressionsFound = false;
            foreach (var parameter in parameters)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (parameter.Value is string)
                    {
                        MatchCollection expressionMatches = ProfileExpressionEvaluator.GeneralExpression.Matches(parameter.Value.ToString());
                        if (expressionMatches?.Any() == true)
                        {
                            expressionsFound = true;

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
                            EvaluationResult evaluation = await ProfileExpressionEvaluator.EvaluateExpressionAsync(dependencies, parameters, parameter.Value.ToString(), cancellationToken);
                            if (evaluation.IsMatched)
                            {
                                parameters[parameter.Key] = evaluation.Outcome;
                            }
                        }
                    }
                }
            }

            return expressionsFound;
        }

        private class EvaluationResult
        {
            public bool IsMatched { get; set; }

            public string Outcome { get; set; }
        }
    }
}
