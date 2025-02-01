// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class GeekBenchParserUnitTests
    {
        private string rawText;
        private GeekBenchMetricsParser testParser;
        private static string workingDirectory;

        [SetUp]
        public void Setup()
        {
            GeekBenchParserUnitTests.workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [Test]
        public void GeekBenchParserVerifySingleCoreResult()
        {
            Assert.AreEqual(5, this.testParser.SingleCoreResult.Columns.Count);
        }

        [Test]
        public void GeekBench5ParserVerifyMetricsSingleCore()
        {
            string outputPath = Path.Combine(workingDirectory, "Examples", "Geekbench", "GeekBench5Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();
                      
            MetricAssert.Exists(metrics, "SingleCore-AES-XTS", 1.86, "GB/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-AES-XTS", 1091, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Text Compression", 4.21, "MB/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Text Compression", 833, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Image Compression", 43.1, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Image Compression", 912, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Navigation", 1.94, "MTE/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Navigation", 687, "Score");
            MetricAssert.Exists(metrics, "SingleCore-HTML5", 913.3, "KElements/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-HTML5", 778, "Score");
            MetricAssert.Exists(metrics, "SingleCore-SQLite", 239.9, "Krows/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-SQLite", 766, "Score");
            MetricAssert.Exists(metrics, "SingleCore-PDF Rendering", 40.1, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-PDF Rendering", 738, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Text Rendering", 236.2, "KB/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Text Rendering", 741, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Clang", 6.48, "Klines/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Clang", 832, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Camera", 8.43, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Camera", 727, "Score");
            MetricAssert.Exists(metrics, "SingleCore-N-Body Physics", 970.6, "Kpairs/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-N-Body Physics", 776, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Rigid Body Physics", 5373.6, "FPS");
            MetricAssert.Exists(metrics, "SingleCoreScore-Rigid Body Physics", 867, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Gaussian Blur", 34.7, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Gaussian Blur", 632, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Face Detection", 5.97, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Face Detection", 776, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Horizon Detection", 18.0, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Horizon Detection", 731, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Image Inpainting", 70.7, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Image Inpainting", 1441, "Score");
            MetricAssert.Exists(metrics, "SingleCore-HDR", 22.0, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-HDR", 1613, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Ray Tracing", 877.6, "Kpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Ray Tracing", 1093, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Structure from Motion", 6.86, "Kpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Structure from Motion", 766, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Speech Recognition", 25.2, "Words/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Speech Recognition", 789, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Machine Learning", 33.2, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Machine Learning", 860, "Score");
        }

        [Test]
        public void GeekBench5ParserVerifyMetricsMultiCore()
        {
            string outputPath = Path.Combine(workingDirectory, "Examples", "Geekbench", "GeekBench5Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "MultiCore-AES-XTS", 27.6, "GB/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-AES-XTS", 16208, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Text Compression", 87.7, "MB/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Text Compression", 17339, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Image Compression", 1.24, "Gpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Image Compression", 26195, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Navigation", 2.61, "MTE/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Navigation", 925, "Score");
            MetricAssert.Exists(metrics, "MultiCore-HTML5", 18.6, "MElements/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-HTML5", 15856, "Score");
            MetricAssert.Exists(metrics, "MultiCore-SQLite", 6.17, "Mrows/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-SQLite", 19683, "Score");
            MetricAssert.Exists(metrics, "MultiCore-PDF Rendering", 372.4, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-PDF Rendering", 6862, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Text Rendering", 3.43, "MB/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Text Rendering", 11022, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Clang", 177.2, "Klines/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Clang", 22741, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Camera", 81.0, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Camera", 6985, "Score");
            MetricAssert.Exists(metrics, "MultiCore-N-Body Physics", 13.3, "Mpairs/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-N-Body Physics", 10610, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Rigid Body Physics", 174257.9, "FPS");
            MetricAssert.Exists(metrics, "MultiCoreScore-Rigid Body Physics", 28127, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Gaussian Blur", 1.03, "Gpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Gaussian Blur", 18741, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Face Detection", 162.6, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Face Detection", 21121, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Horizon Detection", 229.1, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Horizon Detection", 9293, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Image Inpainting", 908.4, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Image Inpainting", 18517, "Score");
            MetricAssert.Exists(metrics, "MultiCore-HDR", 271.4, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-HDR", 19999, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Ray Tracing", 19.1, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Ray Tracing", 23841, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Structure from Motion", 54.7, "Kpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Structure from Motion", 6111, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Speech Recognition", 453.5, "Words/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Speech Recognition", 14186, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Machine Learning", 284.4, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Machine Learning", 7360, "Score");
        }

        [Test]
        public void GeekBench5ParserVerifyMetricsSummary()
        {
            string outputPath = Path.Combine(workingDirectory, "Examples", "Geekbench", "GeekBench5Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            // 21 Single/MultiCore Score/RawScore + 8 summary = 21*2*2+8=92
            Assert.AreEqual(92, metrics.Count);

            MetricAssert.Exists(metrics, "SingleCoreSummary-Single-Core Score", 888, "Score");
            MetricAssert.Exists(metrics, "SingleCoreSummary-Integer Score", 777, "Score");
            MetricAssert.Exists(metrics, "SingleCoreSummary-Floating Point Score", 901, "Score");
            MetricAssert.Exists(metrics, "MultiCoreSummary-Multi-Core Score", 12345, "Score");
            MetricAssert.Exists(metrics, "MultiCoreSummary-Integer Score", 10518, "Score");
            MetricAssert.Exists(metrics, "MultiCoreSummary-Floating Point Score", 14544, "Score");
        }

        [Test]
        public void GeekBench6ParserVerifyMetricsSingleCore()
        {
            string outputPath = Path.Combine(workingDirectory, "Examples", "Geekbench", "GeekBench6Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "SingleCore-File Compression", 249.1, "MB/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-File Compression", 1734, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Navigation", 10.9, "routes/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Navigation", 1807, "Score");
            MetricAssert.Exists(metrics, "SingleCore-HTML5 Browser", 38.1, "pages/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-HTML5 Browser", 1859, "Score");
            MetricAssert.Exists(metrics, "SingleCore-PDF Renderer", 16.6, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-PDF Renderer", 719, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Photo Library", 9.23, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Photo Library", 680, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Clang", 3.32, "Klines/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Clang", 675, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Text Processing", 53.6, "pages/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Text Processing", 669, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Asset Compression", 37.4, "MB/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Asset Compression", 1208, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Object Detection", 58.2, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Object Detection", 1946, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Background Blur", 9.43, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Background Blur", 2278, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Horizon Detection", 78.3, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Horizon Detection", 2515, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Object Remover", 112.9, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Object Remover", 1468, "Score");
            MetricAssert.Exists(metrics, "SingleCore-HDR", 54.2, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-HDR", 1847, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Photo Filter", 23.7, "images/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Photo Filter", 2384, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Ray Tracer", 1.52, "Mpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Ray Tracer", 1570, "Score");
            MetricAssert.Exists(metrics, "SingleCore-Structure from Motion", 67.2, "Kpixels/sec");
            MetricAssert.Exists(metrics, "SingleCoreScore-Structure from Motion", 2122, "Score");
        }

        [Test]
        public void GeekBench6ParserVerifyMetricsMultiCore()
        {
            string outputPath = Path.Combine(workingDirectory, "Examples", "Geekbench", "GeekBench6Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "MultiCore-File Compression", 771.9, "MB/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-File Compression", 5375, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Navigation", 45.2, "routes/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Navigation", 7496, "Score");
            MetricAssert.Exists(metrics, "MultiCore-HTML5 Browser", 146.1, "pages/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-HTML5 Browser", 7139, "Score");
            MetricAssert.Exists(metrics, "MultiCore-PDF Renderer", 173.3, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-PDF Renderer", 7515, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Photo Library", 103.5, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Photo Library", 7629, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Clang", 36.0, "Klines/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Clang", 7305, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Text Processing", 183.8, "pages/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Text Processing", 2295, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Asset Compression", 251.8, "MB/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Asset Compression", 8125, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Object Detection", 149.8, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Object Detection", 5007, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Background Blur", 28.4, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Background Blur", 6874, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Horizon Detection", 290.1, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Horizon Detection", 9322, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Object Remover", 416.7, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Object Remover", 5420, "Score");
            MetricAssert.Exists(metrics, "MultiCore-HDR", 200.6, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-HDR", 6835, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Photo Filter", 76.1, "images/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Photo Filter", 7673, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Ray Tracer", 7.01, "Mpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Ray Tracer", 7243, "Score");
            MetricAssert.Exists(metrics, "MultiCore-Structure from Motion", 239.5, "Kpixels/sec");
            MetricAssert.Exists(metrics, "MultiCoreScore-Structure from Motion", 7565, "Score");
        }

        [Test]
        public void GeekBench6ParserVerifyMetricsSummary()
        {
            string outputPath = Path.Combine(workingDirectory, "Examples", "Geekbench", "GeekBench6Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            // 16 Single/MultiCore Score/RawScore + 8 summary = 16*2*2+6=70
            Assert.AreEqual(70, metrics.Count);

            MetricAssert.Exists(metrics, "SingleCoreSummary-Single-Core Score", 888, "Score");
            MetricAssert.Exists(metrics, "SingleCoreSummary-Integer Score", 777, "Score");
            MetricAssert.Exists(metrics, "SingleCoreSummary-Floating Point Score", 901, "Score");
            MetricAssert.Exists(metrics, "MultiCoreSummary-Multi-Core Score", 12345, "Score");
            MetricAssert.Exists(metrics, "MultiCoreSummary-Integer Score", 10518, "Score");
            MetricAssert.Exists(metrics, "MultiCoreSummary-Floating Point Score", 14544, "Score");
        }
    }
}