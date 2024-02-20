// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Static representation of the distribution of SQL Server specific files depending on the 
    /// number of disks that are on the system.
    /// </summary>
    public static class SqlServerConfiguration
    {
        /// <summary>
        /// Path to the restored SQL data files.
        /// </summary>
        public const string DataFilePath = "dataFilePath";

        /// <summary>
        /// Path to the restored SQL log files.
        /// </summary>
        public const string LogFilePath = "logFilePath";

        /// <summary>
        /// Path to the SQL temp database data files.
        /// </summary>
        public const string TempDBDataFilePath = "tempDbDataFilePath";

        /// <summary>
        /// Path to the SQL temp database log files.
        /// </summary>
        public const string TempDBLogFilePath = "tempDbLogFilePath";

        private static readonly Dictionary<int, IEnumerable<SqlDiskMapping>> DiskMappings = new Dictionary<int, IEnumerable<SqlDiskMapping>>()
        {
            {
                1, new List<SqlDiskMapping>()
                {
                    // e.g.
                    // E:\data\tpch.mdf
                    // E:\log\tpchlog.ldf
                    // E:\tempdb\tempdb.mdf
                    // E:\tempdb\tempdb.ldf
                    new SqlDiskMapping(SqlServerConfiguration.DataFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.LogFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBDataFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBLogFilePath, 0)
                }
            },
            { 
                // Database and tempDB data (*.mdf
                2, new List<SqlDiskMapping>() 
                { 
                    // e.g.
                    // C:\data\tpch.mdf
                    // C:\tempdb\tempdb.mdf
                    // D:\log\tpchlog.ldf
                    // D:\tempdb\tempdb.ldf
                    new SqlDiskMapping(SqlServerConfiguration.DataFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.LogFilePath, 1),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBDataFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBLogFilePath, 1)
                } 
            },
            {
                3, new List<SqlDiskMapping>()
                {
                    new SqlDiskMapping(SqlServerConfiguration.DataFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.LogFilePath, 1),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBDataFilePath, 2),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBLogFilePath, 2)
                }
            },
            {
                4, new List<SqlDiskMapping>()
                {
                    new SqlDiskMapping(SqlServerConfiguration.DataFilePath, 0),
                    new SqlDiskMapping(SqlServerConfiguration.LogFilePath, 1),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBDataFilePath, 2),
                    new SqlDiskMapping(SqlServerConfiguration.TempDBLogFilePath, 3)
                }
            }
        };

        /// <summary>
        /// Retrieves the disk that the file type should be placed on.
        /// </summary>
        /// <param name="fileType">The type of SQL specific file. (i.e. SQL Database Data files, SQL Database log files, etc)</param>
        /// <param name="disks">The disks that are on the system.</param>
        /// <returns>The disk in which the file should be installed on.</returns>
        public static Disk GetDiskFromFileType(string fileType, IEnumerable<Disk> disks)
        {
            disks.ThrowIfNullOrEmpty(nameof(disks));

            Disk selectedDisk = null;
            IEnumerable<Disk> effectiveDisks = disks.Where(d => !d.IsOperatingSystem());
            int count = Math.Min(effectiveDisks.Count(), SqlServerConfiguration.DiskMappings.Count);

            if (count == 0)
            {
                // Use the OS disk if we cannot identify remote disks.
                selectedDisk = disks.FirstOrDefault(d => d.IsOperatingSystem());
            }
            else
            {
                SqlDiskMapping mapping = SqlServerConfiguration.DiskMappings[count].FirstOrDefault(map => map.FileType.Equals(fileType));
                if (mapping == null)
                {
                    throw new DependencyException(
                        $"SQL Server database installation disk selection failed. The SQL Server file type offered '{fileType}' is not a valid file type. " +
                        $"Supported file types include: {SqlServerConfiguration.DataFilePath}, {SqlServerConfiguration.LogFilePath}, {SqlServerConfiguration.TempDBDataFilePath}, " +
                        $"{SqlServerConfiguration.TempDBLogFilePath}.",
                        ErrorReason.DependencyDescriptionInvalid);
                }

                selectedDisk = effectiveDisks.OrderBy(d => d.Index).ElementAt(mapping.DiskIndex);
            }

            if (selectedDisk == null)
            {
                throw new DependencyException(
                    $"SQL Server database installation disk selection failed. There are no disks on the system that can be defined to host the SQL Server database installation.",
                    ErrorReason.DependencyDescriptionInvalid);
            }

            return selectedDisk;
        }

        private class SqlDiskMapping 
        {
            public SqlDiskMapping(string fileType, int diskIndex)
            {
                this.FileType = fileType;
                this.DiskIndex = diskIndex;
            }

            /// <summary>
            /// The SQL server file type. 
            /// </summary>
            public string FileType { get; }

            /// <summary>
            /// The index of the disk that the file type should be placed on.
            /// </summary>
            public int DiskIndex { get; }
        }
    }
}
