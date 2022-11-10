// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Events to which local components can subscribe to enable requests/instructions to 
    /// be passed to them in a push-based eventing model.
    /// </summary>
    public static class VirtualClientEventing
    {
        private static readonly object LockObject = new object();

        /// <summary>
        /// Event is fired anytime special instructions are received by a component within a Virtual
        /// Client instance or from a remote instance.
        /// </summary>
        public static event EventHandler<JObject> ReceiveInstructions;

        /// <summary>
        /// Event is fired anytime special instructions are received by a component within a Virtual
        /// Client instance or from a remote instance. 
        /// </summary>
        /// <remarks>
        /// Backwards Compatibility:
        /// Note that we are currently working to disperse the usage of this event into the codebase vs. 
        /// the event above. Eventually, there will be this event handler only.
        /// </remarks>
        public static event EventHandler<InstructionsEventArgs> SendReceiveInstructions;

        /// <summary>
        /// Returns true/false whether eventing is online for the instance of Virtual Client. By default,
        /// eventing is turned off.
        /// </summary>
        public static bool IsApiOnline { get; private set; }

        /// <summary>
        /// Invokes the <see cref="ReceiveInstructions"/> event to notify subscribers
        /// that instructions were received.
        /// </summary>
        /// <param name="sender">The component invoking the event.</param>
        /// <param name="instructions">The instructions received.</param>
        public static void OnReceiveInstructions(object sender, JObject instructions)
        {
            // .NET Events are multicast, but they are no parallelized. The logic for each subscriber will
            // be executed sequentially. We want a parallel multicast functionality in the Virtual Client.
            Delegate[] subscribers = VirtualClientEventing.ReceiveInstructions?.GetInvocationList();
            if (subscribers?.Any() == true)
            {
                foreach (Delegate subscriber in subscribers)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            subscriber.DynamicInvoke(sender, instructions);
                        }
                        catch
                        {
                            // Individual components are expected to handle failures. The caller invoking the delegates should
                            // not crash due to issues within the subscriber logic.
                        }
                    }).ContinueWith(task => task.Dispose());
                }
            }
        }

        /// <summary>
        /// Invokes the <see cref="SendReceiveInstructions"/> event to notify subscribers
        /// of on-demand instructions.
        /// </summary>
        /// <param name="sender">The component invoking the event.</param>
        /// <param name="args">Provides the instructions to process.</param>
        public static void OnSendReceiveInstructions(object sender, InstructionsEventArgs args)
        {
            // .NET Events are multicast, but they are no parallelized. The logic for each subscriber will
            // be executed sequentially. We want a parallel multicast functionality in the Virtual Client.
            Delegate[] subscribers = VirtualClientEventing.SendReceiveInstructions?.GetInvocationList();
            if (subscribers?.Any() == true)
            {
                foreach (Delegate subscriber in subscribers)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            subscriber.DynamicInvoke(sender, args);
                        }
                        catch
                        {
                            // Individual components are expected to handle failures. The caller invoking the delegates should
                            // not crash due to issues within the subscriber logic.
                        }
                    }).ContinueWith(task => task.Dispose());
                }
            }
        }

        /// <summary>
        /// Turns eventing on or off on the self-hosted eventing REST API. This allows
        /// components to ensure proper initialization before allowing events to be propagated
        /// via the API service.
        /// </summary>
        /// <param name="online">True to set API eventing online. False to turn it off.</param>
        public static void SetEventingApiOnline(bool online)
        {
            lock (VirtualClientEventing.LockObject)
            {
                VirtualClientEventing.IsApiOnline = online;
            }
        }
    }
}
