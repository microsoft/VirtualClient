// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    ///  Parser for Linux lshw disk output.
    /// </summary>
    public class LshwDiskParser : TextParser<IList<Disk>>
    {
        // AWS actually has a clean guide on this: https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/device_naming.html
        // SCSI disk will always start with /dev/sd
        // NVME disk will always start with /dev/nvme
        private static readonly List<string> DevicePathPrefixs = new List<string>
        {
            "/dev/hd",
            "/dev/sd",
            "/dev/nvme",
            "/dev/xvd"
        };

        /// <summary>
        /// Constructor for <see cref="LshwDiskParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public LshwDiskParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Disk> Parse()
        {
            XDocument lshwDiskResults = XDocument.Parse(this.RawText);

            List<Disk> disks = new List<Disk>();
            if (lshwDiskResults.Descendants()?.Any() == true)
            {
                // Note:
                // Examples of the output of the "lshw -xml -c disk -c storage" can be found in source in the
                // /VirtualClient/Documentation/Examples directory.
                IEnumerable<XElement> nodes = lshwDiskResults.Elements("list")?.Elements("node");
                if (nodes?.Any() == true)
                {
                    foreach (XElement xmlDiskNode in nodes)
                    {
                        disks.Add(ParseDisk(xmlDiskNode));
                    }
                }
            }

            return disks;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.RawText.Trim();
        }

        private static DiskVolume ParseVolume(XElement xmlVolume)
        {
            IDictionary<string, IConvertible> volumeProperties = ReadProperties(xmlVolume);
            IDictionary<string, IConvertible> volumeConfigurations = ReadConfigurations(xmlVolume);
            IDictionary<string, IConvertible> volumeCapabilities = ReadCapabilities(xmlVolume);

            volumeProperties.AddRange(volumeConfigurations);
            volumeProperties.AddRange(volumeCapabilities);

            IList<string> accessPath = ReadLogicalNames(xmlVolume, out string devicePath);
            int lun = ReadLogicalUnit(xmlVolume);

            return new DiskVolume(
                lun,
                devicePath,
                accessPath,
                volumeProperties);
        }

        private static Disk ParseDisk(XElement xmlDiskNode)
        {
            IDictionary<string, IConvertible> diskProperties = ReadProperties(xmlDiskNode);
            IDictionary<string, IConvertible> diskConfigurations = ReadConfigurations(xmlDiskNode);
            IDictionary<string, IConvertible> diskCapabilities = ReadCapabilities(xmlDiskNode);

            diskProperties.AddRange(diskConfigurations);
            diskProperties.AddRange(diskCapabilities);

            ReadLogicalNames(xmlDiskNode, out string devicePath);
            int lun = ReadLogicalUnit(xmlDiskNode);

            List<DiskVolume> volumes = new List<DiskVolume>();
            IEnumerable<XElement> diskVolumes = xmlDiskNode.Elements("node");
            if (diskVolumes?.Any() == true)
            {
                foreach (XElement xmlVolume in diskVolumes)
                {
                    DiskVolume volume = ParseVolume(xmlVolume);
                    volumes.Add(volume);
                }
            }

            return new Disk(
                lun,
                devicePath,
                volumes,
                diskProperties);
        }

        private static int ReadLogicalUnit(XElement xmlElement)
        {
            // Sometimes the physical id will be like 0.0.3, translating that to 3.
            string hex = xmlElement.Element(Disk.UnixDiskProperties.PhysicalId).Value.Replace(".", string.Empty).Trim();
            int lun = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);

            return lun;
        }

        private static IList<string> ReadLogicalNames(XElement xmlElement, out string devicePath)
        {
            IList<string> logicalNames = new List<string>();
            IEnumerable<XElement> logicalNamesXml = xmlElement.Elements(Disk.UnixDiskProperties.LogicalName);

            foreach (XElement logicalName in logicalNamesXml)
            {
                logicalNames.Add(logicalName.Value);
            }

            devicePath = logicalNames.FirstOrDefault(n => DevicePathPrefixs.Any(prefex => prefex.StartsWith(prefex, StringComparison.OrdinalIgnoreCase)));
            logicalNames.Remove(devicePath);

            return logicalNames;
        }

        private static IDictionary<string, IConvertible> ReadProperties(XElement xmlElement)
        {
            IDictionary<string, IConvertible> diskProperties = new Dictionary<string, IConvertible>();

            List<string> propertiesInAttributes = new List<string>()
            {
                Disk.UnixDiskProperties.Id,
                Disk.UnixDiskProperties.Claimed,
                Disk.UnixDiskProperties.Class,
                Disk.UnixDiskProperties.Handle
            };

            foreach (string property in propertiesInAttributes)
            {
                if (xmlElement.Attribute(property) != null)
                {
                    diskProperties.Add(property, string.Join(",", xmlElement.Attribute(property).Value.Trim()));
                }
            }

            List<string> properties = new List<string>()
            {
                Disk.UnixDiskProperties.Description,
                Disk.UnixDiskProperties.Product,
                Disk.UnixDiskProperties.Vendor,
                Disk.UnixDiskProperties.PhysicalId,
                Disk.UnixDiskProperties.BusInfo,
                Disk.UnixDiskProperties.LogicalName,
                Disk.UnixDiskProperties.Device,
                Disk.UnixDiskProperties.Version,
                Disk.UnixDiskProperties.Size,
                Disk.UnixDiskProperties.Serial,
                Disk.UnixDiskProperties.Capacity
            };

            foreach (string property in properties)
            {
                if (xmlElement.Elements(property).Any())
                {
                    diskProperties.Add(property, string.Join(",", xmlElement.Elements(property).Select(e => e.Value.Trim())));
                }
            }

            return diskProperties;
        }

        private static IDictionary<string, IConvertible> ReadCapabilities(XElement xmlElement)
        {
            IDictionary<string, IConvertible> parsedCapabilities = new Dictionary<string, IConvertible>();
            IEnumerable<XElement> capabilitiesInXml = xmlElement.Element("capabilities")?.Elements("capability");
            if (capabilitiesInXml?.Any() == true)
            {
                foreach (XElement capability in capabilitiesInXml)
                {
                    // Example XML:
                    // <capabilities>
                    //    <capability id="boot" >Contains boot code</capability>
                    //    <capability id="fat" >Windows FAT</capability>
                    //    <capability id="initialized" >initialized volume</capability>
                    // </capabilities>

                    parsedCapabilities.Add(capability.Attribute("id").Value, capability.Value.Trim());
                }
            }

            return parsedCapabilities;
        }

        private static IDictionary<string, IConvertible> ReadConfigurations(XElement xmlElement)
        {
            IDictionary<string, IConvertible> parsedConfigurations = new Dictionary<string, IConvertible>();
            IEnumerable<XElement> configurationsInXml = xmlElement.Element("configuration")?.Elements("setting");

            if (configurationsInXml?.Any() == true)
            {
                foreach (XElement configuration in configurationsInXml)
                {
                    // Example XML:
                    // <configuration>
                    //    <setting id = "FATs" value = "2" />
                    //    <setting id = "filesystem" value = "fat" />
                    //    <setting id = "label" value = "UEFI" />
                    //    <setting id = "mount.fstype" value = "vfat" />
                    //    <setting id = "mount.options" value = "rw,relatime,fmask=0077,dmask=0077,codepage=437,iocharset=iso8859-1,shortname=mixed,errors=remount-ro" />
                    //    <setting id = "state" value = "mounted" />
                    // </configuration >

                    parsedConfigurations.Add(configuration.Attribute("id").Value, configuration.Attribute("value").Value.Trim());
                }
            }

            return parsedConfigurations;
        }
    }
}
