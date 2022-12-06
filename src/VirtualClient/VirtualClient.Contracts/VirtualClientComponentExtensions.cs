// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for common operations in <see cref="VirtualClientComponent"/> derived
    /// classes.
    /// </summary>
    public static class VirtualClientComponentExtensions
    {
        // Example Format:
        // fio_{IOType}_{BlockSize}_{FileSize}_thmax{MaxThreads}
        private static readonly Regex ParameterReferenceExpression = new Regex(@"(\x7B[\x20-\x7A\x7C\x7D-\x7E]+\x7D)|(\x5B[\x20-\x5A\x5C\x5E-\x7E]+\x5D)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Applies the parameter value to any parameter references/placeholders within the text.
        /// </summary>
        /// <param name="component">The component related to the parameters.</param>
        /// <param name="text">The text containing references/placeholders to replace with values from the parameters.</param>
        /// <param name="parameterName">The parameter whose value will be used to replace the references/placeholders in the text.</param>
        /// <param name="value">The value to use when replacing the parameter reference/placeholder.</param>
        /// <returns>The text having all of the parameter references replaced with matching values.</returns>
        public static string ApplyParameter(this VirtualClientComponent component, string text, string parameterName, IConvertible value)
        {
            component.ThrowIfNull(nameof(component));
            text.ThrowIfNull(nameof(text));
            parameterName.ThrowIfNullOrWhiteSpace(nameof(parameterName));
            value.ThrowIfNull(nameof(value));

            string inlinedText = text.Replace($"{{{parameterName}}}", value.ToString(), StringComparison.OrdinalIgnoreCase);
            inlinedText = inlinedText.Replace($"[{parameterName}]", value.ToString(), StringComparison.OrdinalIgnoreCase);

            return inlinedText;
        }

        /// <summary>
        /// Applies parameter values to any parameter references/placeholders within the text.
        /// </summary>
        /// <param name="component">The component related to the parameters.</param>
        /// <param name="text">The text containing references/placeholders to replace with values from the parameters.</param>
        /// <param name="parameters">The parameters whose values will be used to replace the references/placeholders in the text.</param>
        /// <returns>The text having all of the parameter references replaced with matching values.</returns>
        public static string ApplyParameters(this VirtualClientComponent component, string text, IDictionary<string, IConvertible> parameters)
        {
            component.ThrowIfNull(nameof(component));
            text.ThrowIfNull(nameof(text));

            string inlinedText = text;
            if (parameters?.Any() == true)
            {
                MatchCollection parameterReferences = VirtualClientComponentExtensions.ParameterReferenceExpression.Matches(text);

                if (parameterReferences?.Any() == true)
                {
                    foreach (Match reference in parameterReferences)
                    {
                        string parameterName = reference.Value.Substring(1, reference.Value.Length - 2);
                        if (parameters.TryGetValue(parameterName, out IConvertible parameterValue))
                        {
                            inlinedText = inlinedText.Replace(reference.Value, parameterValue.ToString(), StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }

            return inlinedText;
        }

        /// <summary>
        /// Combines the path segments into a valid path for the OS platform.
        /// </summary>
        public static string Combine(this VirtualClientComponent component, params string[] pathSegments)
        {
            component.ThrowIfNull(nameof(component));
            return component.PlatformSpecifics.Combine(pathSegments);
        }

        /// <summary>
        /// Returns the client instance defined in the environment layout provided to the Virtual Client
        /// whose ID matches.
        /// </summary>
        /// <param name="component">The component with the environment layout.</param>
        /// <param name="clientId">The ID of the agent/client to match in the environment layout. Default = the current agent ID.</param>
        /// <param name="throwIfNotExists">True to throw an exception if the client instance does not exist.</param>
        public static ClientInstance GetLayoutClientInstance(this VirtualClientComponent component, string clientId = null, bool throwIfNotExists = true)
        {
            component.ThrowIfNull(nameof(component));

            if (throwIfNotExists)
            {
                component.ThrowIfLayoutNotDefined();
            }

            string desiredAgentId = clientId ?? component.AgentId;
            ClientInstance instance = component.Layout.GetClientInstance(desiredAgentId);

            if (throwIfNotExists && instance == null)
            {
                throw new DependencyException(
                    $"Client instance not found. A client instance does not exist in the environment layout " +
                    $"provided to the Virtual Client for agent ID '{desiredAgentId}'.",
                    ErrorReason.EnvironmentLayoutClientInstancesNotFound);
            }

            return instance;
        }

        /// <summary>
        /// Returns the set of client instance(s) defined in the environment layout provided to the Virtual Client
        /// whose role matches.
        /// </summary>
        /// <param name="component">The component with the environment layout.</param>
        /// <param name="role">The role of the client instance (e.g. Server, Client etc..)</param>
        /// <param name="throwIfNotExists">True to throw an exception if matching client instances do not exist.</param>
        public static IEnumerable<ClientInstance> GetLayoutClientInstances(this VirtualClientComponent component, string role, bool throwIfNotExists = true)
        {
            component.ThrowIfNull(nameof(component));

            if (throwIfNotExists)
            {
                component.ThrowIfLayoutNotDefined();
            }

            IEnumerable<ClientInstance> clientInstances = component.Layout?.Clients.Where(
                client => string.Equals(client.Role, role, StringComparison.OrdinalIgnoreCase));

            if (throwIfNotExists && clientInstances?.Any() != true)
            {
                throw new DependencyException(
                    $"Client instances not found. A set of client instances do not exist in the environment layout " +
                    $"provided to the Virtual Client for the role '{role}'.",
                    ErrorReason.EnvironmentLayoutClientInstancesNotFound);
            }

            return clientInstances;
        }

        /// <summary>
        /// Combines the path segments into a valid default packages path.
        /// </summary>
        public static string GetPackagePath(this VirtualClientComponent component, params string[] pathSegments)
        {
            component.ThrowIfNull(nameof(component));
            return component.PlatformSpecifics.GetPackagePath(pathSegments);
        }

        /// <summary>
        /// Returns true/false whether an environment layout was supplied and it
        /// defines more than 1 role for the client instances within.
        /// </summary>
        /// <returns>True if this is a multi-role scenario. False if not.</returns>
        public static bool IsMultiRoleLayout(this VirtualClientComponent component)
        {
            component.ThrowIfNull(nameof(component));

            bool isMultiRole = false;
            HashSet<string> distinctRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (component.Layout?.Clients?.Any() == true)
            {
                component.Layout.Clients.ToList().ForEach(client =>
                {
                    if (!string.IsNullOrWhiteSpace(client.Role))
                    {
                        distinctRoles.Add(client.Role);
                    }
                });

                isMultiRole = distinctRoles.Count > 1;
            }

            return isMultiRole;
        }

        /// <summary>
        /// Extension requests background profiler/profiling components to profile the system for the period
        /// of time defined.
        /// </summary>
        /// <param name="component">The component requesting the background system profiling.</param>
        /// <param name="cancellationToken">A token that can be used to gracefully terminate the profiler processes.</param>
        public static Task RequestProfilingAsync(this VirtualClientComponent component, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                // Invokes the SendReceiveInstructions event to enable any subscribers of the event (1 or more, e.g. Azure Profiler)
                // to begin profiling.
                Instructions instructions = new ProfilerInstructions(
                    InstructionsType.Profiling,
                    component.Parameters);

                VirtualClientEventing.OnSendReceiveInstructions(component, new InstructionsEventArgs(instructions, cancellationToken));
            });
        }

        /// <summary>
        /// Extension signals that the server-side is online and ready to receive requests from clients.
        /// </summary>
        /// <param name="component">The component signalling server-side readiness.</param>
        /// <param name="isOnline">True to signal the server-side is ready, false to signal it is not.</param>
        public static void SetServerOnline(this VirtualClientComponent component, bool isOnline)
        {
            VirtualClientEventing.SetEventingApiOnline(isOnline);
        }

        /// <summary>
        /// Checks to see if the component has the required parameter defined and throws an exception if
        /// it is not.
        /// </summary>
        public static void ThrowIfParameterNotDefined(this VirtualClientComponent component, string parameterName, params IConvertible[] allowedValues)
        {
            component.ThrowIfNull(nameof(component));
            parameterName.ThrowIfNullOrWhiteSpace(nameof(parameterName));
            allowedValues.ThrowIfInvalid(nameof(allowedValues), val => val != null);

            if (!component.Parameters.ContainsKey(parameterName))
            {
                throw new DependencyException(
                    $"Missing required parameter. The '{component.TypeName}' component requires a parameter '{parameterName}' to be defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (allowedValues?.Any() == true)
            {
                bool isValid = false;
                IConvertible parameterValue = component.Parameters[parameterName];
                foreach (IConvertible allowable in allowedValues)
                {
                    if (parameterValue.ToString().Equals(allowable.ToString()))
                    {
                        isValid = true;
                        break;
                    }
                }

                if (!isValid)
                {
                    throw new DependencyException(
                       $"Invalid parameter value. The parameter '{parameterName}' for component '{component.TypeName}' component supports the following allowed values: " +
                       $"{string.Join(", ", allowedValues.Select(v => v?.ToString()))}.",
                       ErrorReason.InvalidProfileDefinition);
                }
            }
        }

        /// <summary>
        /// Checks to see if the component has supported roles defined and throws an exception if
        /// the role provided is not one of them.
        /// </summary>
        public static void ThrowIfRoleNotSupported(this VirtualClientComponent component, string role)
        {
            component.ThrowIfNull(nameof(component));
            if (component.SupportedRoles?.Any() == true)
            {
                if (!component.SupportedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"The role 'role' is not supported for this workload. Supported roles include: {string.Join(',', component.SupportedRoles)}.  " +
                        $"Verify that the correct roles are supplied to the application in the environment layout for each of the client instances.");
                }
            }
        }

        /// <summary>
        /// Verifies an enviroment layout is defined and throws an exception if not.
        /// </summary>
        public static void ThrowIfLayoutNotDefined(this VirtualClientComponent component)
        {
            component.ThrowIfNull(nameof(component));

            if (component.Layout?.Clients?.Any() != true)
            {
                throw new DependencyException(
                    "The environment layout is not defined. An environment layout must be provided to the " +
                    "Virtual Client application on the command line.",
                    ErrorReason.EnvironmentLayoutNotDefined);
            }
        }

        /// <summary>
        /// Verifies the IP address exists on the local system or throws an exception.
        /// </summary>
        /// <param name="component">The component checking the IP address.</param>
        /// <param name="ipAddress">The IP address to verify on the local system.</param>
        public static void ThrowIfLayoutClientIPAddressNotFound(this VirtualClientComponent component, string ipAddress)
        {
            component.ThrowIfNull(nameof(component));

            ISystemInfo systemInfo = component.Dependencies.GetService<ISystemInfo>();
            if (!systemInfo.IsLocalIPAddress(ipAddress))
            {
                throw new WorkloadException(
                    $"The IP address defined in the environment layout for this agent " +
                    $"instance '{ipAddress}' does not match with the IP addresses defined on the system.",
                    ErrorReason.LayoutIPAddressDoesNotMatch);
            }
        }

        /// <summary>
        /// Returns the path for the dependency/package given a specific platform and CPU architecture.
        /// </summary>
        /// <param name="component">VC component.</param>
        /// <param name="dependency">The dependency path.</param>
        /// <param name="platform">The OS/system platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <returns>
        /// The dependency/package path given the specific platform and CPU architecture
        /// (e.g. /home/any/path/virtualclient/1.2.3.4/packages/geekbench5.1.0.0/linux-x64)
        /// </returns>
        public static DependencyPath ToPlatformSpecificPath(this VirtualClientComponent component, DependencyPath dependency, PlatformID platform, Architecture? architecture = null)
        {
            component.ThrowIfNull(nameof(component));
            dependency.ThrowIfNull(nameof(dependency));

            return component.PlatformSpecifics.ToPlatformSpecificPath(dependency, platform, architecture);
        }

        /// <summary>
        /// Sets the value of the parameters not defined in the profile and add them to parameters Dictionary
        /// </summary>
        /// <param name="parameters"> Parameters defined in the profile or supplied on the command line..</param>
        /// <param name="key">The name of the parameter.</param>
        /// <param name="value">The value to be set for the parameter.</param>
        public static void SetIfNotDefined(this IDictionary<string, IConvertible> parameters, string key, IConvertible value)
        {
            parameters.ThrowIfNull(nameof(parameters));

            if (!parameters.ContainsKey(key))
            { 
                parameters.Add(key, value);
            }

        }
    }
}
