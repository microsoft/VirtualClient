// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient;

    /// <summary>
    /// Extensions for parsing test documents related to data tables.
    /// </summary>
    public static class DataTableExtensions
    {
        private static readonly Regex WhitespaceExpression = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Format text into data table delimited by column Delimiter regex.
        /// </summary>
        /// <param name="text">Input text</param>
        /// <param name="delimiter">Delimiter regex used to separate each row into columns.</param>
        /// <param name="tableName">Table name. If not supplied, the first line of the text will be used as table name.</param>
        /// <param name="columnNames">Column names. If not supplied, the first line of the text will be used as columns name, parsed using the same Delimiter regex.</param>
        /// <returns>Converted DataTable object.</returns>
        public static DataTable ConvertToDataTable(string text, Regex delimiter, string tableName = null, IList<string> columnNames = null)
        {
            List<string> rows = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tableName == null)
            {
                tableName = rows.FirstOrDefault();
                rows.RemoveAt(0);
            }

            DataTable result = new DataTable(tableName);

            if (columnNames == null)
            {
                columnNames = Regex.Split(rows.First(), delimiter.ToString(), delimiter.Options);
                rows.RemoveAt(0);
            }

            foreach (string column in columnNames)
            {
                result.Columns.Add(new DataColumn(column, typeof(IConvertible)));
            }

            foreach (string row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row))
                {
                    string[] values = Regex.Split(row, delimiter.ToString(), delimiter.Options).Select(v => v.Trim()).ToArray();

                    if (values.Count() > columnNames.Count)
                    {
                        values = values.Take(columnNames.Count).ToArray();
                    }

                    result.Rows.Add(values);
                }
            }

            return result;
        }

        /// <summary>
        /// Convert text into data table based on cell position. Suitable for formatted table with cells of equal lengths.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <param name="columnNames">The column names of the cells. Count needs to match the cellIndexAndLength.</param>
        /// <param name="tableName">If not supplied, tablename will be the first row of the text.</param>
        /// <returns>Converted DataTable.</returns>
        public static DataTable DataTableFromCsv(string text, IList<string> columnNames = null, string tableName = null)
        {
            List<string> rows = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

            DataTable result = new DataTable(tableName);

            // If column names are supplied, use it, otherwise use the first row as column names.
            if (columnNames == null)
            {
                columnNames = Regex.Split(rows.First(), ",", RegexOptions.ExplicitCapture);
                rows.RemoveAt(0);
            }

            foreach (string column in columnNames)
            {
                result.Columns.Add(new DataColumn(column.Trim(), typeof(IConvertible)));
            }

            foreach (string row in rows)
            {
                string[] values = Regex.Split(row, ",", RegexOptions.ExplicitCapture);
                values = values.Select(v => v.Trim()).ToArray();
                result.Rows.Add(values);
            }

            return result;
        }

        /// <summary>
        /// Convert text into data table based on cell position. Suitable for formatted table with cells of equal lengths.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <param name="cellIndexAndLength">The starting index and the length of each cell needed. List(startIndex, length).</param>
        /// <param name="columnNames">The column names of the cells. Count needs to match the cellIndexAndLength.</param>
        /// <param name="tableName">If not supplied, tablename will be the first row of the text.</param>
        /// <returns>Converted DataTable.</returns>
        public static DataTable ConvertToDataTable(string text, IList<KeyValuePair<int, int>> cellIndexAndLength, IList<string> columnNames, string tableName = null)
        {
            if (cellIndexAndLength.Count != columnNames.Count)
            {
                throw new ArgumentOutOfRangeException(
                    $"The cellIndexAndLength count:'{cellIndexAndLength.Count}' and columnNames count:{columnNames.Count}' need to match.");
            }

            List<string> rows = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tableName == null)
            {
                tableName = rows.FirstOrDefault();
                rows.RemoveAt(0);
            }

            DataTable result = new DataTable(tableName);

            foreach (string column in columnNames)
            {
                result.Columns.Add(new DataColumn(column, typeof(IConvertible)));
            }

            foreach (string row in rows)
            {
                List<IConvertible> values = new List<IConvertible>();
                for (int columnIndex = 0; columnIndex < columnNames.Count; columnIndex++)
                {
                    string cellValue = string.Empty;
                    if (row.Length > cellIndexAndLength[columnIndex].Key + cellIndexAndLength[columnIndex].Value)
                    {
                        cellValue = row.Substring(cellIndexAndLength[columnIndex].Key, cellIndexAndLength[columnIndex].Value);
                    }
                    else if (row.Length > cellIndexAndLength[columnIndex].Key)
                    {
                        cellValue = row.Substring(cellIndexAndLength[columnIndex].Key);
                    }

                    values.Add(cellValue.Trim());
                }

                result.Rows.Add(values.ToArray());
            }

            return result;
        }

        /// <summary>
        /// Convert selected columns of a data table into List of Metrics.
        /// </summary>
        /// <param name="dataTable">Source data table.</param>
        /// <param name="nameIndex">Column index of the metric name.</param>
        /// <param name="name">Name of the metric</param>
        /// <param name="valueIndex">Column index of the metric value.</param>
        /// <param name="unitIndex">Column index of the unit. Will use parameter unit instead if not supplied.</param>
        /// <param name="unit">Unit of the metric.</param>
        /// <param name="namePrefix">If supplied, it will append extra prefix to metric name.</param>
        /// <param name="metricRelativity">It shows Metric Relativity whether higherIsBetter,lowerIsBetter or notDefined.</param>
        /// <param name="tagIndex">Index of the tag.</param>
        /// <param name="startTimeIndex">Index of start time. To avoid format issue, cell needs to be in ticks.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTimeIndex">Index of end time. To avoid format issue, cell needs to be in ticks.</param>
        /// <param name="endTime">End time.</param>
        /// <param name="tags">Tags</param>
        /// <param name="ignoreFormatError">If this method ignore FormatException. Default is false.</param>
        /// <returns>Parsed Metrics from a data table.</returns>
        public static IList<Metric> GetMetrics(
            this DataTable dataTable, 
            int valueIndex, 
            int nameIndex = -1, 
            string name = null, 
            int unitIndex = -1, 
            string unit = null, 
            string namePrefix = null,
            MetricRelativity metricRelativity = MetricRelativity.Undefined,
            int tagIndex = -1, 
            int startTimeIndex = -1,
            DateTime? startTime = null,
            int endTimeIndex = -1,
            DateTime? endTime = null,
            List<string> tags = null, 
            bool ignoreFormatError = false)
        {
            IList<Metric> metrics = new List<Metric>();

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    object value = row[valueIndex];
                    if (value == null || value == DBNull.Value)
                    {
                        continue;
                    }

                    string normalizedValue = DataTableExtensions.WhitespaceExpression.Replace(value?.ToString(), string.Empty);
                    if (!string.IsNullOrWhiteSpace(normalizedValue) && double.TryParse(normalizedValue, out double metricValue))
                    {
                        if (unitIndex >= 0)
                        {
                            unit = Convert.ToString(row[unitIndex]);
                        }

                        if (tagIndex >= 0)
                        {
                            tags = new List<string>() { Convert.ToString(row[tagIndex]) };
                        }

                        if (nameIndex >= 0)
                        {
                            name = (string)row[nameIndex];
                        }

                        if (startTimeIndex >= 0)
                        {
                            startTime = new DateTime(Convert.ToInt64(row[startTimeIndex]));
                        }

                        if (endTimeIndex >= 0)
                        {
                            endTime = new DateTime(Convert.ToInt64(row[endTimeIndex]));
                        }

                        Metric metric = new Metric(namePrefix + name, metricValue, unit, metricRelativity, tags)
                        {
                            StartTime = (startTime == null) ? DateTime.MinValue : startTime.Value,
                            EndTime = (endTime == null) ? DateTime.MinValue : endTime.Value
                        };
                        
                        metrics.Add(metric);
                    }
                }
                catch (Exception exc)
                {
                    if (ignoreFormatError && (exc is InvalidCastException || exc is FormatException))
                    {
                        // Ignore Error
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return metrics;
        }

        /// <summary>
        /// Split one data column into multiple columns based on the regex. The extra columns will be inserted right to the index column.
        /// </summary>
        /// <param name="dataTable">Original datatable.</param>
        /// <param name="columnIndex">Column to split.</param>
        /// <param name="splitRegex">Regex to split the column.</param>
        /// <param name="columnNames">Names for the new column.</param>
        public static void SplitDataColumn(this DataTable dataTable, int columnIndex, Regex splitRegex, IList<string> columnNames)
        {
            for (int columnCount = 0; columnCount < columnNames.Count; columnCount++)
            {
                DataColumn newColumn = dataTable.Columns.Add(columnNames[columnCount]);
                newColumn.SetOrdinal(columnIndex + columnCount + 1);
            }

            for (int rowCount = 0; rowCount < dataTable.Rows.Count; rowCount++)
            {
                List<string> splitResult = Regex.Split((string)dataTable.Rows[rowCount][columnIndex], splitRegex.ToString(), splitRegex.Options).ToList();
                for (int columnCount = 0; columnCount < columnNames.Count && columnCount < splitResult.Count; columnCount++)
                {
                    dataTable.Rows[rowCount][columnIndex + columnCount + 1] = splitResult[columnCount] ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Merge two columns into one
        /// </summary>
        /// <param name="dataTable">Original datatable</param>
        /// <param name="columnIndex1">Index of first column to merge.</param>
        /// <param name="columnIndex2">Index of second column to merge.</param>
        /// <param name="separator">Separator between two columns, default null</param>
        public static void MergeDataColumn(this DataTable dataTable, int columnIndex1, int columnIndex2, string separator = null)
        {
            string newColumnName = dataTable.Columns[columnIndex1].ColumnName + separator + dataTable.Columns[columnIndex2].ColumnName;
            dataTable.Columns[columnIndex1].ColumnName = newColumnName;

            for (int rowCount = 0; rowCount < dataTable.Rows.Count; rowCount++)
            {
                string newCellValue = dataTable.Rows[rowCount][columnIndex1] + separator + dataTable.Rows[rowCount][columnIndex2];
                dataTable.Rows[rowCount][columnIndex1] = newCellValue;
            }

            dataTable.Columns.RemoveAt(columnIndex2);
        }

        /// <summary>
        /// Translate the unit of numbers in data cells. Example: 1K->1000 and 1M->1000000.
        /// </summary>
        /// <param name="dataTable">Original datatable.</param>
        public static void TranslateUnits(this DataTable dataTable)
        {
            for (int rowCount = 0; rowCount < dataTable.Rows.Count; rowCount++)
            {
                for (int columnCount = 0; columnCount < dataTable.Columns.Count; columnCount++)
                {
                    dataTable.Rows[rowCount][columnCount] = TextParsingExtensions.TranslateNumericUnit(Convert.ToString(dataTable.Rows[rowCount][columnCount]));
                }
            }
        }

        /// <summary>
        /// Replace empty cell with a string replacement. Default to fill with 0.
        /// </summary>
        /// <param name="dataTable">Original datatable.</param>
        /// <param name="replacement">Replacement string on empty cells</param>
        public static void ReplaceEmptyCell(this DataTable dataTable, string replacement = "0")
        {
            for (int rowCount = 0; rowCount < dataTable.Rows.Count; rowCount++)
            {
                for (int columnCount = 0; columnCount < dataTable.Columns.Count; columnCount++)
                {
                    if (string.IsNullOrWhiteSpace(Convert.ToString(dataTable.Rows[rowCount][columnCount])))
                    {
                        dataTable.Rows[rowCount][columnCount] = replacement;
                    }
                }
            }
        }

        /// <summary>
        /// Replace cell that matches the regex with a string replacement. Default to fill with 0.
        /// </summary>
        /// <param name="dataTable">Original datatable.</param>
        /// <param name="cellRegex">Regex to match the cell</param>
        /// <param name="replacement">Replacement string on empty cells</param>
        public static void ReplaceCell(this DataTable dataTable, Regex cellRegex, IConvertible replacement)
        {
            for (int rowCount = 0; rowCount < dataTable.Rows.Count; rowCount++)
            {
                for (int columnCount = 0; columnCount < dataTable.Columns.Count; columnCount++)
                {
                    if (Regex.IsMatch(Convert.ToString(dataTable.Rows[rowCount][columnCount]), cellRegex.ToString(), cellRegex.Options))
                    {
                        dataTable.Rows[rowCount][columnCount] = replacement;
                    }
                }
            }
        }
    }
}
