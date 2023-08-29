// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// MeasurementSeriesEnd class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/measurement_series_end.json
    /// </summary>
    public class MeasurementSeriesEnd
    {
        /// <summary>
        /// Measurement Series Id associated with the measurement series end.
        /// </summary>
        [JsonProperty("measurementSeriesId", Required = Required.Always)]
        public string MeasurementSeriesId { get; set; }

        /// <summary>
        /// Total count of measurements in the series.
        /// </summary>
        [JsonProperty("totalCount", Required = Required.Always)]
        public int TotalCount { get; set; }
    }
}