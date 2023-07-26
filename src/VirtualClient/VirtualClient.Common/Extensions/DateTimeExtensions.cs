// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;

    /// <summary>
    /// Extension method for date/time objects.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Adds a timespan to time 1 handling any <see cref="ArgumentOutOfRangeException"/> errors that
        /// might happen. In the event an exception happens, the method returns DateTime.MaxValue.
        /// </summary>
        /// <param name="time1">The time to which the timespan is added.</param>
        /// <param name="timespan">The timespan to add to time 1.</param>
        public static DateTime SafeAdd(this DateTime time1, TimeSpan timespan)
        {
            DateTime time = DateTime.MaxValue;

            try
            {
                time = time1.Add(timespan);
            }
            catch
            {
            }

            return time;
        }

        /// <summary>
        /// Subtracts time 2 from time 1 handling any <see cref="ArgumentOutOfRangeException"/> errors that
        /// might happen. In the event an exception happens, the method returns Timespan.Zero.
        /// </summary>
        /// <param name="time1">The time from which the time 2 is subtracted.</param>
        /// <param name="time2">The time to subtract from time 1.</param>
        public static TimeSpan SafeSubtract(this DateTime time1, DateTime time2)
        {
            TimeSpan timeRemaining = TimeSpan.Zero;

            try
            {
                timeRemaining = time1.Subtract(time2);
            }
            catch
            {
            }

            return timeRemaining;
        }
    }
}
