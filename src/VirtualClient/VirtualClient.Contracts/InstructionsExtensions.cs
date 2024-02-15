// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;

    /// <summary>
    /// Extensions for <see cref="Instructions"/> class instances/objects.
    /// </summary>
    public static class InstructionsExtensions
    {
        /// <summary>
        /// Gets or sets the "requestId" property from the instructions.
        /// </summary>
        public static Guid? RequestId(this Instructions instructions, Guid? requestId = null)
        {
            const string propertyName = "requestId";

            Guid? id = null;
            if (requestId != null)
            {
                instructions.Properties[propertyName] = requestId.ToString();
                id = requestId;
            }
            else if (instructions.Properties.TryGetValue(propertyName, out IConvertible value))
            {
                id = Guid.Parse(value.ToString());
            }

            return id;
        }
    }
}
