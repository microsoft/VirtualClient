// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents different types of runtime timeout concepts in the Virtual Client.
    /// <list type="bullet">
    /// <item>
    /// <description>No Timeout - User does not supply a timeout at all on the command line. The application will run until explicity terminated.</description>
    /// </item>
    /// <item>
    /// <description>Explicit Timeout - User supplies a timeout on the command line (e.g. --timeout=1440). The application will exit at that specific time.</description>
    /// </item>
    /// <item>
    /// <description>
    /// Explicit Timeout/Deterministic - User supplies a timeout on the command line with a 'deterministic' hint (e.g. --timeout=1440/deterministic). The application will exit at that 
    /// specific time but only after allowing the current running action to complete.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Explicit Timeout/Deterministic All - User supplies a timeout on the command line with a 'deterministic*' hint (e.g. --timeout=1440/deterministic*). The application will exit at that 
    /// specific time but only after allowing the current iteration of the profile actions (all of them) to complete.
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    public class ProfileTiming
    {
        private bool isTimedOut;
        private int currentIterationCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileTiming"/> class set to timeout
        /// at a specific time.
        /// </summary>
        /// <param name="duration">The duration.</param>
        public ProfileTiming(TimeSpan duration)
        {
            this.Timeout = DateTime.UtcNow.Add(duration);
            this.Duration = duration;

            if (this.Duration < TimeSpan.Zero)
            {
                this.Duration = TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileTiming"/> class set to timeout
        /// when the profile has executed all actions the number of times/iterations defined.
        /// </summary>
        /// <param name="profileIterations">The number of iterations to run the profile actions before exiting.</param>
        public ProfileTiming(int profileIterations)
        {
            profileIterations.ThrowIfInvalid(
                nameof(profileIterations),
                iterations => iterations > 0 || iterations == -1,
                $"Invalid profile iterations value. The value provided '{profileIterations}' must be greater than zero, or equals to -1.");

            this.ProfileIterations = profileIterations;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileTiming"/> class set to timeout at a specific
        /// time but only with a deterministic outcome.
        /// </summary>
        /// <param name="duration">The duration.</param>
        /// <param name="levelOfDeterminism">
        /// Defines the level of determinism for the completion of actions before allowing
        /// an explicit timeout to occur.
        /// </param>
        public ProfileTiming(TimeSpan duration, DeterminismScope levelOfDeterminism)
            : this(duration)
        {
            levelOfDeterminism.ThrowIfInvalid(
                nameof(levelOfDeterminism),
                level => level != DeterminismScope.Undefined,
                $"Invalid level of determinism. The value provided '{levelOfDeterminism}' does not represent an actionable level of determinism.");

            this.LevelOfDeterminism = levelOfDeterminism;
        }

        private ProfileTiming()
        {
            // Run forever
        }

        /// <summary>
        /// Defines the duration of time the application will run if an explicit timeout
        /// is provided.
        /// </summary>
        public TimeSpan? Duration { get; }

        /// <summary>
        /// An explicit timeout.
        /// </summary>
        public DateTime? Timeout { get; }

        /// <summary>
        /// Defines the level of determinism for the completion of actions before allowing
        /// an explicit timeout to occur.
        /// </summary>
        public DeterminismScope LevelOfDeterminism { get; }

        /// <summary>
        /// True/false whether the profile runtime operations are timed out. This indicates
        /// when the application will exit.
        /// </summary>
        public bool IsTimedOut
        {
            get
            {
                return this.isTimedOut;
            }
        }

        /// <summary>
        /// An explicit number of iterations for running the set of actions defined
        /// in the profile (i.e. rounds of execution).
        /// </summary>
        public int? ProfileIterations { get; }

        /// <summary>
        /// Creates an instance of the <see cref="ProfileTiming"/> set to run the profile
        /// forever or until the application is explicitly stopped.
        /// </summary>
        public static ProfileTiming Forever()
        {
            return new ProfileTiming();
        }

        /// <summary>
        /// Creates an instance of the <see cref="ProfileTiming"/> set to run the profile
        /// a specific number of iterations or until the application is explicitly stopped.
        /// </summary>
        public static ProfileTiming Iterations(int iterations)
        {
            return new ProfileTiming(profileIterations: iterations);
        }

        /// <summary>
        /// Creates an instance of the <see cref="ProfileTiming"/> set to run the profile
        /// a single iteration or until the application is explicitly stopped.
        /// </summary>
        public static ProfileTiming OneIteration()
        {
            return new ProfileTiming(profileIterations: 1);
        }

        /// <summary>
        /// Creates a background task that monitors the operations and state of the profile execution
        /// to determine when an operations/application timeout is reached.
        /// </summary>
        public Task MonitorTimeoutAsync(ProfileExecutor profileExecutor, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    // We support 4 distinct scenarios for defining a timeout on the command line.
                    // 1) No timeout. Application runs until explicitly stopped. This is indicated by
                    //    not supplying a --timeout on the command line.
                    //
                    // 2) Explicit timeout. The application will timeout at the time defined.
                    //
                    // 3) Explicit timeout/deterministic. The application will timeout at the time defined but ONLY after
                    //    the current profile action has completed.
                    //
                    // 4) Explicit timeout/deterministic/*. The application will timeout at the time defined but ONLY after
                    //    the current round of all profile actions have completed.

                    if (this.ProfileIterations != null || this.LevelOfDeterminism == DeterminismScope.AllActions)
                    {
                        try
                        {
                            // Timeout when the expected number of profile iterations finishes or when
                            // an explicit timeout is hit and all profile actions have completed.
                            profileExecutor.IterationEnd += this.OnIterationEnd;
                            await this.WaitForTimeoutAsync(cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            profileExecutor.IterationEnd -= this.OnIterationEnd;
                        }
                    }
                    else if (this.LevelOfDeterminism == DeterminismScope.IndividualAction)
                    {
                        try
                        {
                            // Timeout when an explicit timeout is hit and the current action is completed.
                            profileExecutor.ActionEnd += this.OnActionEnd;
                            await this.WaitForTimeoutAsync(cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            profileExecutor.IterationEnd -= this.OnActionEnd;
                        }
                    }
                    else if (this.Timeout != null)
                    {
                        // Timeout when an explicit timeout is hit.
                        await this.WaitForTimeoutAsync(this.Timeout.Value, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // No timeout. We simply keep the tracking task alive until the application is
                        // explicitly stopped (e.g. Ctrl-C).
                        await this.WaitForTimeoutAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected sometimes when the application is explicitly cancelled (e.g. Ctrl-C).
                }
            });
        }

        private void OnActionEnd(object sender, EventArgs args)
        {
            if (DateTime.UtcNow >= this.Timeout.Value)
            {
                this.isTimedOut = true;
            }
        }

        private void OnIterationEnd(object sender, EventArgs args)
        {
            this.currentIterationCount++;
            if (this.ProfileIterations != null)
            {
                if (this.currentIterationCount >= this.ProfileIterations.Value)
                {
                    this.isTimedOut = true;
                }
            }
            else if (DateTime.UtcNow >= this.Timeout.Value)
            {
                this.isTimedOut = true;
            }
        }

        private async Task WaitForTimeoutAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !this.isTimedOut)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WaitForTimeoutAsync(DateTime timeout, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= timeout)
                {
                    this.isTimedOut = true;
                    break;
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
