// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class AtopParserUnitTests
    {
        [Test]
        public void AtopParserParsesMetricsCorrectly_Scenario1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample1.txt");
            string rawText = File.ReadAllText(outputPath);

            // Single distinct sample group. If there is only one record we consider it.
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(98, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Max", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Median", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time", 11);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Min", 11);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Max", 11);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Median", 11);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Max", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Median", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time", 386);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Min", 386);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Max", 386);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Median", 386);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Max", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Median", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\CSwitches", 6013840000);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", 0.77);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", 0.40);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", 0.22);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Serviced Interrupts", 2160600000);

            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% System Time", 2);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% User Time", 4);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IRQ Time", 6);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% Idle Time", 94);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IOWait Time", 2);

            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% System Time", 3);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% User Time", 5);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IRQ Time", 7);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% Idle Time", 92);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IOWait Time", 3);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% User Time", 3);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IRQ Time", 5);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% Idle Time", 96);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IOWait Time", 1);

            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% System Time", 4);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% User Time", 6);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IRQ Time", 8);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% Idle Time", 90);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IOWait Time", 4);

            MetricAssert.Exists(metrics, @"\Memory\Total Bytes", 16750372454.4);
            MetricAssert.Exists(metrics, @"\Memory\Free Bytes", 4831838208);
            MetricAssert.Exists(metrics, @"\Memory\Cached Bytes", 9019431321.6);
            MetricAssert.Exists(metrics, @"\Memory\Buffer Bytes", 244632780.8);
            MetricAssert.Exists(metrics, @"\Memory\Kernel Bytes", 868325785.6);

            MetricAssert.Exists(metrics, @"\Memory\Swap Space Total Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Free Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Committed Bytes", 8160437862.4);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Limit Bytes", 8375186227.2);

            MetricAssert.Exists(metrics, @"\Memory\Page Scans", 265080000);
            MetricAssert.Exists(metrics, @"\Memory\Page Steals", 160600000);
            MetricAssert.Exists(metrics, @"\Memory\Page Reclaims", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Reads", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Writes", 0);

            MetricAssert.Exists(metrics, @"\Disk\Avg. % Busy Time", 4);
            MetricAssert.Exists(metrics, @"\Disk\# Reads", 5429342);
            MetricAssert.Exists(metrics, @"\Disk\# Writes", 135701357);
            MetricAssert.Exists(metrics, @"\Disk\Avg. Request Time", 2, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, @"\Disk(sda)\% Busy Time", 6);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Reads", 463);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\Avg. Request Time", 3.50, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\% Busy Time", 4);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Reads", 726);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Writes", 1357);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\Avg. Request Time", 2.00, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\% Busy Time", 2);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Reads", 5428153);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Writes", 135700000);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\Avg. Request Time", 0.50, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, @"\Network\TCP Segments Received", 73127000);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Transmitted", 98255000);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Received", 12014000);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Transmitted", 12015000);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Received", 85141794);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Transmitted", 102811000);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Forwarded", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Delivered", 85140000);
            MetricAssert.Exists(metrics, @"\Network\Avg. % Usage", 15);
            MetricAssert.Exists(metrics, @"\Network\Packets Received", 56832216);
            MetricAssert.Exists(metrics, @"\Network\Packets Transmitted", 71663000);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Received", 20);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Transmitted", 12);

            MetricAssert.Exists(metrics, @"\Network(eth0)\% Usage", 20);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Received", 52603000);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Transmitted", 60147000);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Received", 37);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Transmitted", 20);
            MetricAssert.Exists(metrics, @"\Network(eth1)\% Usage", 10);
            MetricAssert.Exists(metrics, @"\Network(eth1)\Packets Received", 4229216);
            MetricAssert.Exists(metrics, @"\Network(eth1)\Packets Transmitted", 11516000);
            MetricAssert.Exists(metrics, @"\Network(eth1)\KB/sec Received", 3);
            MetricAssert.Exists(metrics, @"\Network(eth1)\KB/sec Transmitted", 4);
        }

        [Test]
        public void AtopParserParsesMetricsCorrectly_Scenario2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample2.txt");
            string rawText = File.ReadAllText(outputPath);

            // Single distinct sample group. If there is only one record we consider it.
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(93, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Max", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Median", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Min", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Max", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Median", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Min", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Max", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Median", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time", 399);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Min", 399);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Max", 399);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Median", 399);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Max", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Median", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\CSwitches", 1432210000);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Serviced Interrupts", 308730000);

            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% User Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% Idle Time", 100);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IOWait Time", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% User Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% Idle Time", 100);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IOWait Time", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% User Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% Idle Time", 100);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IOWait Time", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% User Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% Idle Time", 100);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IOWait Time", 0);

            MetricAssert.Exists(metrics, @"\Memory\Total Bytes", 33715493273.6);
            MetricAssert.Exists(metrics, @"\Memory\Free Bytes", 30279519436.8);
            MetricAssert.Exists(metrics, @"\Memory\Cached Bytes", 2791728742.4);
            MetricAssert.Exists(metrics, @"\Memory\Buffer Bytes", 5767168);
            MetricAssert.Exists(metrics, @"\Memory\Kernel Bytes", 263192576);

            MetricAssert.Exists(metrics, @"\Memory\Swap Space Total Bytes", 21474836480);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Free Bytes", 21474836480);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Committed Bytes", 599156326.4);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Limit Bytes", 38332583116.8);

            MetricAssert.Exists(metrics, @"\Disk\Avg. % Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk\# Reads", 23289);
            MetricAssert.Exists(metrics, @"\Disk\# Writes", 3676005);
            MetricAssert.Exists(metrics, @"\Disk\Avg. Request Time", 3, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, @"\Disk(sda)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Reads", 22018);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Writes", 3000000);
            MetricAssert.Exists(metrics, @"\Disk(sda)\Avg. Request Time", 2, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Reads", 459);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Writes", 2679);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\Avg. Request Time", 4, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Reads", 812);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Writes", 673326);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\Avg. Request Time", 3, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, @"\Network\TCP Segments Received", 25041000);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Transmitted", 36064000);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Received", 19923);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Transmitted", 69215);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Received", 25110356);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Transmitted", 36131777);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Forwarded", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Delivered", 25110000);
            MetricAssert.Exists(metrics, @"\Network\Avg. % Usage", 0);
            MetricAssert.Exists(metrics, @"\Network\Packets Received", 31370860);
            MetricAssert.Exists(metrics, @"\Network\Packets Transmitted", 36693216);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Received", 7.5);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Transmitted", 3.5);

            MetricAssert.Exists(metrics, @"\Network(eth0)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Received", 31321000);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Transmitted", 36083000);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Received", 15);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Transmitted", 7);
            MetricAssert.Exists(metrics, @"\Network(eth1)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(eth1)\Packets Received", 49860);
            MetricAssert.Exists(metrics, @"\Network(eth1)\Packets Transmitted", 610216);
            MetricAssert.Exists(metrics, @"\Network(eth1)\KB/sec Received", 0);
            MetricAssert.Exists(metrics, @"\Network(eth1)\KB/sec Transmitted", 0);
        }

        [Test]
        public void AtopParserParsesMetricsCorrectly_Scenario3()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample-1s-5s.txt");
            string rawText = File.ReadAllText(outputPath);

            // 1 second sample rate, 5 intervals = 4 distinct sample groups as we don't consider first atop sample.
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(135, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 8.75);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 5);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Max", 14);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Median", 8);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time", 95.25);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Min", 35);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Max", 132);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Median", 107);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Max", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Median", 0.5);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time", 1506.5);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Min", 1471);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Max", 1569);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Median", 1493);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time", 0.25);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Max", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Median", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% System Time", 2);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% System Time", 2.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% System Time", 0.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% System Time", 0.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% System Time", 0.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% System Time", 0.33333333333333331);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% System Time", 0.33333333333333331);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% System Time", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% User Time", 19.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% User Time", 35);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% User Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% User Time", 19.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% User Time", 0.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% User Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% User Time", 1.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% User Time", 2.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% User Time", 0.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% User Time", 0.66666666666666663);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% User Time", 1.3333333333333333);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% User Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% User Time", 1.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% User Time", 88);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IRQ Time", 0.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IRQ Time", 1.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IRQ Time", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% Idle Time", 78.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% Idle Time", 65);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% Idle Time", 98);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% Idle Time", 78);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% Idle Time", 98);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% Idle Time", 98);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% Idle Time", 97.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% Idle Time", 97.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% Idle Time", 97.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% Idle Time", 99);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% Idle Time", 98.333333333333329);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% Idle Time", 99);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% Idle Time", 97.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% Idle Time", 12);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IOWait Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IOWait Time", 0);

            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", 4.6475);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", 6.4625);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", 6.4875000000000007);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\CSwitches", 11337);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Serviced Interrupts", 8555);

            MetricAssert.Exists(metrics, @"\Memory\Total Bytes", 67430986547.2);
            MetricAssert.Exists(metrics, @"\Memory\Free Bytes", 57579405312);
            MetricAssert.Exists(metrics, @"\Memory\Cached Bytes", 4724464025.6);
            MetricAssert.Exists(metrics, @"\Memory\Buffer Bytes", 255328256);
            MetricAssert.Exists(metrics, @"\Memory\Kernel Bytes", 759536025.5999999);

            MetricAssert.Exists(metrics, @"\Memory\Swap Space Total Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Free Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Committed Bytes", 9851581235.1999989);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Limit Bytes", 33715493273.6);

            MetricAssert.Exists(metrics, @"\Disk(sda)\% Busy Time", 8);
            MetricAssert.Exists(metrics, @"\Disk\Avg. % Busy Time", 8);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Reads", 0);
            MetricAssert.Exists(metrics, @"\Disk\# Reads", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Writes", 488);
            MetricAssert.Exists(metrics, @"\Disk\# Writes", 488);
            MetricAssert.Exists(metrics, @"\Disk(sda)\Avg. Request Time", 0.16);
            MetricAssert.Exists(metrics, @"\Disk\Avg. Request Time", 0.16);

            MetricAssert.Exists(metrics, @"\Network\TCP Segments Received", 54);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Transmitted", 97);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Received", 0);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Transmitted", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Received", 54);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Transmitted", 57);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Forwarded", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Delivered", 54);

            MetricAssert.Exists(metrics, @"\Network(enP4560)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network\Avg. % Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(eth0)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Received", 54);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Received", 54);
            MetricAssert.Exists(metrics, @"\Network\Packets Received", 108);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Transmitted", 97);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Transmitted", 57);
            MetricAssert.Exists(metrics, @"\Network\Packets Transmitted", 154);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Received", 6);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Received", 5.375);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Received", 4.75);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Transmitted", 221.5);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Transmitted", 219.25);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Transmitted", 217);

        }

        [Test]
        public void AtopParserParsesMetricsCorrectly_Scenario4()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample-1s-60s.txt");
            string rawText = File.ReadAllText(outputPath);

            // 1 second sample rate, 60 intervals = 59 distinct sample groups as we don't consider first atop sample.
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(150, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 9.796610169491526);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Max", 21);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Median", 10);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time", 94.237288135593218);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Min", 29);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Max", 143);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Median", 98);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time", 2.3050847457627119);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Max", 12);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Median", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time", 1502.6949152542372);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Min", 1429);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Max", 1577);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Median", 1496);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time", 1.152542372881356);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Max", 27);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Median", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% System Time", 1.0232558139534884);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% System Time", 0.68292682926829273);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% System Time", 0.91304347826086951);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% System Time", 0.29729729729729731);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% System Time", 0.97435897435897434);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% System Time", 1.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% System Time", 1.7441860465116279);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% System Time", 1.1836734693877551);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% System Time", 0.77777777777777779);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% System Time", 1.5892857142857142);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% System Time", 0.9);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% System Time", 0.77142857142857146);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% System Time", 0.625);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% System Time", 1.6363636363636365);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% System Time", 0.75);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% System Time", 0.6428571428571429);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% User Time", 16.069767441860463);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% User Time", 17.707317073170731);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% User Time", 10.108695652173912);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% User Time", 0.78378378378378377);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% User Time", 11.743589743589743);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% User Time", 23.346153846153847);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% User Time", 9.9534883720930232);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% User Time", 1.6734693877551021);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% User Time", 4.4444444444444446);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% User Time", 0.9642857142857143);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% User Time", 1.4);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% User Time", 13.857142857142858);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% User Time", 8.4375);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% User Time", 7.9545454545454541);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% User Time", 12.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% User Time", 19.928571428571427);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IRQ Time", 0.069767441860465115);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IRQ Time", 0.81081081081081086);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IRQ Time", 0.019230769230769232);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IRQ Time", 0.33333333333333331);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IRQ Time", 1.6607142857142858);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% IRQ Time", 0.028571428571428571);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IRQ Time", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% Idle Time", 82.813953488372093);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% Idle Time", 81.609756097560975);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% Idle Time", 88.956521739130437);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% Idle Time", 98.108108108108112);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% Idle Time", 87.1025641025641);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% Idle Time", 75.038461538461533);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% Idle Time", 87.883720930232556);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% Idle Time", 97.0204081632653);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% Idle Time", 94.444444444444443);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% Idle Time", 95.803571428571431);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% Idle Time", 97.7);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% Idle Time", 85.342857142857142);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% Idle Time", 90.5625);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% Idle Time", 89.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% Idle Time", 86.75);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% Idle Time", 79.428571428571431);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IOWait Time", 0.023255813953488372);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IOWait Time", 0.021739130434782608);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IOWait Time", 0.17948717948717949);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IOWait Time", 0.096153846153846159);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IOWait Time", 0.46511627906976744);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IOWait Time", 0.12244897959183673);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IOWait Time", 0.375);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IOWait Time", 0.90909090909090906);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IOWait Time", 0);

            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", 5.4228813559322);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", 6.8559322033898313);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", 6.4967796610169524);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\CSwitches", 173092);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Serviced Interrupts", 126737);

            MetricAssert.Exists(metrics, @"\Memory\Total Bytes", 67430986547.200073);
            MetricAssert.Exists(metrics, @"\Memory\Free Bytes", 57645376737.627083);
            MetricAssert.Exists(metrics, @"\Memory\Cached Bytes", 4724464025.6000032);
            MetricAssert.Exists(metrics, @"\Memory\Buffer Bytes", 254969252.0135594);
            MetricAssert.Exists(metrics, @"\Memory\Kernel Bytes", 757704572.09491467);

            MetricAssert.Exists(metrics, @"\Memory\Swap Space Total Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Free Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Committed Bytes", 9771050598.4);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Limit Bytes", 33715493273.600037);
            MetricAssert.Exists(metrics, @"\Memory\Page Scans", 1983);
            MetricAssert.Exists(metrics, @"\Memory\Page Steals", 1983);
            MetricAssert.Exists(metrics, @"\Memory\Page Reclaims", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Reads", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Writes", 0);

            MetricAssert.Exists(metrics, @"\Disk(sda)\% Busy Time", 2.9285714285714284);
            MetricAssert.Exists(metrics, @"\Disk\Avg. % Busy Time", 2.9285714285714284);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Reads", 77);
            MetricAssert.Exists(metrics, @"\Disk\# Reads", 77);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Writes", 2375);
            MetricAssert.Exists(metrics, @"\Disk\# Writes", 2375);
            MetricAssert.Exists(metrics, @"\Disk(sda)\Avg. Request Time", 4.1389285714285711);
            MetricAssert.Exists(metrics, @"\Disk\Avg. Request Time", 4.1389285714285711);

            MetricAssert.Exists(metrics, @"\Network\TCP Segments Received", 196);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Transmitted", 258);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Received", 20);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Transmitted", 20);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Received", 216);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Transmitted", 215);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Forwarded", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Delivered", 216);

            MetricAssert.Exists(metrics, @"\Network(enP4560)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network\Avg. % Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(eth0)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Received", 186);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Received", 204);
            MetricAssert.Exists(metrics, @"\Network\Packets Received", 390);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Transmitted", 264);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Transmitted", 201);
            MetricAssert.Exists(metrics, @"\Network\Packets Transmitted", 465);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Received", 1.7627118644067796);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Received", 2.1440677966101696);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Received", 2.5254237288135593);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Transmitted", 24.271186440677965);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Transmitted", 24.033898305084747);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Transmitted", 23.796610169491526);

        }

        [Test]
        public void AtopParserParsesMetricsCorrectly_Scenario5()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample3.txt");
            string rawText = File.ReadAllText(outputPath);

            // Single distinct sample group
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            // This is a real SAP output, too many outputs so only verifying the count.
            Assert.AreEqual(386, metrics.Count);
        }

        [Test]
        public void AtopParserSupportsDefiningExplicitSubsetsOfCounters_Scenario1()
        {
            // Subsets of Counters
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample-1s-60s.txt");
            string rawText = File.ReadAllText(outputPath);

            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(150, metrics.Count);

            IEnumerable<Metric> expectedCounters = metrics.Where(m => m.Name.StartsWith(@"\Processor"));
            AtopParser specificCounterParser = new AtopParser(rawText, expectedCounters.Select(m => m.Name));
            IList<Metric> actualCounters = specificCounterParser.Parse();

            CollectionAssert.AreEquivalent(
                expectedCounters.Select(m => $"{m.Name}={m.Value}"),
                actualCounters.Select(m => $"{m.Name}={m.Value}"));
        }

        [Test]
        public void AtopParserSupportsDefiningExplicitSubsetsOfCounters_Scenario2()
        {
            // Individual/Single Counters
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample-1s-60s.txt");
            string rawText = File.ReadAllText(outputPath);

            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(150, metrics.Count);

            foreach (Metric expectedMetric in metrics)
            {
                AtopParser parser = new AtopParser(rawText, new List<string> { expectedMetric.Name });
                IList<Metric> actualCounters = parser.Parse();

                Assert.IsNotEmpty(actualCounters);
                Assert.IsTrue(actualCounters.Count == 1);
                Assert.AreEqual(expectedMetric, actualCounters.First());
            }
        }
    }
}