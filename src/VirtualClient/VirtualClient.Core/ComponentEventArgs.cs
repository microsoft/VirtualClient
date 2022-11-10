// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Event arguments for Virtual Client component operations.
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentEventArgs"/> class.
        /// </summary>
        /// <param name="component">The component associated with the event.</param>
        public ComponentEventArgs(VirtualClientComponent component)
        {
            component.ThrowIfNull(nameof(component));
            this.Component = component;
        }

        /// <summary>
        /// The component associated with the event.
        /// </summary>
        public VirtualClientComponent Component { get; }
    }
}
