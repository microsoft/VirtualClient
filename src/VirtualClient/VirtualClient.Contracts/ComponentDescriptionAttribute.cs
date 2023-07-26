// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;

    /// <summary>
    /// Attribute is used to associate an identifier and description with a Virtual Client
    /// component or dependency class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ComponentDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentDescriptionAttribute"/>.
        /// </summary>
        public ComponentDescriptionAttribute()
            : base()
        {
        }

        /// <summary>
        /// An ID/name associated with the the component of dependency class.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// A description of the component or dependency class.
        /// </summary>
        public string Description { get; set; }
    }
}
