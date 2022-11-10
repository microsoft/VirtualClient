using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class SuperBenchmarkMetricsParserTests
    {
        private string rawText;
        private SuperBenchmarkMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "SuperBenchmark", "Example1");
            }
        }

        [Test]
        public void SuperBenchmarkParserHandlesMetricsThatAreNotFormattedAsNumericValues()
        {
            string outputPath = Path.Combine(this.ExamplePath, "results-summary.jsonl");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SuperBenchmarkMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            // The example itself contains non-numeric values.
            // If the test doesn't throw, it means the parser handles non-numeric values.
        }

        [Test]
        public void SuperBenchmarkParserVerifyMetricsFpRate()
        {
            string outputPath = Path.Combine(this.ExamplePath, "results-summary.jsonl");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SuperBenchmarkMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(48, metrics.Count);
            MetricAssert.Exists(metrics, "bert_models/pytorch-bert-base/steptime_train_float32", 358.4588002413511);
            MetricAssert.Exists(metrics, "bert_models/pytorch-bert-base/throughput_train_float32", 5.579473305201674);
            MetricAssert.Exists(metrics, "bert_models/pytorch-bert-base/steptime_train_float16", 283.52972492575645);
            MetricAssert.Exists(metrics, "bert_models/pytorch-bert-base/throughput_train_float16", 7.054074744913095);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet169/steptime_train_float32", 606.3345931470394);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet169/throughput_train_float32", 52.778107941969836);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet169/steptime_train_float16", 584.1372180730104);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet169/throughput_train_float16", 54.787888878995155);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet201/steptime_train_float32", 768.3062478899956);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet201/throughput_train_float32", 41.65103284898928);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet201/steptime_train_float16", 768.8198536634445);
            MetricAssert.Exists(metrics, "densenet_models/pytorch-densenet201/throughput_train_float16", 41.62643300788471);
            MetricAssert.Exists(metrics, "gpt_models/pytorch-gpt2-small/steptime_train_float32", 269.8045689612627);
            MetricAssert.Exists(metrics, "gpt_models/pytorch-gpt2-small/throughput_train_float32", 3.706402389625931);
            MetricAssert.Exists(metrics, "gpt_models/pytorch-gpt2-small/steptime_train_float16", 200.27858577668667);
            MetricAssert.Exists(metrics, "gpt_models/pytorch-gpt2-small/throughput_train_float16", 4.993109815562972);
            MetricAssert.Exists(metrics, "lstm_models/pytorch-lstm/steptime_train_float32", 2516.963880509138);
            MetricAssert.Exists(metrics, "lstm_models/pytorch-lstm/throughput_train_float32", 12.716246516184963);
            MetricAssert.Exists(metrics, "lstm_models/pytorch-lstm/steptime_train_float16", 2367.4698639661074);
            MetricAssert.Exists(metrics, "lstm_models/pytorch-lstm/throughput_train_float16", 13.520498344991013);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet50/steptime_train_float32", 448.0651281774044);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet50/throughput_train_float32", 71.44562632866294);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet50/steptime_train_float16", 370.6081360578537);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet50/throughput_train_float16", 86.34836306194934);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet101/steptime_train_float32", 704.9580551683903);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet101/throughput_train_float32", 45.39960789780254);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet101/steptime_train_float16", 616.4275892078876);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet101/throughput_train_float16", 51.9140916952445);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet152/steptime_train_float32", 987.4175880104303);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet152/throughput_train_float32", 32.41080970363987);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet152/steptime_train_float16", 873.4863679856062);
            MetricAssert.Exists(metrics, "resnet_models/pytorch-resnet152/throughput_train_float16", 36.63563863106791);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg11/steptime_train_float32", 396.5049795806408);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg11/throughput_train_float32", 81.94847328609806);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg11/steptime_train_float16", 314.3566381186247);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg11/throughput_train_float16", 105.62904119514452);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg13/steptime_train_float32", 610.694183036685);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg13/throughput_train_float32", 52.644771434913025);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg13/steptime_train_float16", 530.2897151559591);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg13/throughput_train_float16", 60.793150178968254);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg16/steptime_train_float32", 719.8754735291004);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg16/throughput_train_float32", 44.64957917938);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg16/steptime_train_float16", 620.6783652305603);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg16/throughput_train_float16", 51.87553593187532);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg19/steptime_train_float32", 824.2602981626987);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg19/throughput_train_float32", 38.958361509957555);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg19/steptime_train_float16", 709.9955454468727);
            MetricAssert.Exists(metrics, "vgg_models/pytorch-vgg19/throughput_train_float16", 45.25589424677416);
        }
    }
}