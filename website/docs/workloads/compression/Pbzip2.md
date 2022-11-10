
# Pbzip2
PBZIP2 is a parallel implementation of the bzip2 block-sorting file compressor that uses pthreads and achieves near-linear speedup on SMP machines. The output of this version is fully compatible with bzip2 v1.0.2 or newer (ie: anything compressed with pbzip2 can be decompressed with bzip2). PBZIP2 should work on any system that has a pthreads compatible C++ compiler (such as gcc).

### Documentation
* [Pbzip2](http://compression.ca/pbzip2/)

### Supported Platforms and Architectures
* linux-x64
* linux-arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the Pbzip2 workload. Note that the Virtual Client will handle the installation of any required dependencies.

* unzip
* pbzip2

### Workload Usage 
usage: pbzip2 [ -123456789 ] [ -b#cdfhklm#p#qrS#tvVz ] [ filenames ... ]

Options: <br/>
-b# <br/>
Where # is block size in 100k steps (default 9 = 900k) <br/>
-c, --stdout <br/>
Output to standard out (stdout) <br/>
-d,--decompress <br/>
Decompress file <br/>
-f,--force <br/>
Force, overwrite existing output file <br/>
-h,--help <br/>
Print this help message <br/>
-k,--keep <br/>
Keep input file, do not delete <br/>
-l,--loadavg <br/>
Load average determines max number processors to use <br/>
-m# <br/>
Where # is max memory usage in 1MB steps (default 100 = 100MB) <br/>
-p# <br/>
Where # is the number of processors (default: autodetect) <br/>
-q,--quiet <br/>
Quiet mode (default) <br/>
-r,--read <br/>
Read entire input file into RAM and split between processors <br/>
-S# <br/>
Child thread stack size in 1KB steps (default stack size if unspecified) <br/>
-t,--test <br/>
Test compressed file integrity <br/>
-v,--verbose <br/>
Verbose mode <br/>
-V <br/>
Display version info for pbzip2 then exit <br/>
-z,--compress <br/>
Compress file (default) <br/>
-1,--fast ... -9,--best <br/>
Set BWT block size to 100k .. 900k (default 900k). <br/>
--ignore-trailing-garbage=# <br/>
Ignore trailing garbage flag (1 - ignored; 0 - forbidden) <br/>
If no file names are given, pbzip2 compresses or decompresses from standard input to standard output.

Example usage:
  pbzip2 -b15k myfile.tar <br/>
  pbzip2 -p4 -r -5 myfile.tar second*.txt



### What is Being Tested?
Pbzip2 is used to measure performance in terms of compressionTime, and ratio of compressed size and original size in case of compression and ratio of decompressed size and original size in case of compression. Below are the metrics measured by Pbzip2 Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| CompressionTime         | seconds  |
| Compressed size and Original size ratio        | -  |
| Decompressed size and Original size ratio | - |

# References
* [Pbzip2 github](http://compression.ca/pbzip2/)