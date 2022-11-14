# Lzbench
lzbench is an in-memory benchmark of open-source LZ77/LZSS/LZMA compressors. It joins all compressors into a single exe. At the beginning an input file is read to memory. Then all compressors are used to compress and decompress the file and decompressed file is verified. This approach has a big advantage of using the same compiler with the same optimizations for all compressors. The disadvantage is that it requires source code of each compressor.

### Documentation
* [Lzbench](https://github.com/inikep/lzbench)

### Supported Platforms and Architectures
* linux-x64
* linux-arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the Lzbench workload. Note that the Virtual Client will handle the installation of any required dependencies.

* gcc
* make
* g++

### Workload Usage 
usage: lzbench [options] input [input2] [input3]

where [input] is a file or a directory and [options] are:
 -b#   set block/chunk size to # KB (default = MIN(filesize,1747626 KB))
 -c#   sort results by column # (1=algname, 2=ctime, 3=dtime, 4=comprsize)
 -e#   #=compressors separated by '/' with parameters specified after ',' (deflt=fast)
 -iX,Y set min. number of compression and decompression iterations (default = 1, 1)
 -j    join files in memory but compress them independently (for many small files)
 -l    list of available compressors and aliases
 -m#   set memory limit to # MB (default = no limit)
 -o#   output text format 1=Markdown, 2=text, 3=text+origSize, 4=CSV (default = 2)
 -p#   print time for all iterations: 1=fastest 2=average 3=median (default = 1)
 -r    operate recursively on directories
 -s#   use only compressors with compression speed over # MB (default = 0 MB)
 -tX,Y set min. time in seconds for compression and decompression (default = 1, 2)
 -v    disable progress information
 -x    disable real-time process priority
 -z    show (de)compression times instead of speed

Example usage:
  lzbench -ezstd filename = selects all levels of zstd
  lzbench -ebrotli,2,5/zstd filename = selects levels 2 & 5 of brotli and zstd
  lzbench -t3 -u5 fname = 3 sec compression and 5 sec decompression loops
  lzbench -t0 -u0 -i3 -j5 -ezstd fname = 3 compression and 5 decompression iter.
  lzbench -t0u0i3j5 -ezstd fname = the same as above with aggregated parameters

### Compressors Onboarded

| Compressor Name        |                         
|----------------------------------
| memcpy     |   
| blosclz 2.0.0 -1        
| blosclz 2.0.0 -3        
| blosclz 2.0.0 -6            
| blosclz 2.0.0 -9
| brieflz 1.2.0 -1
| brieflz 1.2.0 -3
| brieflz 1.2.0 -6
| brieflz 1.2.0 -8
| brotli 2019-10-01 -0
| brotli 2019-10-01 -2
| brotli 2019-10-01 -5
| brotli 2019-10-01 -8
| brotli 2019-10-01 -11
| bzip2 1.0.8 -1
| bzip2 1.0.8 -5
| bzip2 1.0.8 -9
| crush 1.0 -0
| crush 1.0 -1
| crush 1.0 -2
| csc 2016-10-13 -1
| csc 2016-10-13 -3
| csc 2016-10-13 -5
| density 0.14.2 -1
| density 0.14.2 -2
| density 0.14.2 -3
| fastlz 0.1 -1
| fastlz 0.1 -2
| fastlzma2 1.0.1 -1
| fastlzma2 1.0.1 -3
| fastlzma2 1.0.1 -5
| fastlzma2 1.0.1 -8
| fastlzma2 1.0.1 -10
| gipfeli 2016-07-13
| libdeflate 1.3 -1
| libdeflate 1.3 -3
| libdeflate 1.3 -6
| libdeflate 1.3 -9
| libdeflate 1.3 -12
| lizard 1.0 -10
| lizard 1.0 -12
| lizard 1.0 -15
| lizard 1.0 -19
| lizard 1.0 -20
| lizard 1.0 -22
| lizard 1.0 -25
| lizard 1.0 -29
| lizard 1.0 -30
| lizard 1.0 -32
| lizard 1.0 -35
| lizard 1.0 -39
| lizard 1.0 -40
| lizard 1.0 -42
| lizard 1.0 -45
| lizard 1.0 -49
| lz4 1.9.2
| lz4fast 1.9.2 -3
| lz4fast 1.9.2 -17
| lz4hc 1.9.2 -1
| lz4hc 1.9.2 -4
| lz4hc 1.9.2 -9
| lz4hc 1.9.2 -12
| lzf 3.6 -0
| lzf 3.6 -1
| lzfse 2017-03-08
| lzg 1.0.10 -1
| lzg 1.0.10 -4
| lzg 1.0.10 -6
| lzg 1.0.10 -8
| lzham 1.0 -d26 -0
| lzham 1.0 -d26 -1
| lzjb 2010
| lzlib 1.11 -0
| lzlib 1.11 -3
| lzlib 1.11 -6
| lzlib 1.11 -9
| lzma 19.00 -0
| lzma 19.00 -2
| lzma 19.00 -4
| lzma 19.00 -5
| lzma 19.00 -9
| lzmat 1.01
| lzo1 2.10 -1
| lzo1 2.10 -99
| lzo1a 2.10 -1
| lzo1a 2.10 -99
| lzo1b 2.10 -1
| lzo1b 2.10 -3
| lzo1b 2.10 -6
| lzo1b 2.10 -9
| lzo1b 2.10 -99
| lzo1b 2.10 -999
| lzo1c 2.10 -1
| lzo1c 2.10 -3
| lzo1c 2.10 -6
| lzo1c 2.10 -9
| lzo1c 2.10 -99
| lzo1c 2.10 -999
| lzo1f 2.10 -1
| lzo1f 2.10 -999
| lzo1x 2.10 -1
| lzo1x 2.10 -11
| lzo1x 2.10 -12
| lzo1x 2.10 -15
| lzo1x 2.10 -999
| lzo1y 2.10 -1
| lzo1y 2.10 -999
| lzo1z 2.10 -999
| lzo2a 2.10 -999
| lzrw 15-Jul-1991 -1
| lzrw 15-Jul-1991 -3
| lzrw 15-Jul-1991 -4
| lzrw 15-Jul-1991 -5
| lzsse2 2019-04-18 -1
| lzsse2 2019-04-18 -6
| lzsse2 2019-04-18 -12
| lzsse2 2019-04-18 -16
| lzsse4 2019-04-18 -1
| lzsse4 2019-04-18 -6
| lzsse4 2019-04-18 -12
| lzsse4 2019-04-18 -16
| lzsse8 2019-04-18 -1
| lzsse8 2019-04-18 -6
| lzsse8 2019-04-18 -12
| lzsse8 2019-04-18 -16
| lzvn 2017-03-08
| pithy 2011-12-24 -0
| pithy 2011-12-24 -3
| pithy 2011-12-24 -6
| pithy 2011-12-24 -9
| quicklz 1.5.0 -1
| quicklz 1.5.0 -2
| quicklz 1.5.0 -3
| shrinker 0.1
| slz_zlib 1.0.0 -1
| slz_zlib 1.0.0 -2
| slz_zlib 1.0.0 -3
| snappy 2019-09-30
| tornado 0.6a -1
| tornado 0.6a -2
| tornado 0.6a -3
| tornado 0.6a -4
| tornado 0.6a -5
| tornado 0.6a -6
| tornado 0.6a -7
| tornado 0.6a -10
| tornado 0.6a -13
| tornado 0.6a -16
| ucl_nrv2b 1.03 -1
| ucl_nrv2b 1.03 -6
| ucl_nrv2b 1.03 -9
| ucl_nrv2d 1.03 -1
| ucl_nrv2d 1.03 -6
| ucl_nrv2d 1.03 -9
| ucl_nrv2e 1.03 -1
| ucl_nrv2e 1.03 -6
| ucl_nrv2e 1.03 -9
| wflz 2015-09-16
| xpack 2016-06-02 -1
| xpack 2016-06-02 -6
| xpack 2016-06-02 -9
| xz 5.2.4 -0
| xz 5.2.4 -3
| xz 5.2.4 -6
| xz 5.2.4 -9
| yalz77 2015-09-19 -1
| yalz77 2015-09-19 -4
| yalz77 2015-09-19 -8
| yalz77 2015-09-19 -12
| yappy 2014-03-22 -1
| yappy 2014-03-22 -10
| yappy 2014-03-22 -100
| zlib 1.2.11 -1
| zlib 1.2.11 -6
| zlib 1.2.11 -9
| zling 2018-10-12 -0
| zling 2018-10-12 -1
| zling 2018-10-12 -2
| zling 2018-10-12 -3
| zling 2018-10-12 -4
| zstd 1.4.3 -1
| zstd 1.4.3 -2
| zstd 1.4.3 -5
| zstd 1.4.3 -8
| zstd 1.4.3 -11
| zstd 1.4.3 -15
| zstd 1.4.3 -18
| zstd 1.4.3 -22



### What is Being Tested?
Lzbench is used to measure performance in terms of compression speed, decompression speed and ratio of compressed size and original size. Below are the metrics measured by Lzbench Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| Compression Speed         | MB/s  |
| Decompression Speed         | MB/s  |
| Compressed size and original size ratio        | -  |

# References
* [Lzbench github](https://github.com/inikep/lzbench)