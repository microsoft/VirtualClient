// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="ComponentTypeCache"/> instances.
    /// </summary>
    public static class ComponentTypeCacheExtensions
    {
        private static readonly Type DescriptorFactoryType = typeof(IFileUploadDescriptorFactory);

        /// <summary>
        /// Gets or creates an instance of the <see cref="IFileUploadDescriptorFactory"/> for the identifier defined.
        /// Note that the identifier should match with the ID property for the <see cref="ComponentDescriptionAttribute"/>
        /// decorating the factory class (e.g. [<see cref="ComponentDescriptionAttribute"/>(ID = "Default")]).
        /// </summary>
        /// <param name="typeCache">The global component/dependency type cache.</param>
        /// <param name="identifier">The ID/identifier for the specific descriptor factory.</param>
        public static IFileUploadDescriptorFactory GetFileUploadDescriptorFactory(this ComponentTypeCache typeCache, string identifier = null)
        {
            typeCache.ThrowIfNull(nameof(typeCache));

            IFileUploadDescriptorFactory factory = null;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                factory = new FileUploadDescriptorFactory();
            }
            else
            {
                if (!ComponentTypeCache.Instance.DescriptorFactoryCache.TryGetValue(identifier, out factory))
                {
                    lock (ComponentTypeCache.LockObject)
                    {
                        IEnumerable<ComponentType> factoryTypes = ComponentTypeCache.Instance.Where(t => t.Type.IsAssignableTo(DescriptorFactoryType));

                        IEnumerable<ComponentType> matchingFactoryTypes = factoryTypes?.Where(f => f.Type.GetCustomAttributes<ComponentDescriptionAttribute>()
                            ?.Where(desc => string.Equals(desc.Id, identifier, StringComparison.OrdinalIgnoreCase))?.Any() == true);

                        if (matchingFactoryTypes?.Any() != true)
                        {
                            throw new DependencyException(
                                $"Missing descriptor factory. A file descriptor factory does not exist for content log structure/identifier '{identifier}'. " +
                                $"This can happen when the content log structure defined is not valid or an extensions assembly containing the descriptor factory " +
                                $"is not present or loaded into the Virtual Client runtime.");
                        }

                        if (matchingFactoryTypes?.Count() > 1)
                        {
                            throw new DependencyException(
                                $"Duplicate descriptor factory definitions. More than 1 file descriptor factory exists for content log structure/identifier '{identifier}'. " +
                                $"Each instance MUST have a unique ID.");
                        }

                        factory = (IFileUploadDescriptorFactory)Activator.CreateInstance(matchingFactoryTypes.First().Type);
                        ComponentTypeCache.Instance.DescriptorFactoryCache[identifier] = factory;
                    }
                }
            }

            return factory;
        }
    }
}
