---
id: gzip
---

# Gzip
gzip is a single-file/stream lossless data compression utility, where the resulting compressed file generally has the suffix .gz.
gzip also refers to the associated compressed data format used by the utility.

### Documentation
* [Gzip](https://www.gzip.org/)

### Package Details
* [Workload Package Details](../VirtualClient.Documentation/DependencyPackages.md)

### Supported Platforms and Architectures
* linux-x64
* linux-arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the Gzip workload. Note that the Virtual Client will handle the installation of any required dependencies.

* unzip
* gzip

### Workload Usage 
 gzip [OPTION]... [FILE]... <br/>
Compress or uncompress FILEs (by default, compress FILES in-place).

Mandatory arguments to long options are mandatory for short options too.

  -c, --stdout      write on standard output, keep original files unchanged <br/>
  -d, --decompress  decompress <br/>
  -f, --force       force overwrite of output file and compress links <br/>
  -h, --help        give this help <br/>
  -k, --keep        keep (don't delete) input files <br/>
  -l, --list        list compressed file contents <br/>
  -L, --license     display software license <br/>
  -n, --no-name     do not save or restore the original name and timestamp <br/>
  -N, --name        save or restore the original name and timestamp <br/>
  -q, --quiet       suppress all warnings <br/>
  -r, --recursive   operate recursively on directories <br/>
      --rsyncable   make rsync-friendly archive <br/>
  -S, --suffix=SUF  use suffix SUF on compressed files <br/>
      --synchronous synchronous output (safer if system crashes, but slower) <br/>
  -t, --test        test compressed file integrity <br/>
  -v, --verbose     verbose mode <br/>
  -V, --version     display version number <br/>
  -1, --fast        compress faster <br/>
  -9, --best        compress better <br/>

With no FILE, or when FILE is -, read standard input.

Example usage:
  gzip -f myfile1.txt


### What is Being Tested?
Gzip is used to measure performance in terms of ReductionRatio. Below are the metrics measured by Gzip Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| ReductionRatio       | -  |

# References
* [Gzip github](https://www.gzip.org/)