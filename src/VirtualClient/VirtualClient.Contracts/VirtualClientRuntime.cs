// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Runtime Resources, settings and events that are global the Virtual Client application.
    /// </summary>
    public static class VirtualClientRuntime
    {
        /// <summary>
        /// Application level lock object.
        /// </summary>
        public static readonly object LockObject = new object();

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
        /// The global application cancellation token source. This can be used to instruct the
        /// Virtual Client to exit.
        /// </summary>
        public static CancellationTokenSource CancellationSource { get; } = new CancellationTokenSource();

        /// <summary>
        /// A set of one or more tasks (cleanup) registered to execute before the application
        /// exits completely. The dictionary key can be used to determine if a particular task exists
        /// in the set of not.
        /// </summary>
        public static List<Action_> CleanupTasks { get; } = new List<Action_>();

        /// <summary>
        /// The command line arguments provided to the Virtual Client application.
        /// </summary>
        public static string[] CommandLineArguments { get; internal set; }

        /// <summary>
        /// The current experiment ID for the application.
        /// </summary>
        public static string ExperimentId { get; internal set; }

        /// <summary>
        /// The current platform-specifics for the application.
        /// </summary>
        public static PlatformSpecifics PlatformSpecifics { get; internal set; }

        /// <summary>
        /// The name of the Virtual Client application/module.
        /// </summary>
        public static string ExecutableName { get; internal set; } = Process.GetCurrentProcess().MainModule.FileName;

        /// <summary>
        /// A set of one or more tasks (exit) registered to execute before the application
        /// exits completely. The dictionary key can be used to determine if a particular task exists
        /// in the set of not.
        /// </summary>
        public static List<Action_> ExitTasks { get; } = new List<Action_>();

        /// <summary>
        /// Returns true/false whether eventing is online for the instance of Virtual Client. By default,
        /// eventing is turned off.
        /// </summary>
        public static bool IsApiOnline { get; private set; }

        /// <summary>
        /// Set to true to request a system reboot of the system..
        /// </summary>
        public static bool IsRebootRequested { get; set; }

        /// <summary>
        /// Cleans up any tracked resources.
        /// </summary>
        public static void OnCleanup()
        {
            if (VirtualClientRuntime.CleanupTasks.Any())
            {
                lock (VirtualClientRuntime.LockObject)
                {
                    foreach (var entry in VirtualClientRuntime.CleanupTasks)
                    {
                        try
                        {
                            entry.Invoke();
                        }
                        catch
                        {
                            // Best effort here.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up any tracked resources.
        /// </summary>
        public static void OnExiting()
        {
            if (VirtualClientRuntime.ExitTasks.Any())
            {
                lock (VirtualClientRuntime.LockObject)
                {
                    foreach (var entry in VirtualClientRuntime.ExitTasks)
                    {
                        try
                        {
                            entry.Invoke();
                        }
                        catch
                        {
                            // Best effort here.
                        }
                    }
                }
            }
        }

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
            Delegate[] subscribers = VirtualClientRuntime.ReceiveInstructions?.GetInvocationList();
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
            Delegate[] subscribers = VirtualClientRuntime.SendReceiveInstructions?.GetInvocationList();
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
            lock (VirtualClientRuntime.LockObject)
            {
                VirtualClientRuntime.IsApiOnline = online;
            }
        }
    }
}
