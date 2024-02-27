// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.Amqp.Framing;
    using Polly.Caching;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts.Parser;

    /// <summary>
    /// Parses the output of the 'dmidecode' toolset on Unix systems.
    /// </summary>
    public class DmiDecodeParser : ITextParser<IEnumerable<MemoryChipInfo>>
    {
        private static readonly Regex DeviceInfoExpression = new Regex(@"([\x20-\x7E]+)\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MemoryDeviceExpression = new Regex(@"Handle\s+0x[a-z0-9]+,\s*DMI[\x20-\x7E]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NumericExpression = new Regex(@"\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool TryParse(string text, out IEnumerable<MemoryChipInfo> results)
        {
            text.ThrowIfNullOrWhiteSpace(nameof(text));
            results = null;

            try
            {
                string[] memoryDevices = DmiDecodeParser.MemoryDeviceExpression.Split(text);
                if (memoryDevices?.Any() == true)
                {
                    int chipIndex = 0;
                    foreach (string memoryDevice in memoryDevices)
                    {
                        try
                        {
                            string deviceInfo = memoryDevice.Trim();
                            if (deviceInfo.StartsWith("Memory Device"))
                            {
                                MatchCollection matches = DmiDecodeParser.DeviceInfoExpression.Matches(deviceInfo);
                                if (matches?.Any() == true)
                                {
                                    IDictionary<string, string> deviceInfoValues = DmiDecodeParser.ParseDeviceInfoValues(matches);

                                    // A memory slot can exist without having a chip in it. When this is the case, the user can
                                    // tell because the slot will not have a "Size" defined that is convertible to a number. In order
                                    // to know that we have a valid memory chip definition, both the "Size" and the "Speed" must have
                                    // numeric parts of the value (e.g. 7168 MB, 1866 MT/s).
                                    //
                                    // e.g.
                                    // Handle 0x0018, DMI type 17, 40 bytes
                                    // Memory Device
                                    // Array Handle: 0x0016
                                    // Error Information Handle: Not Provided
                                    // Total Width: 64 bits
                                    // Data Width: 64 bits
                                    // Size: No Module Installed
                                    // Form Factor: Unknown
                                    if (deviceInfoValues.TryGetValue("Size", out string size) && DmiDecodeParser.NumericExpression.IsMatch(size)
                                        && deviceInfoValues.TryGetValue("Speed", out string speed) && DmiDecodeParser.NumericExpression.IsMatch(speed))
                                    {
                                        long.TryParse(TextParsingExtensions.TranslateByteUnit(size), out long memoryCapacity);
                                        long.TryParse(DmiDecodeParser.NumericExpression.Match(speed).Value, out long memorySpeed);

                                        if (memoryCapacity <= 0 || memorySpeed <= 0)
                                        {
                                            continue;
                                        }

                                        if (results == null)
                                        {
                                            results = new List<MemoryChipInfo>();
                                        }

                                        chipIndex++;
                                        string manufacturer = deviceInfoValues["Manufacturer"];
                                        string partNumber = deviceInfoValues["Part Number"];

                                        (results as List<MemoryChipInfo>).Add(
                                            new MemoryChipInfo(
                                                $"Memory_{chipIndex}",
                                                $"{manufacturer} Memory Chip",
                                                memoryCapacity,
                                                memorySpeed,
                                                manufacturer?.Trim(),
                                                partNumber?.Trim()));
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Best effort only. Continue if the data cannot be parsed correctly.
                        }
                    }
                }
            }
            catch
            {
            }

            return results?.Any() == true;
        }

        private static IDictionary<string, string> ParseDeviceInfoValues(MatchCollection matches)
        {
            IDictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in matches)
            {
                values.Add(match.Groups[1].Value.Trim(), match.Groups[2].Value?.Trim());
            }

            return values;
        }
    }
}
