// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    /// <summary>
    /// A blip in the machines connectivity to the network.
    /// </summary>
    public class NetworkBlip
    {
        /// <summary>
        /// Initializes an instance of <see cref="NetworkBlip"/>
        /// </summary>
        /// <param name="droppedAttempts">The number of connection attempts before successfully reconnecting.</param>
        /// <param name="duration">The duration of the loss of connectivity in milliseconds.</param>
        public NetworkBlip(int droppedAttempts, long duration)
        {
            this.DroppedAttempts = droppedAttempts;
            this.Duration = duration;
        }

        /// <summary>
        /// The number of connection attempts before successfully reconnecting.
        /// </summary>
        public int DroppedAttempts { get; }

        /// <summary>
        /// The duration of the loss of connectivity in milliseconds.
        /// </summary>
        public long Duration { get; }
    }
}
