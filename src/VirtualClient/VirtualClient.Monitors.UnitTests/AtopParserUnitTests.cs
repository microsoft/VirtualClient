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

            // Single distinct sample group
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

            // Single distinct sample group
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

            // 1 second sample rate, 5 intervals = 5 distinct sample groups
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(158, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 8.4);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 5);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Max", 14);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Median", 7);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time", 136.2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Min", 35);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Max", 300);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Median", 121);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Max", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Median", 1);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time", 1463);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Min", 1289);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Max", 1569);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Median", 1474);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time", 0.8);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Max", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Median", 0);

            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% System Time", 1.8);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% System Time", 0.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% System Time", 2.2);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% System Time", 0.6666666666666666);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% System Time", 1);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% System Time", 0.4);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% System Time", 0.6666666666666666);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% System Time", 0.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% System Time", 0.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% System Time", 0.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% System Time", 0.6666666666666666);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% System Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% User Time", 19.2);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% User Time", 31.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% User Time", 5.75);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% User Time", 19.6);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% User Time", 6.666666666666667);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% User Time", 10);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% User Time", 5);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% User Time", 5.6);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% User Time", 19);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% User Time", 6.333333333333333);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% User Time", 5);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% User Time", 18);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% User Time", 5.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% User Time", 9);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% User Time", 7);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% User Time", 53);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IRQ Time", 0.2);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IRQ Time", 1.3333333333333333);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% Idle Time", 78.8);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% Idle Time", 68.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% Idle Time", 93.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% Idle Time", 78.2);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% Idle Time", 92);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% Idle Time", 89);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% Idle Time", 94);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% Idle Time", 94);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% Idle Time", 81);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% Idle Time", 92);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% Idle Time", 94.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% Idle Time", 81);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% Idle Time", 94.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% Idle Time", 90.5);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% Idle Time", 92.33333333333333);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% Idle Time", 47);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IOWait Time", 0.2);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IOWait Time", 0.6666666666666666);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", 4.694);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", 6.476000000000001);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", 6.492);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\CSwitches", 4083778.6);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Serviced Interrupts", 3219311);
            MetricAssert.Exists(metrics, @"\Memory\Total Bytes", 67430986547.2);
            MetricAssert.Exists(metrics, @"\Memory\Free Bytes", 57574036602.880005);
            MetricAssert.Exists(metrics, @"\Memory\Cached Bytes", 4724464025.6);
            MetricAssert.Exists(metrics, @"\Memory\Buffer Bytes", 255328256);
            MetricAssert.Exists(metrics, @"\Memory\Kernel Bytes", 759504568.3199999);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Total Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Free Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Committed Bytes", 9835475107.84);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Limit Bytes", 33715493273.6);
            MetricAssert.Exists(metrics, @"\Memory\Page Scans", 211051);
            MetricAssert.Exists(metrics, @"\Memory\Page Steals", 206063);
            MetricAssert.Exists(metrics, @"\Memory\Page Reclaims", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Reads", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\% Busy Time", 6);
            MetricAssert.Exists(metrics, @"\Disk\Avg. % Busy Time", 3);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Reads", 44908.5);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Reads", 315);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Reads", 315);
            MetricAssert.Exists(metrics, @"\Disk\# Reads", 18089.4);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Writes", 195332.5);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk\# Writes", 78133);
            MetricAssert.Exists(metrics, @"\Disk(sda)\Avg. Request Time", 0.38);
            MetricAssert.Exists(metrics, @"\Disk\Avg. Request Time", 1.2149999999999999);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\Avg. Request Time", 2.06);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\Avg. Request Time", 2.04);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Received", 21073.8);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Transmitted", 33993.8);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Received", 1319.2);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Transmitted", 1320.4);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Received", 22396.6);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Transmitted", 19055.2);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Forwarded", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Delivered", 22395);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network\Avg. % Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(eth0)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Received", 85865);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Received", 19358);
            MetricAssert.Exists(metrics, @"\Network\Packets Received", 105223);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Transmitted", 32129.2);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Transmitted", 15786.8);
            MetricAssert.Exists(metrics, @"\Network\Packets Transmitted", 47916);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Received", 115.4);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Received", 113);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Received", 110.6);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Transmitted", 213);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Transmitted", 210.7);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Transmitted", 208.4);

        }

        [Test]
        public void AtopParserParsesMetricsCorrectly_Scenario4()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Atop", "AtopExample-1s-60s.txt");
            string rawText = File.ReadAllText(outputPath);

            // 1 second sample rate, 60 intervals = 60 distinct sample groups
            AtopParser testParser = new AtopParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(158, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 9.75);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 3);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Max", 21);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Median", 9.5);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time", 97.4);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Min", 29);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Max", 284);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% User Time Median", 101);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time", 2.283333333333333);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Max", 12);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IRQ Time Median", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time", 1499.4);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Min", 1305);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Max", 1577);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% Idle Time Median", 1493.5);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time", 1.1833333333333333);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Min", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Max", 27);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% IOWait Time Median", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% System Time", 1.0227272727272727);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% System Time", 0.6904761904761905);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% System Time", 0.9148936170212766);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% System Time", 0.2894736842105263);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% System Time", 0.975);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% System Time", 1.490566037735849);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% System Time", 1.7272727272727273);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% System Time", 1.18);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% System Time", 0.7368421052631579);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% System Time", 1.5789473684210527);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% System Time", 0.8181818181818182);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% System Time", 0.75);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% System Time", 0.5882352941176471);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% System Time", 1.565217391304348);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% System Time", 0.7058823529411765);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% System Time", 0.6);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% User Time", 16.113636363636363);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% User Time", 17.738095238095237);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% User Time", 10.297872340425531);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% User Time", 1.263157894736842);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% User Time", 11.9);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% User Time", 23.245283018867923);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% User Time", 10.136363636363637);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% User Time", 2);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% User Time", 5.157894736842105);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% User Time", 1.2456140350877194);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% User Time", 2.8181818181818183);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% User Time", 13.944444444444445);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% User Time", 8.941176470588236);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% User Time", 8.347826086956522);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% User Time", 12.764705882352942);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% User Time", 19.733333333333334);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IRQ Time", 0.06818181818181818);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IRQ Time", 0.7894736842105263);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IRQ Time", 0.018867924528301886);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IRQ Time", 0.3157894736842105);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IRQ Time", 1.6491228070175439);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% IRQ Time", 0.027777777777777776);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IRQ Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% Idle Time", 82.77272727272727);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% Idle Time", 81.57142857142857);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% Idle Time", 88.7872340425532);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% Idle Time", 97.63157894736842);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% Idle Time", 86.95);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% Idle Time", 75.15094339622641);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% Idle Time", 87.72727272727273);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% Idle Time", 96.7);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% Idle Time", 93.78947368421052);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% Idle Time", 95.56140350877193);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% Idle Time", 96.27272727272727);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% Idle Time", 85.25);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% Idle Time", 90.05882352941177);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% Idle Time", 89.21739130434783);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% Idle Time", 86.52941176470588);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% Idle Time", 79.66666666666667);
            MetricAssert.Exists(metrics, @"\Processor(cpu002)\% IOWait Time", 0.022727272727272728);
            MetricAssert.Exists(metrics, @"\Processor(cpu010)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu014)\% IOWait Time", 0.02127659574468085);
            MetricAssert.Exists(metrics, @"\Processor(cpu001)\% IOWait Time", 0.02631578947368421);
            MetricAssert.Exists(metrics, @"\Processor(cpu006)\% IOWait Time", 0.175);
            MetricAssert.Exists(metrics, @"\Processor(cpu012)\% IOWait Time", 0.09433962264150944);
            MetricAssert.Exists(metrics, @"\Processor(cpu008)\% IOWait Time", 0.45454545454545453);
            MetricAssert.Exists(metrics, @"\Processor(cpu004)\% IOWait Time", 0.12);
            MetricAssert.Exists(metrics, @"\Processor(cpu003)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu000)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu007)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu005)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu011)\% IOWait Time", 0.35294117647058826);
            MetricAssert.Exists(metrics, @"\Processor(cpu009)\% IOWait Time", 0.8695652173913043);
            MetricAssert.Exists(metrics, @"\Processor(cpu013)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor(cpu015)\% IOWait Time", 0);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", 5.460666666666664);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", 6.865166666666668);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", 6.499500000000003);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\CSwitches", 316701);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\Serviced Interrupts", 252678.95);
            MetricAssert.Exists(metrics, @"\Memory\Total Bytes", 67430986547.20007);
            MetricAssert.Exists(metrics, @"\Memory\Free Bytes", 57650988100.26663);
            MetricAssert.Exists(metrics, @"\Memory\Cached Bytes", 4724464025.600003);
            MetricAssert.Exists(metrics, @"\Memory\Buffer Bytes", 254968244.90666676);
            MetricAssert.Exists(metrics, @"\Memory\Kernel Bytes", 757697522.3466661);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Total Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Free Bytes", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Committed Bytes", 9767471458.986666);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Virtual Limit Bytes", 33715493273.600037);
            MetricAssert.Exists(metrics, @"\Memory\Page Scans", 39575);
            MetricAssert.Exists(metrics, @"\Memory\Page Steals", 38577.4);
            MetricAssert.Exists(metrics, @"\Memory\Page Reclaims", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Reads", 0);
            MetricAssert.Exists(metrics, @"\Memory\Swap Space Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\% Busy Time", 2.9655172413793105);
            MetricAssert.Exists(metrics, @"\Disk\Avg. % Busy Time", 2.774193548387097);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\% Busy Time", 0);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Reads", 3084.5172413793102);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Reads", 315);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Reads", 315);
            MetricAssert.Exists(metrics, @"\Disk\# Reads", 1501.35);
            MetricAssert.Exists(metrics, @"\Disk(sda)\# Writes", 12881);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\# Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\# Writes", 0);
            MetricAssert.Exists(metrics, @"\Disk\# Writes", 6225.816666666667);
            MetricAssert.Exists(metrics, @"\Disk(sda)\Avg. Request Time", 4.017241379310345);
            MetricAssert.Exists(metrics, @"\Disk\Avg. Request Time", 3.8903225806451616);
            MetricAssert.Exists(metrics, @"\Disk(sdb)\Avg. Request Time", 2.06);
            MetricAssert.Exists(metrics, @"\Disk(sdc)\Avg. Request Time", 2.04);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Received", 1733.0833333333333);
            MetricAssert.Exists(metrics, @"\Network\TCP Segments Transmitted", 2779.133333333333);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Received", 94.6);
            MetricAssert.Exists(metrics, @"\Network\UDP Segments Transmitted", 94.7);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Received", 1827.9833333333333);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Transmitted", 1549.1833333333334);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Forwarded", 0);
            MetricAssert.Exists(metrics, @"\Network\IP Datagrams Delivered", 1827.85);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network\Avg. % Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(eth0)\% Usage", 0);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Received", 7134.466666666666);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Received", 1585.7666666666667);
            MetricAssert.Exists(metrics, @"\Network\Packets Received", 8720.233333333334);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\Packets Transmitted", 2619.8333333333335);
            MetricAssert.Exists(metrics, @"\Network(eth0)\Packets Transmitted", 1288.7833333333333);
            MetricAssert.Exists(metrics, @"\Network\Packets Transmitted", 3908.616666666667);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Received", 11.533333333333333);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Received", 11.75);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Received", 11.966666666666667);
            MetricAssert.Exists(metrics, @"\Network(enP4560)\KB/sec Transmitted", 26.966666666666665);
            MetricAssert.Exists(metrics, @"\Network\Avg. KB/sec Transmitted", 26.7);
            MetricAssert.Exists(metrics, @"\Network(eth0)\KB/sec Transmitted", 26.433333333333334);

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

            Assert.AreEqual(158, metrics.Count);

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

            Assert.AreEqual(158, metrics.Count);

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