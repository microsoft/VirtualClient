// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
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

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Geekbench\GeekBenchExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GeekBenchMetricsParser(this.rawText);
            this.testParser.Parse();
        }

        [Test]
        public void GeekBenchParserVerifySingleCoreResult()
        {
            this.testParser.SingleCoreResult.PrintDataTableFormatted();

            Assert.AreEqual(5, this.testParser.SingleCoreResult.Columns.Count);
        }

        [Test]
        public void GeekBenchParserVerifyMetricsSingleCore()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(1.86, metrics.Where(m => m.Name == "SingleCore-AES-XTS").FirstOrDefault().Value);
            Assert.AreEqual("GB/sec", metrics.Where(m => m.Name == "SingleCore-AES-XTS").FirstOrDefault().Unit);
            Assert.AreEqual(1091, metrics.Where(m => m.Name == "SingleCoreScore-AES-XTS").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-AES-XTS").FirstOrDefault().Unit);
            Assert.AreEqual(4.21, metrics.Where(m => m.Name == "SingleCore-Text Compression").FirstOrDefault().Value);
            Assert.AreEqual("MB/sec", metrics.Where(m => m.Name == "SingleCore-Text Compression").FirstOrDefault().Unit);
            Assert.AreEqual(833, metrics.Where(m => m.Name == "SingleCoreScore-Text Compression").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Text Compression").FirstOrDefault().Unit);
            Assert.AreEqual(43.1, metrics.Where(m => m.Name == "SingleCore-Image Compression").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "SingleCore-Image Compression").FirstOrDefault().Unit);
            Assert.AreEqual(912, metrics.Where(m => m.Name == "SingleCoreScore-Image Compression").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Image Compression").FirstOrDefault().Unit);
            Assert.AreEqual(1.94, metrics.Where(m => m.Name == "SingleCore-Navigation").FirstOrDefault().Value);
            Assert.AreEqual("MTE/sec", metrics.Where(m => m.Name == "SingleCore-Navigation").FirstOrDefault().Unit);
            Assert.AreEqual(687, metrics.Where(m => m.Name == "SingleCoreScore-Navigation").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Navigation").FirstOrDefault().Unit);
            Assert.AreEqual(913.3, metrics.Where(m => m.Name == "SingleCore-HTML5").FirstOrDefault().Value);
            Assert.AreEqual("KElements/sec", metrics.Where(m => m.Name == "SingleCore-HTML5").FirstOrDefault().Unit);
            Assert.AreEqual(778, metrics.Where(m => m.Name == "SingleCoreScore-HTML5").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-HTML5").FirstOrDefault().Unit);
            Assert.AreEqual(239.9, metrics.Where(m => m.Name == "SingleCore-SQLite").FirstOrDefault().Value);
            Assert.AreEqual("Krows/sec", metrics.Where(m => m.Name == "SingleCore-SQLite").FirstOrDefault().Unit);
            Assert.AreEqual(766, metrics.Where(m => m.Name == "SingleCoreScore-SQLite").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-SQLite").FirstOrDefault().Unit);
            Assert.AreEqual(40.1, metrics.Where(m => m.Name == "SingleCore-PDF Rendering").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "SingleCore-PDF Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(738, metrics.Where(m => m.Name == "SingleCoreScore-PDF Rendering").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-PDF Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(236.2, metrics.Where(m => m.Name == "SingleCore-Text Rendering").FirstOrDefault().Value);
            Assert.AreEqual("KB/sec", metrics.Where(m => m.Name == "SingleCore-Text Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(741, metrics.Where(m => m.Name == "SingleCoreScore-Text Rendering").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Text Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(6.48, metrics.Where(m => m.Name == "SingleCore-Clang").FirstOrDefault().Value);
            Assert.AreEqual("Klines/sec", metrics.Where(m => m.Name == "SingleCore-Clang").FirstOrDefault().Unit);
            Assert.AreEqual(832, metrics.Where(m => m.Name == "SingleCoreScore-Clang").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Clang").FirstOrDefault().Unit);
            Assert.AreEqual(8.43, metrics.Where(m => m.Name == "SingleCore-Camera").FirstOrDefault().Value);
            Assert.AreEqual("images/sec", metrics.Where(m => m.Name == "SingleCore-Camera").FirstOrDefault().Unit);
            Assert.AreEqual(727, metrics.Where(m => m.Name == "SingleCoreScore-Camera").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Camera").FirstOrDefault().Unit);
            Assert.AreEqual(970.6, metrics.Where(m => m.Name == "SingleCore-N-Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("Kpairs/sec", metrics.Where(m => m.Name == "SingleCore-N-Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(776, metrics.Where(m => m.Name == "SingleCoreScore-N-Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-N-Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(5373.6, metrics.Where(m => m.Name == "SingleCore-Rigid Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("FPS", metrics.Where(m => m.Name == "SingleCore-Rigid Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(867, metrics.Where(m => m.Name == "SingleCoreScore-Rigid Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Rigid Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(34.7, metrics.Where(m => m.Name == "SingleCore-Gaussian Blur").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "SingleCore-Gaussian Blur").FirstOrDefault().Unit);
            Assert.AreEqual(632, metrics.Where(m => m.Name == "SingleCoreScore-Gaussian Blur").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Gaussian Blur").FirstOrDefault().Unit);
            Assert.AreEqual(5.97, metrics.Where(m => m.Name == "SingleCore-Face Detection").FirstOrDefault().Value);
            Assert.AreEqual("images/sec", metrics.Where(m => m.Name == "SingleCore-Face Detection").FirstOrDefault().Unit);
            Assert.AreEqual(776, metrics.Where(m => m.Name == "SingleCoreScore-Face Detection").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Face Detection").FirstOrDefault().Unit);
            Assert.AreEqual(18.0, metrics.Where(m => m.Name == "SingleCore-Horizon Detection").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "SingleCore-Horizon Detection").FirstOrDefault().Unit);
            Assert.AreEqual(731, metrics.Where(m => m.Name == "SingleCoreScore-Horizon Detection").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Horizon Detection").FirstOrDefault().Unit);
            Assert.AreEqual(70.7, metrics.Where(m => m.Name == "SingleCore-Image Inpainting").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "SingleCore-Image Inpainting").FirstOrDefault().Unit);
            Assert.AreEqual(1441, metrics.Where(m => m.Name == "SingleCoreScore-Image Inpainting").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Image Inpainting").FirstOrDefault().Unit);
            Assert.AreEqual(22.0, metrics.Where(m => m.Name == "SingleCore-HDR").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "SingleCore-HDR").FirstOrDefault().Unit);
            Assert.AreEqual(1613, metrics.Where(m => m.Name == "SingleCoreScore-HDR").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-HDR").FirstOrDefault().Unit);
            Assert.AreEqual(877.6, metrics.Where(m => m.Name == "SingleCore-Ray Tracing").FirstOrDefault().Value);
            Assert.AreEqual("Kpixels/sec", metrics.Where(m => m.Name == "SingleCore-Ray Tracing").FirstOrDefault().Unit);
            Assert.AreEqual(1093, metrics.Where(m => m.Name == "SingleCoreScore-Ray Tracing").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Ray Tracing").FirstOrDefault().Unit);
            Assert.AreEqual(6.86, metrics.Where(m => m.Name == "SingleCore-Structure from Motion").FirstOrDefault().Value);
            Assert.AreEqual("Kpixels/sec", metrics.Where(m => m.Name == "SingleCore-Structure from Motion").FirstOrDefault().Unit);
            Assert.AreEqual(766, metrics.Where(m => m.Name == "SingleCoreScore-Structure from Motion").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Structure from Motion").FirstOrDefault().Unit);
            Assert.AreEqual(25.2, metrics.Where(m => m.Name == "SingleCore-Speech Recognition").FirstOrDefault().Value);
            Assert.AreEqual("Words/sec", metrics.Where(m => m.Name == "SingleCore-Speech Recognition").FirstOrDefault().Unit);
            Assert.AreEqual(789, metrics.Where(m => m.Name == "SingleCoreScore-Speech Recognition").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Speech Recognition").FirstOrDefault().Unit);
            Assert.AreEqual(33.2, metrics.Where(m => m.Name == "SingleCore-Machine Learning").FirstOrDefault().Value);
            Assert.AreEqual("images/sec", metrics.Where(m => m.Name == "SingleCore-Machine Learning").FirstOrDefault().Unit);
            Assert.AreEqual(860, metrics.Where(m => m.Name == "SingleCoreScore-Machine Learning").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreScore-Machine Learning").FirstOrDefault().Unit);
        }

        [Test]
        public void GeekBenchParserVerifyMetricsMultiCore()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(27.6, metrics.Where(m => m.Name == "MultiCore-AES-XTS").FirstOrDefault().Value);
            Assert.AreEqual("GB/sec", metrics.Where(m => m.Name == "MultiCore-AES-XTS").FirstOrDefault().Unit);
            Assert.AreEqual(16208, metrics.Where(m => m.Name == "MultiCoreScore-AES-XTS").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-AES-XTS").FirstOrDefault().Unit);
            Assert.AreEqual(87.7, metrics.Where(m => m.Name == "MultiCore-Text Compression").FirstOrDefault().Value);
            Assert.AreEqual("MB/sec", metrics.Where(m => m.Name == "MultiCore-Text Compression").FirstOrDefault().Unit);
            Assert.AreEqual(17339, metrics.Where(m => m.Name == "MultiCoreScore-Text Compression").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Text Compression").FirstOrDefault().Unit);
            Assert.AreEqual(1.24, metrics.Where(m => m.Name == "MultiCore-Image Compression").FirstOrDefault().Value);
            Assert.AreEqual("Gpixels/sec", metrics.Where(m => m.Name == "MultiCore-Image Compression").FirstOrDefault().Unit);
            Assert.AreEqual(26195, metrics.Where(m => m.Name == "MultiCoreScore-Image Compression").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Image Compression").FirstOrDefault().Unit);
            Assert.AreEqual(2.61, metrics.Where(m => m.Name == "MultiCore-Navigation").FirstOrDefault().Value);
            Assert.AreEqual("MTE/sec", metrics.Where(m => m.Name == "MultiCore-Navigation").FirstOrDefault().Unit);
            Assert.AreEqual(925, metrics.Where(m => m.Name == "MultiCoreScore-Navigation").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Navigation").FirstOrDefault().Unit);
            Assert.AreEqual(18.6, metrics.Where(m => m.Name == "MultiCore-HTML5").FirstOrDefault().Value);
            Assert.AreEqual("MElements/sec", metrics.Where(m => m.Name == "MultiCore-HTML5").FirstOrDefault().Unit);
            Assert.AreEqual(15856, metrics.Where(m => m.Name == "MultiCoreScore-HTML5").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-HTML5").FirstOrDefault().Unit);
            Assert.AreEqual(6.17, metrics.Where(m => m.Name == "MultiCore-SQLite").FirstOrDefault().Value);
            Assert.AreEqual("Mrows/sec", metrics.Where(m => m.Name == "MultiCore-SQLite").FirstOrDefault().Unit);
            Assert.AreEqual(19683, metrics.Where(m => m.Name == "MultiCoreScore-SQLite").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-SQLite").FirstOrDefault().Unit);
            Assert.AreEqual(372.4, metrics.Where(m => m.Name == "MultiCore-PDF Rendering").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "MultiCore-PDF Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(6862, metrics.Where(m => m.Name == "MultiCoreScore-PDF Rendering").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-PDF Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(3.43, metrics.Where(m => m.Name == "MultiCore-Text Rendering").FirstOrDefault().Value);
            Assert.AreEqual("MB/sec", metrics.Where(m => m.Name == "MultiCore-Text Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(11022, metrics.Where(m => m.Name == "MultiCoreScore-Text Rendering").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Text Rendering").FirstOrDefault().Unit);
            Assert.AreEqual(177.2, metrics.Where(m => m.Name == "MultiCore-Clang").FirstOrDefault().Value);
            Assert.AreEqual("Klines/sec", metrics.Where(m => m.Name == "MultiCore-Clang").FirstOrDefault().Unit);
            Assert.AreEqual(22741, metrics.Where(m => m.Name == "MultiCoreScore-Clang").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Clang").FirstOrDefault().Unit);
            Assert.AreEqual(81.0, metrics.Where(m => m.Name == "MultiCore-Camera").FirstOrDefault().Value);
            Assert.AreEqual("images/sec", metrics.Where(m => m.Name == "MultiCore-Camera").FirstOrDefault().Unit);
            Assert.AreEqual(6985, metrics.Where(m => m.Name == "MultiCoreScore-Camera").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Camera").FirstOrDefault().Unit);
            Assert.AreEqual(13.3, metrics.Where(m => m.Name == "MultiCore-N-Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("Mpairs/sec", metrics.Where(m => m.Name == "MultiCore-N-Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(10610, metrics.Where(m => m.Name == "MultiCoreScore-N-Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-N-Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(174257.9, metrics.Where(m => m.Name == "MultiCore-Rigid Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("FPS", metrics.Where(m => m.Name == "MultiCore-Rigid Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(28127, metrics.Where(m => m.Name == "MultiCoreScore-Rigid Body Physics").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Rigid Body Physics").FirstOrDefault().Unit);
            Assert.AreEqual(1.03, metrics.Where(m => m.Name == "MultiCore-Gaussian Blur").FirstOrDefault().Value);
            Assert.AreEqual("Gpixels/sec", metrics.Where(m => m.Name == "MultiCore-Gaussian Blur").FirstOrDefault().Unit);
            Assert.AreEqual(18741, metrics.Where(m => m.Name == "MultiCoreScore-Gaussian Blur").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Gaussian Blur").FirstOrDefault().Unit);
            Assert.AreEqual(162.6, metrics.Where(m => m.Name == "MultiCore-Face Detection").FirstOrDefault().Value);
            Assert.AreEqual("images/sec", metrics.Where(m => m.Name == "MultiCore-Face Detection").FirstOrDefault().Unit);
            Assert.AreEqual(21121, metrics.Where(m => m.Name == "MultiCoreScore-Face Detection").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Face Detection").FirstOrDefault().Unit);
            Assert.AreEqual(229.1, metrics.Where(m => m.Name == "MultiCore-Horizon Detection").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "MultiCore-Horizon Detection").FirstOrDefault().Unit);
            Assert.AreEqual(9293, metrics.Where(m => m.Name == "MultiCoreScore-Horizon Detection").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Horizon Detection").FirstOrDefault().Unit);
            Assert.AreEqual(908.4, metrics.Where(m => m.Name == "MultiCore-Image Inpainting").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "MultiCore-Image Inpainting").FirstOrDefault().Unit);
            Assert.AreEqual(18517, metrics.Where(m => m.Name == "MultiCoreScore-Image Inpainting").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Image Inpainting").FirstOrDefault().Unit);
            Assert.AreEqual(271.4, metrics.Where(m => m.Name == "MultiCore-HDR").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "MultiCore-HDR").FirstOrDefault().Unit);
            Assert.AreEqual(19999, metrics.Where(m => m.Name == "MultiCoreScore-HDR").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-HDR").FirstOrDefault().Unit);
            Assert.AreEqual(19.1, metrics.Where(m => m.Name == "MultiCore-Ray Tracing").FirstOrDefault().Value);
            Assert.AreEqual("Mpixels/sec", metrics.Where(m => m.Name == "MultiCore-Ray Tracing").FirstOrDefault().Unit);
            Assert.AreEqual(23841, metrics.Where(m => m.Name == "MultiCoreScore-Ray Tracing").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Ray Tracing").FirstOrDefault().Unit);
            Assert.AreEqual(54.7, metrics.Where(m => m.Name == "MultiCore-Structure from Motion").FirstOrDefault().Value);
            Assert.AreEqual("Kpixels/sec", metrics.Where(m => m.Name == "MultiCore-Structure from Motion").FirstOrDefault().Unit);
            Assert.AreEqual(6111, metrics.Where(m => m.Name == "MultiCoreScore-Structure from Motion").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Structure from Motion").FirstOrDefault().Unit);
            Assert.AreEqual(453.5, metrics.Where(m => m.Name == "MultiCore-Speech Recognition").FirstOrDefault().Value);
            Assert.AreEqual("Words/sec", metrics.Where(m => m.Name == "MultiCore-Speech Recognition").FirstOrDefault().Unit);
            Assert.AreEqual(14186, metrics.Where(m => m.Name == "MultiCoreScore-Speech Recognition").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Speech Recognition").FirstOrDefault().Unit);
            Assert.AreEqual(284.4, metrics.Where(m => m.Name == "MultiCore-Machine Learning").FirstOrDefault().Value);
            Assert.AreEqual("images/sec", metrics.Where(m => m.Name == "MultiCore-Machine Learning").FirstOrDefault().Unit);
            Assert.AreEqual(7360, metrics.Where(m => m.Name == "MultiCoreScore-Machine Learning").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreScore-Machine Learning").FirstOrDefault().Unit);
        }

        [Test]
        public void GeekBenchParserVerifyMetricsSummary()
        {
            IList<Metric> metrics = this.testParser.Parse();

            // 21 Single/MultiCore Score/RawScore + 8 summary = 21*2*2+8=92
            Assert.AreEqual(92, metrics.Count);

            Assert.AreEqual(888, metrics.Where(m => m.Name == "SingleCoreSummary-Single-Core Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreSummary-Single-Core Score").FirstOrDefault().Unit);
            Assert.AreEqual(1091, metrics.Where(m => m.Name == "SingleCoreSummary-Crypto Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreSummary-Crypto Score").FirstOrDefault().Unit);
            Assert.AreEqual(777, metrics.Where(m => m.Name == "SingleCoreSummary-Integer Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreSummary-Integer Score").FirstOrDefault().Unit);
            Assert.AreEqual(901, metrics.Where(m => m.Name == "SingleCoreSummary-Floating Point Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCoreSummary-Floating Point Score").FirstOrDefault().Unit);
            Assert.AreEqual(12345, metrics.Where(m => m.Name == "MultiCoreSummary-Multi-Core Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreSummary-Multi-Core Score").FirstOrDefault().Unit);
            Assert.AreEqual(16208, metrics.Where(m => m.Name == "MultiCoreSummary-Crypto Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreSummary-Crypto Score").FirstOrDefault().Unit);
            Assert.AreEqual(10518, metrics.Where(m => m.Name == "MultiCoreSummary-Integer Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreSummary-Integer Score").FirstOrDefault().Unit);
            Assert.AreEqual(14544, metrics.Where(m => m.Name == "MultiCoreSummary-Floating Point Score").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCoreSummary-Floating Point Score").FirstOrDefault().Unit);
        }
    }
}