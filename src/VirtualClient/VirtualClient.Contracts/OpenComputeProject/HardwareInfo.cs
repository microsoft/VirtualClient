// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.OpenComputeProject
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// SoftwareType
    /// </summary>
    public enum SoftwareType
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        UNSPECIFIED,

        /// <summary>
        /// Firmware
        /// </summary>
        FIRMWARE,

        /// <summary>
        /// System
        /// </summary>
        SYSTEM,

        /// <summary>
        /// Application
        /// </summary>
        APPLICATION
    }

    /// <summary>
    /// Dut Info class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/dut_info.json
    /// </summary>
    public class DutInfo
    {
        /// <summary>
        /// DUT Info Id
        /// </summary>
        [JsonProperty("dutInfoId", Required = Required.Always)]
        public string DutInfoId { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Platform Infos
        /// </summary>
        [JsonProperty("platformInfos")]
        public List<PlatformInfo> PlatformInfos { get; set; }

        /// <summary>
        /// Software Infos
        /// </summary>
        [JsonProperty("softwareInfos")]
        public List<SoftwareInfo> SoftwareInfos { get; set; }

        /// <summary>
        /// Hardware Infos
        /// </summary>
        [JsonProperty("hardwareInfos")]
        public List<HardwareInfo> HardwareInfos { get; set; }

        /// <summary>
        /// Metadata
        /// </summary>
        [JsonProperty("metadata")]
        public object Metadata { get; set; }
    }

    /// <summary>
    /// Platform Info
    /// </summary>
    public class PlatformInfo
    {
        /// <summary>
        /// Info
        /// </summary>
        [JsonProperty("info", Required = Required.Always)]
        public string Info { get; set; }
    }

    /// <summary>
    /// Software Info
    /// </summary>
    public class SoftwareInfo
    {
        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Revision
        /// </summary>
        [JsonProperty("revision")]
        public string Revision { get; set; }

        /// <summary>
        /// Software Type
        /// </summary>
        [JsonProperty("softwareType")]
        public string SoftwareType { get; set; }

        /// <summary>
        /// Software Info Id
        /// </summary>
        [JsonProperty("softwareInfoId", Required = Required.Always)]
        public string SoftwareInfoId { get; set; }

        /// <summary>
        /// Computer System
        /// </summary>
        [JsonProperty("computerSystem")]
        public string ComputerSystem { get; set; }
    }

    /// <summary>
    /// Hardware info
    /// </summary>
    public class HardwareInfo
    {
        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Revision
        /// </summary>
        [JsonProperty("revision")]
        public string Revision { get; set; }

        /// <summary>
        /// Location
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// Hardware Info Id
        /// </summary>
        [JsonProperty("hardwareInfoId", Required = Required.Always)]
        public string HardwareInfoId { get; set; }

        /// <summary>
        /// Serial Number
        /// </summary>
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Part Number
        /// </summary>
        [JsonProperty("partNumber")]
        public string PartNumber { get; set; }

        /// <summary>
        /// Part Type
        /// </summary>
        [JsonProperty("partType")]
        public string PartType { get; set; }

        /// <summary>
        /// Manufacturer
        /// </summary>
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        /// <summary>
        /// Manufacturer Part Number
        /// </summary>
        [JsonProperty("manufacturerPartNumber")]
        public string ManufacturerPartNumber { get; set; }

        /// <summary>
        /// OData Id
        /// </summary>
        [JsonProperty("odataId")]
        public string ODataId { get; set; }

        /// <summary>
        /// Computer System
        /// </summary>
        [JsonProperty("computerSystem")]
        public string ComputerSystem { get; set; }

        /// <summary>
        /// Manager
        /// </summary>
        [JsonProperty("manager")]
        public string Manager { get; set; }
    }
}