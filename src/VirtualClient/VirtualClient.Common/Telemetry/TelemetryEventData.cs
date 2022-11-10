// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Text;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents telemetry event data serialized as JSON
    /// </summary>
    [EventData]
    public class TelemetryEventData
    {
        /// <summary>
        /// The size of the ETW event header
        /// </summary>
        /// <remarks>
        /// We were unable to find an actual struct whose properties sized
        /// up to this number.  However, empirical testing found that this was
        /// the size of the event header so we are very simply using the constant
        /// here in lieu of a Marshal.SizeOf(struct) approach.
        /// </remarks>
        public const int EtwEventHeaderSizeInBytes = 248;

        /// <summary>
        /// The maximum ETW event size (Payload + Headers)
        /// </summary>
        /// <remarks>
        /// https://msdn.microsoft.com/en-us/library/bb382834.aspx
        /// </remarks>
        public const int MaxEtwEventSizeInBytes = 65536;

        private const int SizeOfGuidInBytes = 16;
        private const int SizeOfInt64InBytes = sizeof(long);
        private const int SizeOfInt32InBytes = sizeof(int);
        private const int SizeOfEventPayloadModificationEnum = sizeof(ContextModifications);

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryEventData"/> class
        /// </summary>
        public TelemetryEventData()
        {
            this.DurationMs = -1;
            this.PayloadModifications = ContextModifications.None;
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryEventData"/> class
        /// with context.
        /// </summary>
        /// <param name="context">The event context message</param>
        public TelemetryEventData(string context)
        {
            this.DurationMs = -1;
            this.Context = context;
            this.PayloadModifications = ContextModifications.None;
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// The maximum size of the event payload (in bytes).  This is the maximum size the <see cref="TelemetryEventData"/>
        /// object can be (minus headers) for ETW (i.e. MaxEtwEventSizeInBytes - EtwEventHeaderSizeInBytes).
        /// </summary>
        public static int MaxEtwEventPayloadSizeInBytes { get; } = TelemetryEventData.MaxEtwEventSizeInBytes - TelemetryEventData.EtwEventHeaderSizeInBytes;

        /// <summary>
        /// Gets or sets the JSON serialized event context data
        /// </summary>
        public string Context
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the duration of the event, in milliseconds.
        /// </summary>
        public long DurationMs
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the event size in bytes (including the header and the payload).  ETW
        /// events must be under 64K bytes in size.
        /// </summary>
        public int EventSize
        {
            get
            {
                return TelemetryEventData.EtwEventHeaderSizeInBytes + this.GetPayloadSizeInBytes();
            }
        }

        /// <summary>
        /// Gets the type of modifications made to the event payload if any (ex:  payload exceeded size limitations
        /// and was constrained).
        /// </summary>
        public ContextModifications PayloadModifications
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the event timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the transaction ID used to distinguish a given
        /// set of activity steps from another identical set of steps.
        /// </summary>
        public Guid TransactionId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the client/user in the context of the telemetry event.
        /// </summary>
        public string User
        {
            get;
            set;
        }

        /// <summary>
        /// Returns true if the size of the event data exceeds the maximum
        /// ETW restrictions of 64K (this includes the size of the event header).
        /// </summary>
        /// <param name="eventData">The event data object to check against size restrictions.</param>
        /// <returns>
        /// True if the event data object will exceed ETW event size restrictions, False otherwise.
        /// </returns>
        public static bool EventSizeExceedsEtwLimit(TelemetryEventData eventData)
        {
            eventData.ThrowIfNull(nameof(eventData));
            return eventData.EventSize > TelemetryEventData.MaxEtwEventSizeInBytes;
        }

        /// <summary>
        /// Returns a deep clone of the current object
        /// </summary>
        /// <returns>
        /// An exact clone of the current object
        /// </returns>
        public TelemetryEventData Clone()
        {
            return new TelemetryEventData
            {
                Context = this.Context,
                DurationMs = this.DurationMs,
                PayloadModifications = this.PayloadModifications,
                TransactionId = this.TransactionId,
                User = this.User,
            };
        }

        /// <summary>
        /// Returns a clone of the current object having the minimal set of properties
        /// required to preserve the identity of the event.
        /// </summary>
        /// <param name="contextModificationLogic">
        /// A function/delegate to invoke against the Context property of the object to change the size
        /// of the text (i.e. to minimize the text).
        /// </param>
        /// <returns>
        /// An minimal clone of the current object
        /// </returns>
        public TelemetryEventData Constrain(Func<string> contextModificationLogic)
        {
            string minimizedContext = null;
            if (contextModificationLogic != null)
            {
                minimizedContext = contextModificationLogic.Invoke();
            }

            return new TelemetryEventData
            {
                Context = minimizedContext,
                DurationMs = this.DurationMs,
                PayloadModifications = this.PayloadModifications,
                TransactionId = this.TransactionId,
                User = this.User,
            };
        }

        /// <summary>
        /// Returns the size (in bytes) of all data associated with the telemetry event.  This
        /// size must be less than the 64K bytes maximum limit for EventSource events less the
        /// size of the event header.
        /// </summary>
        /// <returns>
        /// The size in bytes of the event payload.  This does not include the size of the header.
        /// (note:  The total event size includes the size of the header plus the size of the payload)
        /// </returns>
        public int GetPayloadSizeInBytes(bool withContext = true)
        {
            // .               Transaction ID                         EventSize                               DurationMs                               PayloadModifications
            int payloadSize = TelemetryEventData.SizeOfGuidInBytes + TelemetryEventData.SizeOfInt32InBytes + TelemetryEventData.SizeOfInt64InBytes + TelemetryEventData.SizeOfEventPayloadModificationEnum;

            if (this.User != null)
            {
                // # characters * 2 bytes per character (Unicode)
                payloadSize += Encoding.Unicode.GetByteCount(this.User);
            }

            if (withContext && this.Context != null)
            {
                payloadSize += Encoding.Unicode.GetByteCount(this.Context);
            }

            return payloadSize;
        }
    }
}