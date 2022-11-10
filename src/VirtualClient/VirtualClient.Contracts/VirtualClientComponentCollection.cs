// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Represents a set/collection of <see cref="VirtualClientComponent"/> instances. This enables support for
    /// more advanced execution scenarios (e.g. parallel execution).
    /// </summary>
    public abstract class VirtualClientComponentCollection : VirtualClientComponent, ICollection<VirtualClientComponent>
    {
        private ICollection<VirtualClientComponent> components;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientComponentCollection"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        protected VirtualClientComponentCollection(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.components = new Collection<VirtualClientComponent>();
        }

        /// <summary>
        /// The count of the components in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                return this.components.Count;
            }
        }

        /// <summary>
        /// Returns true/false whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a component to the collection.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void Add(VirtualClientComponent component)
        {
            this.components.Add(component);
        }

        /// <summary>
        /// Clears all components from the collection.
        /// </summary>
        public void Clear()
        {
            this.components.Clear();
        }

        /// <summary>
        /// Returns true/false whether the collection contains the component supplied.
        /// </summary>
        /// <param name="component">The component to verify as existing in the collection.</param>
        public bool Contains(VirtualClientComponent component)
        {
            return this.components.Contains(component);
        }

        /// <summary>
        /// Copies the components in the collection to in the array starting at the index defined.
        /// </summary>
        /// <param name="array">The array of to which the components should be copied.</param>
        /// <param name="arrayIndex">The index within the array to begin copying components from the collection.</param>
        public void CopyTo(VirtualClientComponent[] array, int arrayIndex)
        {
            this.components.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator for the collection of components.
        /// </summary>
        public IEnumerator<VirtualClientComponent> GetEnumerator()
        {
            return this.components.GetEnumerator();
        }

        /// <summary>
        /// Removes the component from the collection.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        /// <returns>True if the component was successfully removed or false if not.</returns>
        public bool Remove(VirtualClientComponent component)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
