using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    public static class DataTableTestExtensions
    {
        /// <summary>
        /// Print data table on Console.
        /// </summary>
        /// <param name="dataTable">Input data table.</param>
        public static void PrintDataTable(this DataTable dataTable)
        {
            string result = string.Empty;
            StringBuilder resultBuilder = new StringBuilder();

            if (dataTable != null && dataTable.Rows != null && dataTable.Columns != null && dataTable.Columns.Count > 0)
            {
                int lastItemIndex = dataTable.Columns.Count - 1;
                int index = 0;

                foreach (DataColumn column in dataTable.Columns)
                {
                    resultBuilder.Append(column.ColumnName);

                    if (index < lastItemIndex)
                    {
                        resultBuilder.Append(", ");  // add the separator
                    }

                    index++;
                }

                resultBuilder.AppendLine();  // add a CRLF after column names row

                foreach (DataRow dataRow in dataTable.Rows)
                {
                    lastItemIndex = dataRow.ItemArray.Length - 1;
                    index = 0;

                    foreach (object item in dataRow.ItemArray)
                    {
                        resultBuilder.Append(item);

                        if (index < lastItemIndex)
                        {
                            resultBuilder.Append(", ");  // add the separator
                        }

                        index++;
                    }

                    resultBuilder.AppendLine();  // add a CRLF after each data row
                }

                result = resultBuilder.ToString();
            }

            Console.Write(result);
        }

        /// <summary>
        /// Data table with formated column width.
        /// </summary>
        /// <param name="dataTable">Input data table.</param>
        public static void PrintDataTableFormatted(this DataTable dataTable)
        {
            Console.WriteLine();
            Dictionary<string, int> colWidths = new Dictionary<string, int>();

            foreach (DataColumn col in dataTable.Columns)
            {
                Console.Write(col.ColumnName);
                var maxLabelSize = dataTable.Rows.OfType<DataRow>()
                        .Select(m => (m.Field<object>(col.ColumnName)?.ToString() ?? string.Empty).Length)
                        .OrderByDescending(m => m).FirstOrDefault();

                colWidths.Add(col.ColumnName, maxLabelSize);
                for (int i = 0; i < maxLabelSize - col.ColumnName.Length + 10; i++)
                {
                    Console.Write(" ");
                }
            }

            Console.WriteLine();

            foreach (DataRow dataRow in dataTable.Rows)
            {
                for (int j = 0; j < dataRow.ItemArray.Length; j++)
                {
                    Console.Write(dataRow.ItemArray[j]);
                    for (int i = 0; i < colWidths[dataTable.Columns[j].ColumnName] - dataRow.ItemArray[j].ToString().Length + 10; i++)
                    {
                        Console.Write(" ");
                    }
                }

                Console.WriteLine();
            }
        }

        public static void PrintMetricList(IList<Metric> metricList)
        {
            foreach (Metric metric in metricList)
            {
                Console.WriteLine($"{metric.Name}   |   {metric.Value}  |   {metric.Unit}");
            }
        }
    }
}