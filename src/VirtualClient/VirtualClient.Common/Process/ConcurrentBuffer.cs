// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System.Text;

    /// <summary>
    /// A minimal thread-safe implementation of a <see cref="StringBuilder"/> for use
    /// as a process output buffer.
    /// </summary>
    /// <remarks>
    /// This addresses a race condition in the <see cref="StringBuilder"/> class as noted in the following
    /// link: https://github.com/xunit/xunit/issues/164
    /// </remarks>
    public class ConcurrentBuffer
    {
        private StringBuilder underlyingStringBuilder;

        /// <summary>
        /// Intializes a new instance of the <see cref="ConcurrentBuffer"/> class.
        /// </summary>
        public ConcurrentBuffer()
        {
            this.underlyingStringBuilder = new StringBuilder();
        }

        /// <summary>
        /// Intializes a new instance of the <see cref="ConcurrentBuffer"/> class.
        /// </summary>
        public ConcurrentBuffer(StringBuilder stringBuilder)
        {
            this.underlyingStringBuilder = stringBuilder;
        }

        /// <summary>
        /// The capacity defined for the output buffer.
        /// </summary>
        public int Capacity
        {
            get
            {
                lock (this.underlyingStringBuilder)
                {
                    return this.underlyingStringBuilder.Capacity;
                }
            }
        }

        /// <summary>
        /// The length of the output buffer (character length).
        /// </summary>
        public int Length
        {
            get
            {
                lock (this.underlyingStringBuilder)
                {
                    return this.underlyingStringBuilder.Length;
                }
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(char value, int repeatCount)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value, repeatCount);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(StringBuilder value)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(string value, int startIndex, int count)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value, startIndex, count);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(string value)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(StringBuilder value, int startIndex, int count)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value, startIndex, count);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(object value)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(char[] value)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value);
                return this;
            }
        }

        /// <summary>
        /// Appends the value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer Append(char[] value, int startIndex, int charCount)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Append(value, startIndex, charCount);
                return this;
            }
        }

        /// <summary>
        /// Appends the formatted value(s) to the output buffer.
        /// </summary>
        public ConcurrentBuffer AppendFormat(string format, params object[] args)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.AppendFormat(format, args);
                return this;
            }
        }

        /// <summary>
        /// Appends a line terminator to the output buffer.
        /// </summary>
        /// <returns></returns>
        public ConcurrentBuffer AppendLine()
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.AppendLine();
                return this;
            }
        }

        /// <summary>
        /// Appends the value to the output buffer followed by a line terminator.
        /// </summary>
        public ConcurrentBuffer AppendLine(string value)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.AppendLine(value);
                return this;
            }
        }

        /// <summary>
        /// Clears all characters from the output buffer.
        /// </summary>
        public ConcurrentBuffer Clear()
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Clear();
                return this;
            }
        }

        /// <summary>
        /// Inserts the value into the output buffer.
        /// </summary>
        /// <param name="index">The index at which the value should be inserted.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="count">The number of times to insert/repeat the value.</param>
        public ConcurrentBuffer Insert(int index, string value, int count)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Insert(index, value, count);
                return this;
            }
        }

        /// <summary>
        /// Inserts the value into the output buffer.
        /// </summary>
        /// <param name="index">The index at which the value should be inserted.</param>
        /// <param name="value">The value to insert.</param>
        public ConcurrentBuffer Insert(int index, string value)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Insert(index, value);
                return this;
            }
        }

        /// <summary>
        /// Replaces instances of the old/original value in the output buffer with the
        /// new value.
        /// </summary>
        /// <param name="oldValue">The old/original value.</param>
        /// <param name="newValue">The new/replacement value.</param>
        public ConcurrentBuffer Replace(string oldValue, string newValue)
        {
            lock (this.underlyingStringBuilder)
            {
                this.underlyingStringBuilder.Replace(oldValue, newValue);
                return this;
            }
        }

        /// <summary>
        /// Returns the entire string content of the output buffer.
        /// </summary>
        public override string ToString()
        {
            lock (this.underlyingStringBuilder)
            {
                return this.underlyingStringBuilder.ToString();
            }
        }

        /// <summary>
        /// Returns the length of string content in the output buffer
        /// from the start index.
        /// </summary>
        public string ToString(int startIndex, int length)
        {
            lock (this.underlyingStringBuilder)
            {
                return this.underlyingStringBuilder.ToString(startIndex, length);
            }
        }
    }
}
