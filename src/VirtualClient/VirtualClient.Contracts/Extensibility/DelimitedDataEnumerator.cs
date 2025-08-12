// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Provides an enumerator for delimited telemetry data content.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Implemented sufficiently")]
    public abstract class DelimitedDataEnumerator<T> : IEnumerator<T>
        where T : TelemetryDataPoint
    {
        /// <summary>
        /// A deserializer for YAML-formatted content;
        /// </summary>
        protected static readonly IDeserializer YamlDeserializer = new Deserializer();

        private static char[] csvTrimChars = new char[] { ',', '"', ' ' };
        private static string defaultDelimiter = "-----";

        private string delimitedContent;
        private DataFormat delimitedItemFormat;
        private string[] csvColumns;
        private string[] csvLines;
        private string[] items;
        private int currentItemIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelimitedDataEnumerator{T}"/> class.
        /// </summary>
        /// <param name="content">Content containing delimited data points.</param>
        /// <param name="format">The format of the data points (e.g. CSV, JSON, YAML).</param>
        protected DelimitedDataEnumerator(string content, DataFormat format)
        {
            content.ThrowIfNull(nameof(content));
            format.ThrowIfInvalid(nameof(format), f => f != DataFormat.Undefined, "The data format must be defined.");

            this.delimitedContent = content;
            this.delimitedItemFormat = format;
            this.currentItemIndex = -1;
        }

        /// <summary>
        /// Event is raised whenever an error occurs during the parsing of individual
        /// data point content.
        /// </summary>
        public event EventHandler<DataParsingEventArgs> ParsingError;

        /// <summary>
        /// The current enumerated data point.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// The delimiter for JSON and YAML content.
        /// </summary>
        public string Delimiter { get; set; } = defaultDelimiter;

        /// <summary>
        /// The current enumerated data point.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Advances the enumerator to the next data point.
        /// </summary>
        /// <returns>True if a next data point exists, false if the enumeration is complete.</returns>
        public bool MoveNext()
        {
            T nextDataPoint = null;
            bool next = false;
            if (!string.IsNullOrWhiteSpace(this.delimitedContent))
            {
                switch (this.delimitedItemFormat)
                {
                    case DataFormat.Csv:
                        nextDataPoint = this.ParseNextFromCsv(out next);
                        break;

                    case DataFormat.Json:
                        nextDataPoint = this.ParseNextFromJson(out next);
                        break;

                    case DataFormat.Yaml:
                        nextDataPoint = this.ParseNextFromYaml(out next);
                        break;
                }
            }

            this.Current = nextDataPoint;
            return next;
        }

        /// <summary>
        /// Resets the enumerator to a preliminary state.
        /// </summary>
        public void Reset()
        {
            this.csvColumns = null;
            this.csvLines = null;
            this.items = null;
            this.currentItemIndex = -1;
            this.Current = null;
        }

        /// <summary>
        /// Creates a data point from a CSV text value.
        /// </summary>
        protected virtual void ApplyTo(TelemetryDataPoint dataPoint, string[] columns, string[] values)
        {
            columns.ThrowIfNullOrEmpty(nameof(columns));
            values.ThrowIfNullOrEmpty(nameof(values));
            if (columns.Length != values.Length)
            {
                throw new ArgumentException($"Invalid CSV information. The count of columns/headers '{columns.Length}' does not match the count of the field values '{values.Length}'.");
            }

            for (int i = 0; i < values.Length; i++)
            {
                string fieldName = columns[i];
                string fieldValue = values[i];

                switch (fieldName.ToLowerInvariant())
                {
                    case "appname":
                        dataPoint.AppName = fieldValue;
                        break;

                    case "apphost":
                        dataPoint.AppHost = fieldValue;
                        break;

                    case "appversion":
                        dataPoint.AppVersion = fieldValue;
                        break;

                    case "clientid":
                        dataPoint.ClientId = fieldValue;
                        break;

                    case "executionprofile":
                        dataPoint.ExecutionProfile = fieldValue;
                        break;

                    case "executionsystem":
                        dataPoint.ExecutionSystem = fieldValue;
                        break;

                    case "experimentid":
                        dataPoint.ExperimentId = fieldValue;
                        break;

                    case "operatingsystemplatform":
                        dataPoint.OperatingSystemPlatform = fieldValue;
                        break;

                    case "operationid":
                        dataPoint.OperationId = Guid.Parse(fieldValue);
                        break;

                    case "operationparentid":
                        dataPoint.OperationParentId = Guid.Parse(fieldValue);
                        break;

                    case "platformarchitecture":
                        dataPoint.PlatformArchitecture = fieldValue;
                        break;

                    case "severitylevel":
                        dataPoint.SeverityLevel = int.Parse(fieldValue);
                        break;

                    case "tags":
                        dataPoint.Tags = fieldValue;
                        break;

                    case "timestamp":
                        dataPoint.Timestamp = DateTime.Parse(fieldValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                        break;

                    case "metadata":
                        IDictionary<string, IConvertible> metadata = TextParsingExtensions.ParseDelimitedValues(fieldValue);
                        if (metadata?.Any() == true)
                        {
                            dataPoint.Metadata = new SortedMetadataDictionary(metadata.ToDictionary(
                                entry => entry.Key, 
                                entry => entry.Value as object));
                        }

                        break;

                    case "metadata_host":
                        IDictionary<string, IConvertible> hostMetadata = TextParsingExtensions.ParseDelimitedValues(fieldValue);
                        if (hostMetadata?.Any() == true)
                        {
                            dataPoint.HostMetadata = new SortedMetadataDictionary(hostMetadata.ToDictionary(
                                entry => entry.Key, 
                                entry => entry.Value as object));
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// When implemented parses the data point from the CSV columns and row field values.
        /// </summary>
        /// <param name="columns">The CSV column/header names.</param>
        /// <param name="values">A single row of CSV field values.</param>
        /// <returns>A data point object.</returns>
        protected abstract T ParseFields(string[] columns, string[] values);

        /// <summary>
        /// When implemented parses the data point from a JSON-formatted description.
        /// </summary>
        /// <param name="jsonItem">A single data point definition in JSON format.</param>
        /// <returns>A data point object.</returns>
        protected abstract T ParseJson(string jsonItem);

        /// <summary>
        /// When implemented parses the data point from a YAML-formatted description.
        /// </summary>
        /// <param name="yamlItem">A single data point definition in YAML format.</param>
        /// <returns>A data point object.</returns>
        protected abstract T ParseYaml(string yamlItem);

        private T ParseNextFromCsv(out bool next)
        {
            next = false;
            T dataPoint = null;
            string nextLine = null;

            try
            {
                if (this.csvLines == null)
                {
                    this.csvLines = Regex.Split(this.delimitedContent, @"\r\n|\n");
                }

                if (this.csvLines?.Any() == true && this.csvLines.Length >= 2)
                {
                    if (this.csvColumns == null)
                    {
                        this.csvColumns = this.csvLines[0].Split(',', StringSplitOptions.TrimEntries);

                        if (this.csvColumns?.Any() != true)
                        {
                            throw new SchemaException($"Invalid CSV content. The content does not contain a valid set of column/header definitions in the first line of text.");
                        }
                    }

                    this.currentItemIndex++;
                    if (this.currentItemIndex < this.csvLines.Length - 1)
                    {
                        try
                        {
                            nextLine = this.csvLines.Skip(1).ElementAt(this.currentItemIndex);
                            MatchCollection matches = Regex.Matches(nextLine, "(?:^|,)(?:\"([^\"]*)\"|([^\",\\n\\r]*))");
                            if (matches?.Any() == true)
                            {
                                string[] fields = matches.Select(m => m.Value.Trim(csvTrimChars)).ToArray();

                                if (this.csvColumns.Length != fields.Length)
                                {
                                    throw new SchemaException($"Invalid CSV content. The count of the columns/headers does match the count of the field values at row index {this.currentItemIndex}.");
                                }

                                dataPoint = this.ParseFields(this.csvColumns, fields);
                            }
                        }
                        finally
                        {
                            next = true;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.ParsingError?.Invoke(this, new DataParsingEventArgs(nextLine, this.currentItemIndex, exc));
            }

            return dataPoint;
        }

        private T ParseNextFromJson(out bool next)
        {
            next = false;
            T dataPoint = null;
            string nextItem = null;

            try
            {
                if (this.items == null)
                {
                    this.items = this.delimitedContent.Split(this.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                if (this.items?.Any() == true)
                {
                    this.currentItemIndex++;
                    if (this.currentItemIndex < this.items.Length)
                    {
                        try
                        {
                            nextItem = this.items[this.currentItemIndex];
                            dataPoint = this.ParseJson(nextItem.Trim());

                            if (dataPoint == null)
                            {
                                throw new Newtonsoft.Json.JsonSerializationException($"Invalid JSON content. The item at index '{this.currentItemIndex}' cannot be parsed as valid JSON.");
                            }
                        }
                        finally
                        {
                            next = true;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.ParsingError?.Invoke(this, new DataParsingEventArgs(nextItem, this.currentItemIndex, exc));
            }

            return dataPoint;
        }

        private T ParseNextFromYaml(out bool next)
        {
            next = false;
            T dataPoint = null;
            string nextItem = null;

            try
            {
                if (this.items == null)
                {
                    this.items = this.delimitedContent.Split(this.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                if (this.items?.Any() == true)
                {
                    this.currentItemIndex++;
                    if (this.currentItemIndex < this.items.Length)
                    {
                        try
                        {
                            nextItem = this.items[this.currentItemIndex];
                            dataPoint = this.ParseYaml(nextItem.Trim());
                        }
                        finally
                        {
                            next = true;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.ParsingError?.Invoke(this, new DataParsingEventArgs(nextItem, this.currentItemIndex, exc));
            }

            return dataPoint;
        }
    }
}
