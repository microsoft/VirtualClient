---
id: 7zip
---

# 7zip
7-Zip is a file archiver with a high compression ratio.

The main features of 7-Zip are:
* High compression ratio in 7z format with LZMA and LZMA2 compression
* Supported formats:
* Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM
* Unpacking only: APFS, AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VHDX, VMDK, WIM, XAR and Z.
* For ZIP and GZIP formats, 7-Zip provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip
* Strong AES-256 encryption in 7z and ZIP formats
* Self-extracting capability for 7z format
* Integration with Windows Shell
* Powerful File Manager
* Powerful command line version
* Plugin for FAR Manager
* Localizations for 87 languages

### Documentation
* [7zip](https://www.7-zip.org/)

### Supported Platforms and Architectures
* win-x64
* win-arm64

### Package Dependencies
The following package dependencies are required to be installed on the Windows system in order to support the requirements
of the 7zip workload. Note that the Virtual Client will handle the installation of any required dependencies.

* 7zip.commandline
* unzip
* wget

### Workload Usage 
7z  [switches...] [file_names...] [@listfile]


  a : Add files to archive <br/>
  b : Benchmark <br/>
  d : Delete files from archive <br/>
  e : Extract files from archive (without using directory names) <br/>
  h : Calculate hash values for files <br/>
  i : Show information about supported formats <br/>
  l : List contents of archive <br/>
  rn : Rename files in archive <br/>
  t : Test integrity of archive <br/>
  u : Update files to archive <br/>
  x : eXtract files with full paths <br/>


  -- : Stop switches and @listfile parsing <br/>
  -ai[r[-|0]]{@listfile|!wildcard} : Include archives <br/>
  -ax[r[-|0]]{@listfile|!wildcard} : eXclude archives <br/>
  -ao{a|s|t|u} : set Overwrite mode <br/> 
  -an : disable archive_name field <br/>
  -bb[0-3] : set output log level <br/>
  -bd : disable progress indicator <br/>
  -bs{o|e|p}{0|1|2} : set output stream for output/error/progress line <br/>
  -bt : show execution time statistics <br/>
  -i[r[-|0]]{@listfile|!wildcard} : Include filenames <br/>
  -m{Parameters} : set compression Method <br/>
    -mmt[N] : set number of CPU threads <br/>
    -mx[N] : set compression level: -mx1 (fastest) ... -mx9 (ultra) <br/>
  -o{Directory} : set Output directory <br/>
  -p{Password} : set Password <br/>
  -r[-|0] : Recurse subdirectories <br/>
  -sa{a|e|s} : set Archive name mode <br/>
  -scc{UTF-8|WIN|DOS} : set charset for console input/output <br/>
  -scs{UTF-8|UTF-16LE|UTF-16BE|WIN|DOS|{id}} : set charset for list files <br/>
  -scrc[CRC32|CRC64|SHA1|SHA256|*] : set hash function for x, e, h commands <br/>
  -sdel : delete files after compression <br/>
  -seml[.] : send archive by email <br/>
  -sfx[{name}] : Create SFX archive <br/>
  -si[{name}] : read data from stdin <br/>
  -slp : set Large Pages mode <br/>
  -slt : show technical information for l (List) command <br/>
  -snh : store hard links as links <br/>
  -snl : store symbolic links as links <br/>
  -sni : store NT security information <br/>
  -sns[-] : store NTFS alternate streams <br/>
  -so : write data to stdout <br/>
  -spd : disable wildcard matching for file names <br/>
  -spe : eliminate duplication of root folder for extract command <br/>
  -spf : use fully qualified file paths <br/>
  -ssc[-] : set sensitive case mode <br/>
  -sse : stop archive creating, if it can't open some input file <br/>
  -ssw : compress shared files <br/>
  -stl : set archive timestamp from the most recently modified file <br/>
  -stm{HexMask} : set CPU thread affinity mask (hexadecimal number) <br/>
  -stx{Type} : exclude archive type <br/>
  -t{Type} : Set type of archive <br/>
  -u[-][p#][q#][r#][x#][y#][z#][!newArchiveName] : Update options <br/>
  -v{Size}[b|k|m|g] : Create volumes <br/>
  -w[{path}] : assign Work directory. Empty path means a temporary directory <br/>
  -x[r[-|0]]{@listfile|!wildcard} : eXclude filenames <br/>
  -y : assume Yes on all queries


### What is Being Tested?
7zip is used to measure performance in terms of compressionTime, and ratio of compressed size and original size. Below are the metrics measured by 7zip Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| Compressed size and Original size ratio        | -  |
| CompressionTime   | seconds |

# References
* [7zip](https://www.7-zip.org/)