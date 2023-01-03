# Compression
This represents a suite of workloads that focus on compression and decompression, algorithms often used in media and streaming
applications. The following list of compression/decompression software toolsets are incorporated:

* [Lzbench](https://github.com/inikep/lzbench)  
  lzbench is an in-memory benchmark of open-source LZ77/LZSS/LZMA compressors. It joins all compressors into a single exe. At the beginning an input file is 
  read to memory. Then all compressors are used to compress and decompress the file and decompressed file is verified. This approach has a big advantage of 
  using the same compiler with the same optimizations for all compressors. The disadvantage is that it requires source code of each compressor.

* [7zip](https://www.7-zip.org/)  
  7-Zip is file archive software with a high compression ratio.

* [Gzip](https://www.gzip.org/)  
  Gzip is a single-file/stream lossless data compression utility, where the resulting compressed file generally has the suffix .gz.
  Gzip also refers to the associated compressed data format used by the utility.

* [Pbzip2](./pbzip2.md)  
  PBZIP2 is a parallel implementation of the bzip2 block-sorting file compressor that uses pthreads and achieves near-linear speedup on SMP machines. The output of this
  version is fully compatible with bzip2 v1.0.2 or newer (ie: anything compressed with pbzip2 can be decompressed with bzip2). PBZIP2 should work on any system 
  that has a pthreads compatible C++ compiler (such as gcc).

## What is Being Measured?
The following section provides details on the performance and reliability aspects of the system that are being measured when
the workload runs.

* **Aspects of Compression and Decompression Performance**  
  * Compression speed (time to compress the stream)
  * Decompression speed (time to decompress the stream)
  * Ratio of reduction (compressed size vs. original size)

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the compression/decompression workloads.

| Tool Name | Scenario Name   | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-----------------|-------------|---------------------|---------------------|---------------------|------|
| 7zip | 7zBZIP2FastestMode | Compressed size and Original size ratio | 28.54626703642159 | 28.54626703642159 | 28.54626703642148 |  |
| 7zip | 7zBZIP2FastestMode | CompressionTime | 77.777 | 309.621 | 103.63701393114492 | seconds |
| 7zip | 7zBZIP2MaximumMode | Compressed size and Original size ratio | 25.70608145057875 | 25.70608145057875 | 25.706081450578656 |  |
| 7zip | 7zBZIP2MaximumMode | CompressionTime | 262.375 | 995.985 | 333.5317747336377 | seconds |
| 7zip | 7zBZIP2UltraMode | Compressed size and Original size ratio | 25.578691713419994 | 25.578691713419994 | 25.57869171341992 |  |
| 7zip | 7zBZIP2UltraMode | CompressionTime | 873.9680000000001 | 3534.6029999999998 | 1153.8054393884889 | seconds |
| 7zip | 7zLZMAFastestMode | Compressed size and Original size ratio | 27.576542694586327 | 27.576542694586327 | 27.576542694586136 |  |
| 7zip | 7zLZMAFastestMode | CompressionTime | 26.381999999999999 | 103.99000000000003 | 31.698898469527188 | seconds |
| 7zip | 7zLZMAMaximumMode | Compressed size and Original size ratio | 22.991623799687628 | 22.991623799687628 | 22.991623799687657 |  |
| 7zip | 7zLZMAMaximumMode | CompressionTime | 210.252 | 784.7560000000001 | 262.7526564035848 | seconds |
| 7zip | 7zLZMAUltraMode | Compressed size and Original size ratio | 22.980768768008166 | 22.980768768008166 | 22.980768768008216 |  |
| 7zip | 7zLZMAUltraMode | CompressionTime | 213.596 | 789.093 | 266.5016152087777 | seconds |
| 7zip | 7zPPMdFastestMode | Compressed size and Original size ratio | 26.349047445726965 | 26.349047445726965 | 26.349047445726865 |  |
| 7zip | 7zPPMdFastestMode | CompressionTime | 43.550000000000007 | 239.761 | 51.61994930340558 | seconds |
| 7zip | 7zPPMdMaximumMode | Compressed size and Original size ratio | 23.105210009428207 | 23.105210009428207 | 23.105210009428178 |  |
| 7zip | 7zPPMdMaximumMode | CompressionTime | 81.60799999999999 | 311.63599999999999 | 95.67844357334434 | seconds |
| 7zip | 7zPPMdUltraMode | Compressed size and Original size ratio | 22.6746116728724 | 22.6746116728724 | 22.674611672872424 |  |
| 7zip | 7zPPMdUltraMode | CompressionTime | 109.17200000000003 | 391.588 | 125.45798865619547 | seconds |
| 7zip | tarMode | Compressed size and Original size ratio | 100.00445978264081 | 100.00445978264081 | 100.00445978264092 |  |
| 7zip | tarMode | CompressionTime | 0.40800000000000005 | 7.165000000000001 | 0.651991662806855 | seconds |
| 7zip | zipBZIP2UltraMode | Compressed size and Original size ratio | 25.529782260502079 | 25.529782260502079 | 25.529782260501965 |  |
| 7zip | zipBZIP2UltraMode | CompressionTime | 660.4379999999999 | 3453.892 | 1063.4589593562469 | seconds |
| 7zip | zipDeflate64UltraMode | Compressed size and Original size ratio | 29.64416105835945 | 29.64416105835945 | 29.644161058359435 |  |
| 7zip | zipDeflate64UltraMode | CompressionTime | 408.687 | 2283.504 | 634.8115308483292 | seconds |
| 7zip | zipDeflateUltraMode | Compressed size and Original size ratio | 30.54039948743641 | 30.54039948743641 | 30.540399487436475 |  |
| 7zip | zipDeflateUltraMode | CompressionTime | 378.94200000000009 | 2157.2039999999999 | 593.5336110435417 | seconds |
| 7zip | zipLZMAUltraMode | Compressed size and Original size ratio | 23.001676240352276 | 23.001676240352276 | 23.00167624035228 |  |
| 7zip | zipLZMAUltraMode | CompressionTime | 208.87900000000003 | 831.223 | 275.24728488032698 | seconds |
| 7zip | zipPPMdUltraMode | Compressed size and Original size ratio | 22.414832165054614 | 22.414832165054614 | 22.414832165054589 |  |
| 7zip | zipPPMdUltraMode | CompressionTime | 126.83800000000001 | 512.9830000000001 | 166.30101273291926 | seconds |
| Gzip | GzipCompression | ReductionRatio | 26.5 | 90.5 | 62.55014446339679 |  |
| Gzip | GzipDecompression | ReductionRatio | 26.5 | 90.5 | 62.5479484010236 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(blosclz 2.0.0 -1) | 1.0 | 100.0 | 99.99092020254933 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(blosclz 2.0.0 -3) | 67.72 | 100.0 | 94.91330264716741 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(blosclz 2.0.0 -6) | 29.51 | 100.0 | 70.30532485189855 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(blosclz 2.0.0 -9) | 29.51 | 100.0 | 69.19955428615825 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brieflz 1.3.0 -1) | 13.41 | 86.56 | 45.590009723086549 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brieflz 1.3.0 -3) | 11.82 | 82.08 | 42.679992415992568 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brieflz 1.3.0 -6) | 8.96 | 79.91 | 38.74821970286243 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brieflz 1.3.0 -8) | 7.27 | 78.88 | 37.47986990510265 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brotli 2019-10-01 -0) | 11.99 | 83.01 | 43.56122608120724 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brotli 2019-10-01 -11) | 4.82 | 63.29 | 29.224025554756 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brotli 2019-10-01 -2) | 8.96 | 77.1 | 38.7620251244555 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brotli 2019-10-01 -5) | 6.65 | 72.56 | 34.37627948583316 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(brotli 2019-10-01 -8) | 6.09 | 71.85 | 33.079113380007758 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(bzip2 1.0.8 -1) | 7.03 | 69.88 | 33.13978043135806 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(bzip2 1.0.8 -5) | 5.73 | 68.43 | 30.587530532866614 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(bzip2 1.0.8 -9) | 5.4 | 68.13 | 30.04151539314258 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(crush 1.0 -0) | 10.93 | 79.41 | 41.076148519858417 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(crush 1.0 -1) | 8.36 | 76.86 | 37.67712735124211 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(crush 1.0 -2) | 7.82 | 75.46 | 36.2261612526745 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(csc 2016-10-13 -1) | 7.34 | 70.09 | 31.65966774306501 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(csc 2016-10-13 -3) | 7.6 | 67.43 | 30.09155448780303 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(csc 2016-10-13 -5) | 5.92 | 66.09 | 28.382816597284383 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(density 0.14.2 -1) | 53.65 | 88.2 | 65.94869290327601 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(density 0.14.2 -2) | 28.39 | 84.8 | 54.005572134464198 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(density 0.14.2 -3) | 16.91 | 82.92 | 48.08582620029564 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlz 0.1 -1) | 20.71 | 96.8 | 56.695560656758228 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlz 0.1 -2) | 19.62 | 96.77 | 54.88234517924563 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlzma2 1.0.1 -1) | 7.19 | 66.34 | 33.40807334449338 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlzma2 1.0.1 -10) | 4.43 | 61.52 | 28.478143655445046 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlzma2 1.0.1 -3) | 6.8 | 62.06 | 30.611717989644544 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlzma2 1.0.1 -5) | 6.03 | 61.66 | 29.34066230580537 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(fastlzma2 1.0.1 -8) | 4.92 | 61.57 | 28.61435171124861 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(gipfeli 2016-07-13) | 15.09 | 90.17 | 48.58151618995699 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(libdeflate 1.3 -1) | 12.12 | 75.76 | 40.646381938900699 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(libdeflate 1.3 -12) | 8.2 | 72.36 | 36.52483938207389 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(libdeflate 1.3 -3) | 11.24 | 74.85 | 39.351822660098537 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(libdeflate 1.3 -6) | 9.43 | 73.68 | 38.12003699521013 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(libdeflate 1.3 -9) | 9.22 | 72.37 | 36.85610407821452 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -10) | 16.53 | 99.83 | 57.33410418249473 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -12) | 14.33 | 86.96 | 47.72932908655248 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -15) | 11.91 | 86.05 | 45.1657280891434 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -19) | 10.86 | 84.68 | 43.14251202929888 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -20) | 15.5 | 96.99 | 53.48677829076813 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -22) | 13.19 | 90.07 | 47.7740002727503 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -25) | 10.26 | 85.49 | 43.06461357127556 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -29) | 7.14 | 85.38 | 40.21797112743277 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -30) | 13.62 | 91.53 | 48.14057667205674 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -32) | 11.55 | 85.34 | 44.805343373141848 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -35) | 11.03 | 78.86 | 41.768286315461399 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -39) | 9.58 | 75.67 | 39.04006448973228 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -40) | 12.4 | 85.84 | 44.626154970190508 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -42) | 10.73 | 81.59 | 41.2279135331021 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -45) | 8.37 | 76.33 | 37.99530105222138 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lizard 1.0 -49) | 5.93 | 73.89 | 35.08781391270459 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4 1.9.2) | 16.49 | 99.01 | 55.785999454382459 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4fast 1.9.2 -17) | 21.38 | 100.34 | 69.80031920491079 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4fast 1.9.2 -3) | 16.94 | 99.83 | 58.83688453668519 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4hc 1.9.2 -1) | 13.51 | 86.47 | 46.76217445531435 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4hc 1.9.2 -12) | 10.78 | 84.64 | 43.0839663639552 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4hc 1.9.2 -4) | 12.02 | 84.69 | 44.50082199789536 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lz4hc 1.9.2 -9) | 10.95 | 84.67 | 43.448272401294058 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzf 3.6 -0) | 21.65 | 97.75 | 57.288645122284219 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzf 3.6 -1) | 20.67 | 95.72 | 55.34011896867808 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzfse 2017-03-08) | 9.6 | 75.03 | 38.1885660502497 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzg 1.0.10 -1) | 24.49 | 92.18 | 58.60653882917512 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzg 1.0.10 -4) | 17.86 | 90.99 | 52.627542295158729 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzg 1.0.10 -6) | 15.81 | 88.87 | 49.44423361367489 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzg 1.0.10 -8) | 13.75 | 85.43 | 46.39021854937859 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzham 1.0 -d26 -0) | 8.39 | 68.82 | 35.62012879807974 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzham 1.0 -d26 -1) | 6.37 | 65.28 | 31.115252629730095 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzjb 2010) | 25.97 | 98.58 | 66.04742291178765 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzlib 1.11 -0) | 8.55 | 69.02 | 35.77932474629198 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzlib 1.11 -3) | 7.86 | 64.12 | 31.71930092312497 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzlib 1.11 -6) | 5.97 | 60.88 | 28.76038800843153 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzlib 1.11 -9) | 4.3 | 60.85 | 28.3257299576412 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzma 19.00 -0) | 8.28 | 67.89 | 35.97057627714419 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzma 19.00 -2) | 7.41 | 67.47 | 33.616490392126227 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzma 19.00 -4) | 7.15 | 67.49 | 32.92226234182155 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzma 19.00 -5) | 5.9 | 60.9 | 28.78115874379903 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzma 19.00 -9) | 5.19 | 60.9 | 28.484276674283707 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzmat 1.01) | 12.06 | 82.06 | 43.34398283553727 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1 2.10 -1) | 21.27 | 97.8 | 57.77112246093757 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1 2.10 -99) | 17.22 | 95.21 | 51.992468945312477 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1a 2.10 -1) | 21.21 | 88.52 | 56.0705171874999 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1a 2.10 -99) | 17.19 | 82.52 | 50.135671875000017 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1b 2.10 -1) | 17.53 | 94.41 | 52.98615551388718 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1b 2.10 -3) | 16.93 | 93.15 | 51.454242743857289 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1b 2.10 -6) | 17.64 | 85.21 | 49.718933552091808 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1b 2.10 -9) | 16.27 | 85.95 | 48.89764034178682 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1b 2.10 -99) | 14.35 | 85.87 | 47.26098449142442 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1b 2.10 -999) | 11.4 | 80.54 | 42.52683078066626 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1c 2.10 -1) | 18.52 | 94.94 | 54.28047398278565 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1c 2.10 -3) | 17.87 | 93.72 | 52.82783329094851 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1c 2.10 -6) | 18.31 | 84.73 | 50.67240106418367 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1c 2.10 -9) | 17.18 | 84.26 | 49.67556701030927 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1c 2.10 -99) | 15.74 | 83.29 | 48.2789117549248 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1c 2.10 -999) | 12.83 | 79.29 | 44.3560102897162 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1f 2.10 -1) | 18.81 | 94.93 | 54.65413200829468 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1f 2.10 -999) | 12.82 | 81.69 | 44.99018232325209 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1x 2.10 -1) | 18.72 | 100.27 | 55.45193708674055 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1x 2.10 -11) | 19.26 | 100.32 | 58.664756641496108 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1x 2.10 -12) | 18.94 | 100.29 | 56.91882722328731 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1x 2.10 -15) | 18.79 | 100.28 | 55.96330568488591 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1x 2.10 -999) | 11.29 | 79.86 | 42.25713995070235 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1y 2.10 -1) | 18.55 | 100.27 | 56.04797065966786 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1y 2.10 -999) | 11.29 | 80.76 | 42.56547644696892 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo1z 2.10 -999) | 11.29 | 79.99 | 42.22295661541474 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzo2a 2.10 -999) | 13.42 | 78.28 | 45.394575195408119 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzrw 15-Jul-1991 -1) | 31.06 | 92.29 | 60.67200544606826 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzrw 15-Jul-1991 -3) | 25.72 | 91.61 | 57.03344339715076 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzrw 15-Jul-1991 -4) | 25.07 | 89.33 | 54.19616154886248 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzrw 15-Jul-1991 -5) | 19.54 | 87.04 | 49.69951225725547 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse2 2019-04-18 -1) | 15.78 | 89.47 | 48.80698616174692 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse2 2019-04-18 -12) | 11.17 | 83.66 | 42.482057483006567 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse2 2019-04-18 -16) | 11.17 | 83.66 | 42.49360981078635 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse2 2019-04-18 -6) | 11.18 | 83.66 | 42.48141131882806 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse4 2019-04-18 -1) | 13.25 | 87.97 | 46.11863327891101 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse4 2019-04-18 -12) | 11.28 | 87.8 | 42.75787434101815 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse4 2019-04-18 -16) | 11.28 | 87.8 | 42.75793956800564 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse4 2019-04-18 -6) | 11.28 | 87.8 | 42.759214941873029 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse8 2019-04-18 -1) | 13.58 | 84.77 | 45.41388676869817 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse8 2019-04-18 -12) | 11.54 | 84.6 | 42.086616434852718 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse8 2019-04-18 -16) | 11.54 | 84.6 | 42.08657393562599 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzsse8 2019-04-18 -6) | 11.54 | 84.6 | 42.089752714825319 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(lzvn 2017-03-08) | 14.05 | 83.58 | 45.43754111906824 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(memcpy) | 100.0 | 100.0 | 100.0 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(pithy 2011-12-24 -0) | 16.65 | 98.82 | 56.757044935862108 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(pithy 2011-12-24 -3) | 15.63 | 97.17 | 53.74389033171991 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(pithy 2011-12-24 -6) | 14.95 | 94.53 | 51.066992877661068 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(pithy 2011-12-24 -9) | 14.75 | 93.56 | 50.26221417607932 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(quicklz 1.5.0 -1) | 18.36 | 89.61 | 52.13116415443351 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(quicklz 1.5.0 -2) | 14.51 | 84.41 | 46.700006690542767 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(quicklz 1.5.0 -3) | 13.36 | 83.74 | 45.66553791054172 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(shrinker 0.1) | 15.94 | 90.98 | 52.18137908274491 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(slz_gzip 1.2.0 -1) | 17.33 | 98.56 | 55.39967825727607 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(slz_gzip 1.2.0 -2) | 16.26 | 98.15 | 54.077282602278739 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(slz_gzip 1.2.0 -3) | 15.98 | 98.05 | 53.75384162780171 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(snappy 2019-09-30) | 18.32 | 99.83 | 56.07605545388349 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -1) | 16.29 | 114.84 | 60.178269836272168 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -10) | 6.29 | 69.91 | 33.38658409464737 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -13) | 6.37 | 67.98 | 32.41582045184303 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -16) | 5.02 | 67.43 | 31.270285776571119 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -2) | 14.69 | 99.27 | 50.64494450566747 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -3) | 11.06 | 76.93 | 40.566242920572239 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -4) | 10.79 | 76.55 | 39.54775984213127 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -5) | 8.91 | 71.54 | 36.52949047478035 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -6) | 8.21 | 70.93 | 35.746297385879049 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(tornado 0.6a -7) | 7.12 | 69.99 | 34.078368281799139 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2b 1.03 -1) | 15.1 | 81.03 | 45.297618056381669 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2b 1.03 -6) | 11.37 | 77.15 | 41.523312235868349 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2b 1.03 -9) | 9.65 | 77.07 | 40.31242296779826 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2d 1.03 -1) | 15.08 | 79.99 | 45.0823111111111 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2d 1.03 -6) | 11.37 | 77.02 | 41.394452669180477 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2d 1.03 -9) | 9.5 | 76.4 | 39.70092518708194 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2e 1.03 -1) | 15.0 | 80.28 | 45.03913086655148 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2e 1.03 -6) | 11.28 | 76.87 | 41.21479433553802 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(ucl_nrv2e 1.03 -9) | 9.45 | 76.17 | 39.59470196960945 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(wflz 2015-09-16) | 18.83 | 94.49 | 59.618930121831677 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xpack 2016-06-02 -1) | 11.44 | 72.53 | 39.89646625375899 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xpack 2016-06-02 -6) | 7.99 | 70.49 | 35.64701925949528 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xpack 2016-06-02 -9) | 7.42 | 69.97 | 35.122027126610749 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xz 5.2.4 -0) | 8.05 | 68.48 | 35.73626974483595 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xz 5.2.4 -3) | 6.35 | 66.92 | 32.357593570105397 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xz 5.2.4 -6) | 5.3 | 60.87 | 28.744679164093478 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(xz 5.2.4 -9) | 5.18 | 60.87 | 28.651549096559575 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yalz77 2015-09-19 -1) | 15.05 | 93.62 | 52.033159152937489 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yalz77 2015-09-19 -12) | 4.0 | 93.27 | 47.553214855911317 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yalz77 2015-09-19 -4) | 12.49 | 93.15 | 49.0025382635244 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yalz77 2015-09-19 -8) | 11.65 | 93.4 | 48.03735777143654 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yappy 2014-03-22 -1) | 24.51 | 98.27 | 57.459965659066437 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yappy 2014-03-22 -10) | 19.58 | 98.27 | 54.97120395327944 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(yappy 2014-03-22 -100) | 18.72 | 98.27 | 54.41111392860001 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zlib 1.2.11 -1) | 13.78 | 76.78 | 42.55240141365335 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zlib 1.2.11 -6) | 9.54 | 73.52 | 38.483303116826117 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zlib 1.2.11 -9) | 8.91 | 73.44 | 38.23369666353855 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zling 2018-10-12 -0) | 8.11 | 72.41 | 36.13437921808236 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zling 2018-10-12 -1) | 7.66 | 72.19 | 35.688635437881888 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zling 2018-10-12 -2) | 7.46 | 72.08 | 35.445766773162869 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zling 2018-10-12 -3) | 7.31 | 72.02 | 35.21219209265185 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zling 2018-10-12 -4) | 7.22 | 71.83 | 34.997833233473148 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -1) | 8.49 | 86.27 | 42.469316357100087 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -11) | 6.72 | 71.78 | 34.25264773522651 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -15) | 5.84 | 71.77 | 33.229587832973439 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -18) | 5.5 | 69.15 | 31.53026457883369 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -2) | 8.61 | 80.1 | 40.1501561813461 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -22) | 4.81 | 69.06 | 31.26637705508219 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -5) | 8.12 | 73.22 | 36.44376592770125 |  |
| Lzbench | Lzbench | Compressed size and Original size ratio(zstd 1.4.5 -8) | 7.08 | 72.35 | 34.98902536448971 |  |
| Lzbench | Lzbench | Compression Speed(blosclz 2.0.0 -1) | 1394.31 | 14776.34 | 9097.599858010204 | MB/s |
| Lzbench | Lzbench | Compression Speed(blosclz 2.0.0 -3) | 114.66 | 11885.31 | 2239.0791046979086 | MB/s |
| Lzbench | Lzbench | Compression Speed(blosclz 2.0.0 -6) | 43.44 | 7295.2 | 848.7351901487783 | MB/s |
| Lzbench | Lzbench | Compression Speed(blosclz 2.0.0 -9) | 44.38 | 1398.48 | 352.8370697258757 | MB/s |
| Lzbench | Lzbench | Compression Speed(brieflz 1.3.0 -1) | 29.05 | 301.39 | 156.468116693868 | MB/s |
| Lzbench | Lzbench | Compression Speed(brieflz 1.3.0 -3) | 14.9 | 227.58 | 96.39768707218419 | MB/s |
| Lzbench | Lzbench | Compression Speed(brieflz 1.3.0 -6) | 3.38 | 56.06 | 23.743967602675796 | MB/s |
| Lzbench | Lzbench | Compression Speed(brieflz 1.3.0 -8) | 0.64 | 4.92 | 3.2972013067828249 | MB/s |
| Lzbench | Lzbench | Compression Speed(brotli 2019-10-01 -0) | 54.23 | 715.83 | 312.4815665836963 | MB/s |
| Lzbench | Lzbench | Compression Speed(brotli 2019-10-01 -11) | 0.09 | 0.68 | 0.4302277368287988 | MB/s |
| Lzbench | Lzbench | Compression Speed(brotli 2019-10-01 -2) | 18.08 | 265.59 | 120.88512961126344 | MB/s |
| Lzbench | Lzbench | Compression Speed(brotli 2019-10-01 -5) | 3.53 | 85.1 | 30.49815880053672 | MB/s |
| Lzbench | Lzbench | Compression Speed(brotli 2019-10-01 -8) | 0.99 | 43.74 | 12.548064760793466 | MB/s |
| Lzbench | Lzbench | Compression Speed(bzip2 1.0.8 -1) | 2.38 | 18.26 | 11.322619459732785 | MB/s |
| Lzbench | Lzbench | Compression Speed(bzip2 1.0.8 -5) | 1.9 | 18.89 | 11.064114626888893 | MB/s |
| Lzbench | Lzbench | Compression Speed(bzip2 1.0.8 -9) | 1.68 | 18.38 | 10.59817538264066 | MB/s |
| Lzbench | Lzbench | Compression Speed(crush 1.0 -0) | 7.41 | 139.38 | 54.542906212315717 | MB/s |
| Lzbench | Lzbench | Compression Speed(crush 1.0 -1) | 0.75 | 26.29 | 8.33534038745818 | MB/s |
| Lzbench | Lzbench | Compression Speed(crush 1.0 -2) | 0.07 | 6.05 | 1.462390242953569 | MB/s |
| Lzbench | Lzbench | Compression Speed(csc 2016-10-13 -1) | 2.58 | 63.24 | 22.65882100062247 | MB/s |
| Lzbench | Lzbench | Compression Speed(csc 2016-10-13 -3) | 1.18 | 31.4 | 10.152805509084543 | MB/s |
| Lzbench | Lzbench | Compression Speed(csc 2016-10-13 -5) | 0.47 | 22.08 | 4.629775707115902 | MB/s |
| Lzbench | Lzbench | Compression Speed(density 0.14.2 -1) | 167.39 | 2125.82 | 1230.6706335959537 | MB/s |
| Lzbench | Lzbench | Compression Speed(density 0.14.2 -2) | 123.99 | 1108.03 | 704.9344852540659 | MB/s |
| Lzbench | Lzbench | Compression Speed(density 0.14.2 -3) | 50.72 | 431.74 | 298.1481199906622 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlz 0.1 -1) | 38.01 | 551.46 | 252.6335754026924 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlz 0.1 -2) | 4.0 | 550.33 | 261.1950987488569 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlzma2 1.0.1 -1) | 2.9 | 37.4 | 17.630873982948576 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlzma2 1.0.1 -10) | 0.77 | 6.06 | 4.031916749411055 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlzma2 1.0.1 -3) | 1.88 | 20.58 | 9.41909681940281 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlzma2 1.0.1 -5) | 1.49 | 9.53 | 6.42465833430674 | MB/s |
| Lzbench | Lzbench | Compression Speed(fastlzma2 1.0.1 -8) | 1.13 | 7.24 | 4.819979558462797 | MB/s |
| Lzbench | Lzbench | Compression Speed(gipfeli 2016-07-13) | 44.28 | 653.84 | 272.60836036527186 | MB/s |
| Lzbench | Lzbench | Compression Speed(libdeflate 1.3 -1) | 20.71 | 294.58 | 135.37139858642107 | MB/s |
| Lzbench | Lzbench | Compression Speed(libdeflate 1.3 -12) | 0.88 | 18.53 | 6.88628277554837 | MB/s |
| Lzbench | Lzbench | Compression Speed(libdeflate 1.3 -3) | 17.36 | 263.64 | 113.62387694704049 | MB/s |
| Lzbench | Lzbench | Compression Speed(libdeflate 1.3 -6) | 12.2 | 157.18 | 70.50402605241638 | MB/s |
| Lzbench | Lzbench | Compression Speed(libdeflate 1.3 -9) | 1.76 | 23.99 | 12.141068785541796 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -10) | 68.66 | 809.63 | 409.10465665361468 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -12) | 22.41 | 227.5 | 122.04293885025227 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -15) | 11.04 | 100.22 | 58.870673640737937 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -19) | 0.28 | 24.43 | 6.607793622036507 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -20) | 47.81 | 737.78 | 338.03546256793876 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -22) | 16.07 | 376.35 | 135.66142851576957 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -25) | 2.31 | 51.54 | 21.065819127588698 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -29) | 0.23 | 5.48 | 2.4534068460324578 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -30) | 51.01 | 605.63 | 288.7071300824096 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -32) | 23.72 | 212.9 | 120.95537766175065 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -35) | 10.75 | 109.58 | 62.295380973718568 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -39) | 0.28 | 24.22 | 6.428691162718204 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -40) | 37.24 | 597.76 | 246.95011339282238 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -42) | 14.05 | 330.51 | 118.17463858473289 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -45) | 2.19 | 48.77 | 20.168945830085737 | MB/s |
| Lzbench | Lzbench | Compression Speed(lizard 1.0 -49) | 0.22 | 5.3 | 2.3225167575993757 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4 1.9.2) | 84.35 | 1410.9 | 564.7161906153787 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4fast 1.9.2 -17) | 127.79 | 4345.91 | 1274.4706181428433 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4fast 1.9.2 -3) | 95.29 | 2103.27 | 718.7670306927797 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4hc 1.9.2 -1) | 15.0 | 197.66 | 93.05745371633472 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4hc 1.9.2 -12) | 1.31 | 26.37 | 12.224298242195113 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4hc 1.9.2 -4) | 10.51 | 121.56 | 61.17812234477919 | MB/s |
| Lzbench | Lzbench | Compression Speed(lz4hc 1.9.2 -9) | 2.7 | 53.67 | 27.981011030128227 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzf 3.6 -0) | 3.0 | 594.37 | 282.2649176140241 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzf 3.6 -1) | 51.52 | 618.98 | 293.723025510005 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzfse 2017-03-08) | 12.35 | 101.2 | 60.56648505774032 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzg 1.0.10 -1) | 11.7 | 109.05 | 54.36954294520414 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzg 1.0.10 -4) | 6.62 | 66.62 | 34.79061408813374 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzg 1.0.10 -6) | 3.76 | 50.0 | 23.83807949733643 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzg 1.0.10 -8) | 0.92 | 20.64 | 8.737031826253244 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzham 1.0 -d26 -0) | 1.34 | 14.5 | 7.839318554367341 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzham 1.0 -d26 -1) | 0.54 | 3.78 | 2.435530121187284 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzjb 2010) | 46.63 | 442.95 | 244.0955729898517 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzlib 1.11 -0) | 3.27 | 72.47 | 27.775190671350509 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzlib 1.11 -3) | 0.85 | 19.02 | 6.764049960967995 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzlib 1.11 -6) | 0.32 | 5.41 | 2.6312549524757498 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzlib 1.11 -9) | 0.27 | 3.05 | 1.7111858849246625 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzma 19.00 -0) | 2.9 | 66.23 | 24.926588065353614 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzma 19.00 -2) | 2.07 | 72.9 | 22.736408178409623 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzma 19.00 -4) | 1.93 | 61.33 | 18.316807530073427 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzma 19.00 -5) | 0.52 | 6.74 | 3.2128092411045578 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzma 19.00 -9) | 0.48 | 4.68 | 2.644742588180149 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzmat 1.01) | 3.72 | 72.04 | 31.812636485486224 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1 2.10 -1) | 27.17 | 627.29 | 227.24079959375795 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1 2.10 -99) | 13.77 | 190.46 | 87.2875890625 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1a 2.10 -1) | 28.79 | 607.11 | 232.74775917968746 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1a 2.10 -99) | 12.76 | 187.98 | 85.23441445312501 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1b 2.10 -1) | 22.75 | 565.95 | 196.0898892578125 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1b 2.10 -3) | 22.3 | 506.38 | 189.85328723778273 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1b 2.10 -6) | 25.72 | 460.96 | 171.9568871830931 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1b 2.10 -9) | 23.68 | 270.38 | 128.4897497213695 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1b 2.10 -99) | 11.85 | 205.59 | 88.5883731889017 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1b 2.10 -999) | 1.74 | 31.35 | 14.18836296774446 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1c 2.10 -1) | 22.53 | 551.75 | 199.66396987480437 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1c 2.10 -3) | 23.22 | 561.75 | 197.30957159624416 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1c 2.10 -6) | 20.3 | 448.53 | 159.52031358203409 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1c 2.10 -9) | 18.84 | 263.89 | 116.58679610321012 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1c 2.10 -99) | 11.94 | 203.58 | 83.9358802010994 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1c 2.10 -999) | 3.2 | 43.35 | 21.429777382186665 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1f 2.10 -1) | 20.64 | 545.14 | 183.92367338954208 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1f 2.10 -999) | 2.49 | 40.16 | 19.07896044446183 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1x 2.10 -1) | 73.95 | 8294.12 | 1112.9350027387614 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1x 2.10 -11) | 80.62 | 9664.81 | 1256.3371937477994 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1x 2.10 -12) | 76.9 | 9170.12 | 1210.877098869283 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1x 2.10 -15) | 75.39 | 8711.95 | 1161.9000737509294 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1x 2.10 -999) | 0.74 | 14.9 | 6.922765953284556 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1y 2.10 -1) | 74.32 | 8371.98 | 1116.9970266833603 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1y 2.10 -999) | 0.69 | 15.05 | 6.946227867439675 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo1z 2.10 -999) | 0.71 | 14.61 | 6.843239447654491 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzo2a 2.10 -999) | 3.62 | 46.73 | 22.26428036980452 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzrw 15-Jul-1991 -1) | 30.89 | 397.57 | 195.92532097870589 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzrw 15-Jul-1991 -3) | 47.81 | 458.17 | 256.6585046820515 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzrw 15-Jul-1991 -4) | 48.71 | 469.66 | 251.00431345652644 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzrw 15-Jul-1991 -5) | 22.71 | 169.99 | 108.6450663322294 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse2 2019-04-18 -1) | 3.86 | 29.02 | 18.02409638790349 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse2 2019-04-18 -12) | 1.32 | 14.65 | 7.892625707102451 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse2 2019-04-18 -16) | 1.33 | 14.6 | 7.892907365194152 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse2 2019-04-18 -6) | 1.34 | 14.71 | 8.032308763533658 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse4 2019-04-18 -1) | 3.38 | 29.81 | 17.54220036587525 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse4 2019-04-18 -12) | 1.42 | 27.89 | 10.350995554331576 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse4 2019-04-18 -16) | 1.41 | 27.7 | 10.349713982217328 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse4 2019-04-18 -6) | 1.42 | 28.02 | 10.487593091645849 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse8 2019-04-18 -1) | 3.4 | 26.88 | 16.278255695007279 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse8 2019-04-18 -12) | 1.38 | 24.39 | 9.821740670063545 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse8 2019-04-18 -16) | 1.35 | 24.47 | 9.823165184543953 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzsse8 2019-04-18 -6) | 1.42 | 24.51 | 9.954676987842783 | MB/s |
| Lzbench | Lzbench | Compression Speed(lzvn 2017-03-08) | 8.2 | 94.91 | 50.19506866294169 | MB/s |
| Lzbench | Lzbench | Compression Speed(memcpy) | 3877.81 | 13637.41 | 12118.250668348046 | MB/s |
| Lzbench | Lzbench | Compression Speed(pithy 2011-12-24 -0) | 61.25 | 1316.84 | 511.4474498308019 | MB/s |
| Lzbench | Lzbench | Compression Speed(pithy 2011-12-24 -3) | 65.02 | 941.58 | 442.74886025144118 | MB/s |
| Lzbench | Lzbench | Compression Speed(pithy 2011-12-24 -6) | 70.72 | 948.59 | 412.5733907842442 | MB/s |
| Lzbench | Lzbench | Compression Speed(pithy 2011-12-24 -9) | 63.32 | 892.44 | 377.4995104096974 | MB/s |
| Lzbench | Lzbench | Compression Speed(quicklz 1.5.0 -1) | 70.7 | 714.26 | 381.1981927663427 | MB/s |
| Lzbench | Lzbench | Compression Speed(quicklz 1.5.0 -2) | 34.46 | 413.03 | 197.1609693415719 | MB/s |
| Lzbench | Lzbench | Compression Speed(quicklz 1.5.0 -3) | 8.24 | 93.5 | 46.28537624463774 | MB/s |
| Lzbench | Lzbench | Compression Speed(shrinker 0.1) | 60.65 | 594.09 | 312.73981159246378 | MB/s |
| Lzbench | Lzbench | Compression Speed(slz_gzip 1.2.0 -1) | 43.85 | 456.87 | 244.72307279060159 | MB/s |
| Lzbench | Lzbench | Compression Speed(slz_gzip 1.2.0 -2) | 41.06 | 456.06 | 239.86394985929906 | MB/s |
| Lzbench | Lzbench | Compression Speed(slz_gzip 1.2.0 -3) | 40.18 | 454.59 | 237.14170080878444 | MB/s |
| Lzbench | Lzbench | Compression Speed(snappy 2019-09-30) | 59.27 | 7232.33 | 959.9569114666351 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -1) | 49.14 | 642.47 | 315.953065312789 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -10) | 0.69 | 24.77 | 7.564038564435902 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -13) | 1.01 | 11.25 | 5.874686787816333 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -16) | 0.43 | 4.43 | 2.554913890485345 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -2) | 46.37 | 546.39 | 257.57239707965996 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -3) | 16.08 | 411.33 | 155.03999408027154 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -4) | 12.33 | 361.32 | 131.71562960039467 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -5) | 5.08 | 167.85 | 60.50486941071958 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -6) | 4.06 | 110.71 | 41.1006175959564 | MB/s |
| Lzbench | Lzbench | Compression Speed(tornado 0.6a -7) | 2.49 | 54.47 | 21.115587782340865 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2b 1.03 -1) | 6.63 | 77.86 | 40.4350989254986 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2b 1.03 -6) | 2.26 | 43.96 | 16.608759168946514 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2b 1.03 -9) | 0.17 | 7.16 | 2.3237372274359635 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2d 1.03 -1) | 6.55 | 78.57 | 40.48524880952381 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2d 1.03 -6) | 2.4 | 44.75 | 16.96642162698413 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2d 1.03 -9) | 0.17 | 7.3 | 2.361816828440421 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2e 1.03 -1) | 6.66 | 79.35 | 40.774911371801739 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2e 1.03 -6) | 2.47 | 44.72 | 17.072491807185846 | MB/s |
| Lzbench | Lzbench | Compression Speed(ucl_nrv2e 1.03 -9) | 0.17 | 7.24 | 2.377699400553641 | MB/s |
| Lzbench | Lzbench | Compression Speed(wflz 2015-09-16) | 24.2 | 745.5 | 258.44337274200327 | MB/s |
| Lzbench | Lzbench | Compression Speed(xpack 2016-06-02 -1) | 18.64 | 298.38 | 129.04832991456397 | MB/s |
| Lzbench | Lzbench | Compression Speed(xpack 2016-06-02 -6) | 4.89 | 152.03 | 44.63290004580486 | MB/s |
| Lzbench | Lzbench | Compression Speed(xpack 2016-06-02 -9) | 1.85 | 50.04 | 17.585548009321039 | MB/s |
| Lzbench | Lzbench | Compression Speed(xz 5.2.4 -0) | 2.31 | 62.13 | 20.97696293050216 | MB/s |
| Lzbench | Lzbench | Compression Speed(xz 5.2.4 -3) | 1.2 | 27.65 | 8.960801944106926 | MB/s |
| Lzbench | Lzbench | Compression Speed(xz 5.2.4 -6) | 0.5 | 5.75 | 2.7533290839192885 | MB/s |
| Lzbench | Lzbench | Compression Speed(xz 5.2.4 -9) | 0.48 | 5.58 | 2.6765641372990425 | MB/s |
| Lzbench | Lzbench | Compression Speed(yalz77 2015-09-19 -1) | 10.19 | 331.45 | 105.73028248700122 | MB/s |
| Lzbench | Lzbench | Compression Speed(yalz77 2015-09-19 -12) | 2.95 | 105.33 | 34.199992416531959 | MB/s |
| Lzbench | Lzbench | Compression Speed(yalz77 2015-09-19 -4) | 4.83 | 187.03 | 60.70227069886047 | MB/s |
| Lzbench | Lzbench | Compression Speed(yalz77 2015-09-19 -8) | 3.92 | 128.68 | 42.66433879432482 | MB/s |
| Lzbench | Lzbench | Compression Speed(yappy 2014-03-22 -1) | 18.09 | 219.91 | 102.71861554739282 | MB/s |
| Lzbench | Lzbench | Compression Speed(yappy 2014-03-22 -10) | 17.15 | 152.75 | 78.4394311783732 | MB/s |
| Lzbench | Lzbench | Compression Speed(yappy 2014-03-22 -100) | 12.72 | 86.16 | 61.2909575912467 | MB/s |
| Lzbench | Lzbench | Compression Speed(zlib 1.2.11 -1) | 8.47 | 164.29 | 66.44598833958949 | MB/s |
| Lzbench | Lzbench | Compression Speed(zlib 1.2.11 -6) | 3.61 | 69.63 | 23.96407882914362 | MB/s |
| Lzbench | Lzbench | Compression Speed(zlib 1.2.11 -9) | 1.33 | 26.34 | 11.94374678034463 | MB/s |
| Lzbench | Lzbench | Compression Speed(zling 2018-10-12 -0) | 7.2 | 270.08 | 81.33147837546425 | MB/s |
| Lzbench | Lzbench | Compression Speed(zling 2018-10-12 -1) | 6.74 | 241.56 | 74.05331616149515 | MB/s |
| Lzbench | Lzbench | Compression Speed(zling 2018-10-12 -2) | 4.84 | 212.5 | 66.41862619808306 | MB/s |
| Lzbench | Lzbench | Compression Speed(zling 2018-10-12 -3) | 5.94 | 174.04 | 57.85701517571886 | MB/s |
| Lzbench | Lzbench | Compression Speed(zling 2018-10-12 -4) | 5.19 | 150.21 | 49.01363166197408 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -1) | 58.3 | 733.79 | 373.68731555821855 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -11) | 3.65 | 85.24 | 29.471737367685244 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -15) | 0.96 | 17.54 | 8.1063438924886 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -18) | 0.62 | 5.46 | 3.553915286777058 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -2) | 48.36 | 705.89 | 314.4708002796085 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -22) | 0.41 | 4.78 | 2.559853811695064 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -5) | 9.36 | 292.57 | 100.08991751547834 | MB/s |
| Lzbench | Lzbench | Compression Speed(zstd 1.4.5 -8) | 5.35 | 128.73 | 45.35846474935092 | MB/s |
| Lzbench | Lzbench | Decompression Speed(blosclz 2.0.0 -1) | 2521.63 | 15515.42 | 10908.108975229376 | MB/s |
| Lzbench | Lzbench | Decompression Speed(blosclz 2.0.0 -3) | 531.74 | 15083.05 | 9473.623344662175 | MB/s |
| Lzbench | Lzbench | Decompression Speed(blosclz 2.0.0 -6) | 90.86 | 15076.78 | 2845.8947226430489 | MB/s |
| Lzbench | Lzbench | Decompression Speed(blosclz 2.0.0 -9) | 7.0 | 14820.24 | 1756.4652570429378 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brieflz 1.3.0 -1) | 39.73 | 331.54 | 199.5518123882801 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brieflz 1.3.0 -3) | 38.41 | 347.43 | 202.00163620099566 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brieflz 1.3.0 -6) | 36.51 | 388.73 | 217.0706652535781 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brieflz 1.3.0 -8) | 37.01 | 412.99 | 224.73261200995644 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brotli 2019-10-01 -0) | 37.05 | 870.02 | 315.56087118855018 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brotli 2019-10-01 -11) | 39.39 | 1223.63 | 428.52725529473539 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brotli 2019-10-01 -2) | 42.2 | 969.3 | 362.75481204908319 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brotli 2019-10-01 -5) | 45.72 | 1224.12 | 436.21922816638468 | MB/s |
| Lzbench | Lzbench | Decompression Speed(brotli 2019-10-01 -8) | 45.83 | 1357.19 | 463.6603928432516 | MB/s |
| Lzbench | Lzbench | Decompression Speed(bzip2 1.0.8 -1) | 5.78 | 80.16 | 37.75717205702173 | MB/s |
| Lzbench | Lzbench | Decompression Speed(bzip2 1.0.8 -5) | 5.41 | 60.04 | 32.23068476633151 | MB/s |
| Lzbench | Lzbench | Decompression Speed(bzip2 1.0.8 -9) | 5.16 | 55.32 | 29.60845154514868 | MB/s |
| Lzbench | Lzbench | Decompression Speed(crush 1.0 -0) | 49.16 | 804.14 | 310.4221463025635 | MB/s |
| Lzbench | Lzbench | Decompression Speed(crush 1.0 -1) | 50.27 | 1127.8 | 384.0300544651715 | MB/s |
| Lzbench | Lzbench | Decompression Speed(crush 1.0 -2) | 49.94 | 1243.47 | 407.1744718926279 | MB/s |
| Lzbench | Lzbench | Decompression Speed(csc 2016-10-13 -1) | 5.15 | 222.59 | 60.89921643387932 | MB/s |
| Lzbench | Lzbench | Decompression Speed(csc 2016-10-13 -3) | 4.64 | 211.04 | 59.53574679998444 | MB/s |
| Lzbench | Lzbench | Decompression Speed(csc 2016-10-13 -5) | 5.27 | 283.88 | 72.23723339687973 | MB/s |
| Lzbench | Lzbench | Decompression Speed(density 0.14.2 -1) | 331.69 | 1965.45 | 1663.6330320597618 | MB/s |
| Lzbench | Lzbench | Decompression Speed(density 0.14.2 -2) | 255.44 | 1441.27 | 1094.297458174461 | MB/s |
| Lzbench | Lzbench | Decompression Speed(density 0.14.2 -3) | 66.54 | 495.21 | 322.87696385495289 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlz 0.1 -1) | 85.08 | 969.3 | 498.6494576297564 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlz 0.1 -2) | 7.0 | 1057.57 | 541.8998050052544 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlzma2 1.0.1 -1) | 6.63 | 229.53 | 69.31242963366684 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlzma2 1.0.1 -10) | 7.08 | 360.11 | 94.38486848509628 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlzma2 1.0.1 -3) | 7.04 | 239.45 | 75.95330653638027 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlzma2 1.0.1 -5) | 7.15 | 293.03 | 87.1152694389285 | MB/s |
| Lzbench | Lzbench | Decompression Speed(fastlzma2 1.0.1 -8) | 7.07 | 324.79 | 91.35102460771717 | MB/s |
| Lzbench | Lzbench | Decompression Speed(gipfeli 2016-07-13) | 0.0 | 739.51 | 211.60811970638057 | MB/s |
| Lzbench | Lzbench | Decompression Speed(libdeflate 1.3 -1) | 88.82 | 1620.73 | 654.4741570124028 | MB/s |
| Lzbench | Lzbench | Decompression Speed(libdeflate 1.3 -12) | 83.96 | 1887.99 | 714.363444890326 | MB/s |
| Lzbench | Lzbench | Decompression Speed(libdeflate 1.3 -3) | 89.53 | 1761.99 | 713.4006625778817 | MB/s |
| Lzbench | Lzbench | Decompression Speed(libdeflate 1.3 -6) | 100.5 | 1785.09 | 721.5343348650648 | MB/s |
| Lzbench | Lzbench | Decompression Speed(libdeflate 1.3 -9) | 85.38 | 1733.5 | 699.465714953453 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -10) | 388.59 | 13815.17 | 2944.463470866694 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -12) | 382.4 | 2797.22 | 1892.508296222703 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -15) | 407.1 | 3182.28 | 2004.109846102897 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -19) | 374.55 | 3285.66 | 1920.75027623556 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -20) | 294.38 | 3888.1 | 1673.669484347301 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -22) | 290.93 | 2496.58 | 1520.992531317579 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -25) | 206.95 | 2864.44 | 1544.9062081864052 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -29) | 304.59 | 3910.69 | 1678.292202458649 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -30) | 197.03 | 2544.94 | 1158.742414619416 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -32) | 168.27 | 1933.06 | 1229.9371367063453 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -35) | 212.67 | 2444.88 | 1450.9891628350998 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -39) | 232.08 | 2542.42 | 1422.0541688746883 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -40) | 178.16 | 1758.01 | 989.6909252620504 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -42) | 145.89 | 1892.95 | 1054.2469233916534 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -45) | 145.01 | 2184.22 | 1161.533957521434 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lizard 1.0 -49) | 142.3 | 2951.99 | 1196.804290335152 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4 1.9.2) | 767.73 | 9639.5 | 3890.1327485482677 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4fast 1.9.2 -17) | 705.41 | 13454.66 | 5151.1778413719189 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4fast 1.9.2 -3) | 778.91 | 11691.66 | 4089.0026953132617 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4hc 1.9.2 -1) | 609.3 | 4835.18 | 3101.0375628483454 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4hc 1.9.2 -12) | 672.6 | 5452.2 | 3462.385953735822 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4hc 1.9.2 -4) | 652.23 | 5191.15 | 3252.9654772576689 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lz4hc 1.9.2 -9) | 673.48 | 5638.77 | 3379.780318626496 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzf 3.6 -0) | 106.14 | 1031.47 | 584.2229309201546 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzf 3.6 -1) | 114.62 | 1095.2 | 593.9199547528963 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzfse 2017-03-08) | 107.11 | 1564.92 | 637.299869303995 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzg 1.0.10 -1) | 82.77 | 827.12 | 459.3588773579385 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzg 1.0.10 -4) | 79.6 | 896.1 | 467.7927769430406 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzg 1.0.10 -6) | 91.61 | 973.55 | 501.0404931020352 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzg 1.0.10 -8) | 92.65 | 1077.49 | 554.1676365445782 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzham 1.0 -d26 -0) | 23.69 | 479.0 | 181.49579541019436 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzham 1.0 -d26 -1) | 24.18 | 819.88 | 272.5255162656363 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzjb 2010) | 59.66 | 517.23 | 318.18044359875099 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzlib 1.11 -0) | 5.01 | 136.15 | 45.78335870413739 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzlib 1.11 -3) | 5.35 | 153.71 | 52.96712601729152 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzlib 1.11 -6) | 5.5 | 179.65 | 61.00455890389569 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzlib 1.11 -9) | 5.51 | 199.02 | 63.49738113826216 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzma 19.00 -0) | 6.59 | 201.7 | 61.533044369400148 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzma 19.00 -2) | 6.87 | 249.56 | 74.29669426652085 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzma 19.00 -4) | 6.83 | 277.39 | 79.54378085455399 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzma 19.00 -5) | 7.28 | 329.63 | 93.77020271853762 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzma 19.00 -9) | 7.29 | 359.45 | 97.52388820530848 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzmat 1.01) | 59.59 | 729.11 | 356.0158033088603 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1 2.10 -1) | 103.46 | 1101.81 | 601.6733876953124 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1 2.10 -99) | 111.04 | 1330.07 | 647.122251171875 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1a 2.10 -1) | 99.28 | 1074.71 | 599.23931953125 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1a 2.10 -99) | 105.74 | 1273.66 | 639.4170185546875 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1b 2.10 -1) | 89.25 | 1157.57 | 583.5750368171254 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1b 2.10 -3) | 96.1 | 1187.33 | 594.3647603422008 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1b 2.10 -6) | 99.4 | 1174.64 | 592.9625333020822 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1b 2.10 -9) | 94.75 | 1240.84 | 594.2201527090707 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1b 2.10 -99) | 92.78 | 1339.94 | 612.2357688772418 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1b 2.10 -999) | 105.79 | 1626.46 | 694.2658488351623 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1c 2.10 -1) | 88.42 | 1064.74 | 580.357873826291 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1c 2.10 -3) | 92.98 | 1114.46 | 587.5432644757434 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1c 2.10 -6) | 97.45 | 1122.18 | 581.858948727479 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1c 2.10 -9) | 95.41 | 1164.65 | 581.3493808564331 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1c 2.10 -99) | 95.79 | 1218.17 | 588.238172499462 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1c 2.10 -999) | 96.7 | 1459.35 | 641.6798517185391 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1f 2.10 -1) | 80.67 | 1110.06 | 541.9114480222231 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1f 2.10 -999) | 88.98 | 1460.62 | 582.3670421377989 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1x 2.10 -1) | 88.44 | 11147.22 | 1494.5935384795963 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1x 2.10 -11) | 90.1 | 11764.71 | 1572.697933800227 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1x 2.10 -12) | 87.17 | 11422.18 | 1522.2501324386715 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1x 2.10 -15) | 87.55 | 11185.46 | 1495.8857369224148 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1x 2.10 -999) | 91.1 | 1556.29 | 602.8892188661529 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1y 2.10 -1) | 91.17 | 11107.77 | 1487.6580740639307 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1y 2.10 -999) | 86.84 | 1526.41 | 591.1742630496524 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo1z 2.10 -999) | 92.74 | 1534.05 | 609.2663457056116 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzo2a 2.10 -999) | 77.79 | 1242.42 | 478.92604144631187 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzrw 15-Jul-1991 -1) | 82.54 | 681.25 | 452.8909830349097 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzrw 15-Jul-1991 -3) | 91.2 | 779.71 | 495.48281201622538 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzrw 15-Jul-1991 -4) | 86.67 | 785.37 | 453.9585404949932 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzrw 15-Jul-1991 -5) | 82.16 | 1045.99 | 479.7242303697752 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse2 2019-04-18 -1) | 363.63 | 5279.73 | 2779.9943648942637 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse2 2019-04-18 -12) | 367.1 | 6569.2 | 3306.095117183663 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse2 2019-04-18 -16) | 364.35 | 6562.79 | 3305.956642342945 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse2 2019-04-18 -6) | 3.0 | 6578.35 | 3304.9348680156219 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse4 2019-04-18 -1) | 561.22 | 6194.66 | 3426.763322973425 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse4 2019-04-18 -12) | 563.24 | 6750.13 | 3758.360859036903 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse4 2019-04-18 -16) | 570.23 | 6760.07 | 3758.4324468880324 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse4 2019-04-18 -6) | 562.34 | 6788.38 | 3757.8706786395734 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse8 2019-04-18 -1) | 716.51 | 5639.8 | 3465.3526633749067 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse8 2019-04-18 -12) | 744.6 | 6187.6 | 3780.920305325491 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse8 2019-04-18 -16) | 732.38 | 6186.69 | 3780.823233060518 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzsse8 2019-04-18 -6) | 734.11 | 6202.33 | 3780.0024879017989 | MB/s |
| Lzbench | Lzbench | Decompression Speed(lzvn 2017-03-08) | 114.56 | 1572.98 | 796.0660598489021 | MB/s |
| Lzbench | Lzbench | Decompression Speed(memcpy) | 3885.41 | 13634.69 | 12113.023032786883 | MB/s |
| Lzbench | Lzbench | Decompression Speed(pithy 2011-12-24 -0) | 171.17 | 5852.7 | 1880.4370732667035 | MB/s |
| Lzbench | Lzbench | Decompression Speed(pithy 2011-12-24 -3) | 264.08 | 3538.99 | 1680.7459033584513 | MB/s |
| Lzbench | Lzbench | Decompression Speed(pithy 2011-12-24 -6) | 302.92 | 2936.23 | 1646.4157059379059 | MB/s |
| Lzbench | Lzbench | Decompression Speed(pithy 2011-12-24 -9) | 285.29 | 2991.04 | 1673.3309813451927 | MB/s |
| Lzbench | Lzbench | Decompression Speed(quicklz 1.5.0 -1) | 82.05 | 1123.19 | 517.9284653075682 | MB/s |
| Lzbench | Lzbench | Decompression Speed(quicklz 1.5.0 -2) | 70.6 | 1368.0 | 548.3059496635049 | MB/s |
| Lzbench | Lzbench | Decompression Speed(quicklz 1.5.0 -3) | 117.31 | 1640.5 | 727.8623493643985 | MB/s |
| Lzbench | Lzbench | Decompression Speed(shrinker 0.1) | 236.49 | 1958.72 | 1301.3199325959559 | MB/s |
| Lzbench | Lzbench | Decompression Speed(slz_gzip 1.2.0 -1) | 53.35 | 763.99 | 322.0089926599366 | MB/s |
| Lzbench | Lzbench | Decompression Speed(slz_gzip 1.2.0 -2) | 52.74 | 635.08 | 314.97779384851529 | MB/s |
| Lzbench | Lzbench | Decompression Speed(slz_gzip 1.2.0 -3) | 53.68 | 609.43 | 313.77893775704987 | MB/s |
| Lzbench | Lzbench | Decompression Speed(snappy 2019-09-30) | 178.61 | 11241.88 | 1919.2072946061356 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -1) | 0.0 | 878.99 | 367.2289981699038 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -10) | 20.15 | 759.66 | 221.08472008085455 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -13) | 20.36 | 580.48 | 201.06382077247779 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -16) | 20.69 | 719.37 | 224.68525159040017 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -2) | 69.28 | 837.52 | 382.93767317380357 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -3) | 31.07 | 658.41 | 243.01155757276764 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -4) | 33.94 | 658.6 | 249.52273882585105 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -5) | 19.51 | 546.89 | 176.38955858256836 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -6) | 19.31 | 582.25 | 185.29022093666087 | MB/s |
| Lzbench | Lzbench | Decompression Speed(tornado 0.6a -7) | 19.81 | 679.97 | 209.86089167950085 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2b 1.03 -1) | 39.14 | 463.79 | 221.42027140081678 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2b 1.03 -6) | 42.52 | 616.62 | 265.28821851271786 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2b 1.03 -9) | 41.57 | 749.71 | 298.29448383960638 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2d 1.03 -1) | 42.89 | 483.13 | 234.29550396825395 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2d 1.03 -6) | 46.45 | 626.79 | 273.4730853343917 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2d 1.03 -9) | 42.81 | 751.68 | 305.13067428889027 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2e 1.03 -1) | 44.08 | 492.74 | 237.65854456644758 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2e 1.03 -6) | 47.23 | 638.65 | 278.0127649009911 | MB/s |
| Lzbench | Lzbench | Decompression Speed(ucl_nrv2e 1.03 -9) | 44.55 | 763.34 | 309.9511753928266 | MB/s |
| Lzbench | Lzbench | Decompression Speed(wflz 2015-09-16) | 144.22 | 1747.16 | 1025.7717144457559 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xpack 2016-06-02 -1) | 72.4 | 1123.95 | 574.7206398741362 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xpack 2016-06-02 -6) | 80.02 | 1611.62 | 772.3957757418841 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xpack 2016-06-02 -9) | 79.58 | 1795.79 | 812.135909896632 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xz 5.2.4 -0) | 5.45 | 195.44 | 58.45359301236977 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xz 5.2.4 -3) | 5.85 | 295.45 | 78.92745831922398 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xz 5.2.4 -6) | 6.08 | 308.73 | 86.34849393390043 | MB/s |
| Lzbench | Lzbench | Decompression Speed(xz 5.2.4 -9) | 6.07 | 297.71 | 84.8484281929199 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yalz77 2015-09-19 -1) | 89.74 | 971.28 | 537.6655828037533 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yalz77 2015-09-19 -12) | 78.24 | 1214.75 | 555.2243231754775 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yalz77 2015-09-19 -4) | 6.0 | 1141.04 | 543.4508225073722 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yalz77 2015-09-19 -8) | 81.38 | 1194.7 | 551.1975834613772 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yappy 2014-03-22 -1) | 390.07 | 4992.71 | 2455.5709408688019 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yappy 2014-03-22 -10) | 398.89 | 4992.97 | 2596.1714822801238 | MB/s |
| Lzbench | Lzbench | Decompression Speed(yappy 2014-03-22 -100) | 400.73 | 4994.19 | 2628.516029270825 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zlib 1.2.11 -1) | 44.23 | 546.05 | 264.14605275243096 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zlib 1.2.11 -6) | 44.01 | 659.53 | 290.80615957510528 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zlib 1.2.11 -9) | 44.23 | 686.91 | 296.2185082762614 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zling 2018-10-12 -0) | 25.39 | 600.96 | 203.71764386406293 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zling 2018-10-12 -1) | 22.93 | 637.04 | 211.32730561878518 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zling 2018-10-12 -2) | 25.06 | 654.86 | 214.57273382587855 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zling 2018-10-12 -3) | 24.97 | 638.54 | 213.74746904952074 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zling 2018-10-12 -4) | 24.83 | 644.43 | 215.22356975977957 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -1) | 240.81 | 2011.36 | 1209.3410756940282 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -11) | 146.66 | 2511.72 | 1155.7770242975703 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -15) | 142.86 | 2912.42 | 1214.8403213742902 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -18) | 143.16 | 2932.1 | 1142.1661763058956 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -2) | 219.62 | 1898.89 | 1117.7342432594367 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -22) | 138.73 | 2672.59 | 1067.0979147165886 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -5) | 166.42 | 1950.51 | 1008.7534707409627 | MB/s |
| Lzbench | Lzbench | Decompression Speed(zstd 1.4.5 -8) | 161.45 | 2380.4 | 1169.657967845017 | MB/s |
| Pbzip2 | Pbzip2Compression | Compressed size and Original size ratio | 25.746507313581135 | 25.746507313581135 | 25.74650731358112 |  |
| Pbzip2 | Pbzip2Compression | CompressionTime | 2.605653 | 61.668289 | 16.600059365629567 | seconds |
| Pbzip2 | Pbzip2Decompression | CompressionTime | 1.105641 | 8649137.0 | 2086.0029479223205 | seconds |
| Pbzip2 | Pbzip2Decompression | Decompressed size and Original size ratio | 378.15426428428256 | 381.6750992318161 | 378.6081843573533 |  |