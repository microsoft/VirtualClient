# SPECjvm
The SPECjvm® 2008 benchmark is a suite for measuring the performance of a Java Runtime Environment (JRE). 
It contains several real-life applications and benchmarks focusing on core Java functionality. 
It is SPEC's first Java Virtual Machine benchmark which focuses on the performance of the JRE executing a single application.
It reflects the performance of the hardware processor and memory subsystem but has low dependence on file I/O and includes no network I/O across machines.

* [SPECjvm Documentation](https://www.spec.org/jvm2008/)  
* [User Guide](https://www.spec.org/jvm2008/docs/UserGuide.html)
* [Benchmarks](https://www.spec.org/jvm2008/docs/benchmarks/index.html)

## What is Being Measured?
The following benchmarks are supported by the SPECjvm suite of workloads. Howevder, due to the evolvement of JDK, some of the benchmarks marked as "Not supported" in SPECjvm 2008 are no longer compatible 
with the latest JDK and cannot be onboarded. These benchmarks are mentioned here for the sake of clarity.

* **Compress**  
  This benchmark compresses data, using a modified Lempel-Ziv method (LZW). Basically finds common substrings and replaces them with a variable size code. This is deterministic, and can be done on the fly. 
  Thus, the decompression procedure needs no input table, but tracks the way the table was built. Algorithm from "A Technique for High Performance Data Compression", Terry A. Welch, IEEE Computer Vol 17, No 6 (June 1984), pp 8-19.

* **Crypto**  
  This benchmark focuses on different areas of crypto and are split in three different sub-benchmarks. The different benchmarks use the implementation inside the product and will therefore focus on both the vendor 
  implementation of the protocol as well as how it is executed.

* **Derby**  
  This benchmark uses an open-source database written in pure Java. It is synthesized with business logic to stress the BigDecimal library. It is a direct replacement to the SPECjvm98 db benchmark but is more capable 
  and represents as close to a "real" application. The focus of this benchmark is on BigDecimal computations (based on telco benchmark) and database logic, especially, on locks behavior. BigDecimal computations are trying to 
  be outside 64-bit to examine not only 'simple' BigDecimal, where 'long' is used often for internal representation.

* **MPEGaudio**  
  This benchmark is very similar to the SPECjvm98 mpegaudio. The mp3 library has been replaced with JLayer, an LGPL mp3 library. Its floating-point heavy and a good test of mp3 decoding. Input data were taken from SPECjvm98.

* **Scimark**  
  This benchmark was developed by NIST and is widely used by the industry as a floating point benchmark. Each of the subtests (fft, lu, monte_carlo, sor, sparse) were incorporated into SPECjvm 2008. There are two versions 
  of this test, one with a "large" dataset (32Mbytes) which stresses the memory subsystem and a "small" dataset which stresses the JVMs (512Kbytes).

* **Serial**  
  This benchmark serializes and deserializes primitives and objects, using data from the JBoss benchmark. The benchmark has a producer-consumer scenario where serialized objects are sent via sockets and deserialized by a 
  consumer on the same system. The benchmark heavily stress the Object.equals() test.

* **Sunflow**  
  This benchmark tests graphics visualization using an open source, internally multi-threaded global illumination rendering system. The sunflow library is threaded internally, i.e. it's possible to run several bundles of 
  dependent threads to render an image. The number of internal sunflow threads is required to be 4 for a compliant run. It is however possible to configure in property specjvm.benchmark.sunflow.threads.per.instance, but no more 
  than 16, per sunflow design. Per default, the benchmark harness will use half the number of benchmark threads, i.e. will run as many sunflow benchmark instances in parallel as half the number of hardware threads. This can be 
  configured in specjvm.benchmark.threads.sunflow.

* ~~**Compiler** (Not supported)~~  
  This benchmark uses the OpenJDK (JDK 7 alpha) front end compiler to compile a set of .java files. The code compiled is javac itself and the sunflow sub-benchmark from SPECjvm 2008. 
  This benchmark uses its own FileManager to deal with memory rather than with disk and file system operations. '-proc:none' option is used to make this benchmark 1.5 compatible.

* ~~**Startup** (Not supported)~~  
  This benchmark starts each benchmark for one operation. A new JVM is launched and time is measured from start to end. Start up benchmark is single-threaded. This allows multi-threaded JVM optimizations at startup time. 
  Startup launcher is required to be the same as a 'main' JVM for submissions. Both startup launcher and arguments for startup launcher can be modified. Each startup benchmark runs the suite with the single benchmark. Each non-startup 
  benchmark is used as startup benchmark argument, except derby. Startup.scimark benchmarks use 512Kbytes datasets.

* ~~**XML** (Not supported)~~  
  This benchmark has two sub-benchmarks: XML.transform and XML.validation. XML.transform exercises the JRE's implementation of javax.xml.transform (and associated APIs) by applying style sheets (.xsl files) to XML documents. The style 
  sheets and XML documents are several real life examples that vary in size (3KB to 156KB) and in the style sheet features that are used most heavily. One "operation" of XML.transform consists of processing each stylesheet/document 
  pair, accessing the XML document as a DOM source, a SAX source, and a Stream source. In order that each style sheet / document pair contribute about equally to the time taken for a single operation, some of the input pairs are 
  processed multiple times during one operation.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the SPECjvm workload.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| Noncompliant composite result | 176.48 | 481.56 | 285.4057446808511 | ops/m |
| compress | 201.68 | 542.48 | 311.9317021276596 | ops/m |
| crypto | 436.27 | 971.07 | 602.9888031914893 | ops/m |
| derby | 187.56 | 1292.0 | 633.8829920212767 | ops/m |
| mpegaudio | 143.04 | 372.47 | 215.15396276595747 | ops/m |
| scimark.large | 39.42 | 180.22 | 109.9052659574468 | ops/m |
| scimark.small | 349.04 | 869.87 | 500.34553191489359 | ops/m |
| serial | 154.3 | 415.73 | 243.5232579787234 | ops/m |
| sunflow | 88.09 | 209.13 | 132.164335106383 | ops/m |