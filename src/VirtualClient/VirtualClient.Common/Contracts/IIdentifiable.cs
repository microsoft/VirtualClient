// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    /// <summary>
    /// Defines properties that can be used to identify a VirtualClient.Common
    /// contract/entity.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the ID of the contract/entity.
        /// </summary>
        string Id { get; }
    }
}