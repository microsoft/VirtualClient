# Compression Workloads
Virtual Client host different types of compression and decompression workloads which are:
 * 7zip
 * Gzip
 * Lzbench
 * Pbzip2

### Documentation
* [Lzbench](./Lzbench.md)
* [7zip](./7zip.md)
* [Gzip](./Gzip.md)
* [Pbzip2](./Pbzip2.md)


### Supported Platforms and Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the Lzbench workload. Note that the Virtual Client will handle the installation of any required dependencies.

* gcc
* make
* g++
* unzip
* pbzip2
* gzip

### What is Being Tested?
* Lzbench
<br/><br/>
Lzbench is used to measure performance in terms of compression speed, decompression speed and ratio of compressed size and original size. Below are the metrics measured by Lzbench Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| Compression Speed         | MB/s  |
| Decompression Speed         | MB/s  |
| Compressed size and original size ratio        | -  |

* Gzip
<br/><br/>
	Gzip is used to measure performance in terms of ReductionRatio. Below are the metrics measured by Gzip Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| ReductionRatio       | -  |


* 7zip 
 <br/><br/>
7zip is used to measure performance in terms of compressionTime, and ratio of compressed size and original size. Below are the metrics measured by 7zip Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| Compressed size and Original size ratio        | -  |
| CompressionTime   | seconds |

* Pbzip2 
 <br/><br/>
Pbzip2 is used to measure performance in terms of compressionTime, and ratio of compressed size and original size in case of compression and ratio of decompressed size and original size in case of compression. Below are the metrics measured by Pbzip2 Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| CompressionTime         | seconds  |
| Compressed size and Original size ratio        | -  |
| Decompressed size and Original size ratio | - |

# References
* [Lzbench github](https://github.com/inikep/lzbench)
* [Pbzip2 github](http://compression.ca/pbzip2/)
* [Gzip github](https://www.gzip.org/)
* [7zip](https://www.7-zip.org/)

