# OpenSSL
OpenSSL 3.0 is an open-source industry standard transport layer security (TLS, SSL) toolset. The OpenSSL toolset includes a feature/command (openssl speed) that enables measuring
the performance of the CPU in processing operations associated with various cryptography/encryption algorithms supported by the toolset (e.g. md5, sha1, sha256, aes-256-cbc).

This toolset was compiled directly from the open source GitHub repo in order to take advantage of 3.0 beta features sets and expanded support for additional
cryptography algorithms/operations.

* [OpenSSL GitHub](https://github.com/openssl/openssl)
* [OpenSSL Documentation](https://www.openssl.org/)
* [OpenSSL Manual Pages](https://www.openssl.org/docs/manmaster/)
* [OpenSSL speed](https://www.openssl.org/docs/manmaster/man1/openssl-speed.html)

## What is Being Measured?
OpenSSL 3.0 is designed to be a very simple benchmarking tool. It produces a set of measurements each testing the performance of the CPU for handling a particular cryptography
algorithm across a set of buffer sizes (e.g. 16-byte, 64-byte, 256-byte, 1024-byte, 8192-byte and 16384-byte).

The OpenSSL 3.0 build used by the VC Team runs the following CPU-intensive cryptography algorithm tests:

* md5
* sha1
* sha256
* sha512
* hmac(md5)
* des-ede3
* aes-128-cbc
* aes-192-cbc
* aes-256-cbc
* camellia-128-cbc
* camellia-192-cbc
* camellia-256-cbc
* ghash

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the OpenSSL Speed workload

| Metric Name  | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|--------------|---------------------|---------------------|---------------------|------|
| aes-128-cbc 1024-byte | 743221.38 | 779174.02 | 772402.6833333334 |KB/sec | 
| aes-128-cbc 16-byte | 480032.11 | 626316.78 | 603386.664074074 |KB/sec | 
| aes-128-cbc 16384-byte | 740298.6 | 782401.03 | 777006.7877777778 |KB/sec | 
| aes-128-cbc 256-byte | 749001.86 | 769747.19 | 763158.7555555553 |KB/sec | 
| aes-128-cbc 64-byte | 674625.69 | 733641.34 | 722047.8648148149 |KB/sec | 
| aes-128-cbc 8192-byte | 740819.61 | 782218.46 | 775589.774074074 |KB/sec | 
| aes-192-cbc 1024-byte | 636177.93 | 650852.43 | 646747.9537037036 |KB/sec | 
| aes-192-cbc 16-byte | 269321.25 | 540298.43 | 510732.25333333338 |KB/sec | 
| aes-192-cbc 16384-byte | 639476.94 | 654000.67 | 648852.6892592593 |KB/sec | 
| aes-192-cbc 256-byte | 593617.01 | 646267.33 | 637929.6492592595 |KB/sec | 
| aes-192-cbc 64-byte | 481352.87 | 620238.22 | 605616.2618518518 |KB/sec | 
| aes-192-cbc 8192-byte | 640499.22 | 653473.52 | 649360.9251851852 |KB/sec | 
| aes-256-cbc 1024-byte | 552828.37 | 559189.84 | 556096.7766666667 |KB/sec | 
| aes-256-cbc 16-byte | 388865.18 | 475468.6 | 458766.4844444445 |KB/sec | 
| aes-256-cbc 16384-byte | 550862.69 | 561158.51 | 557790.697037037 |KB/sec | 
| aes-256-cbc 256-byte | 545001.23 | 554559.1 | 550489.4881481482 |KB/sec | 
| aes-256-cbc 64-byte | 505109.8 | 537577.39 | 529298.1674074074 |KB/sec | 
| aes-256-cbc 8192-byte | 555123.57 | 561313.44 | 558133.2844444445 |KB/sec | 
| camellia-128-cbc 1024-byte | 196003.0 | 201482.0 | 199902.53814814814 |KB/sec | 
| camellia-128-cbc 16-byte | 105480.64 | 107996.46 | 106831.78555555556 |KB/sec | 
| camellia-128-cbc 16384-byte | 200965.4 | 204080.29 | 202701.8614814815 |KB/sec | 
| camellia-128-cbc 256-byte | 189692.78 | 193900.71 | 192372.28111111115 |KB/sec | 
| camellia-128-cbc 64-byte | 163067.16 | 167298.27 | 165350.6937037037 |KB/sec | 
| camellia-128-cbc 8192-byte | 200821.95 | 203738.81 | 202458.41037037038 |KB/sec | 
| camellia-192-cbc 1024-byte | 140974.7 | 151461.24 | 150052.69518518519 |KB/sec | 
| camellia-192-cbc 16-byte | 86760.31 | 91940.14 | 90815.50444444444 |KB/sec | 
| camellia-192-cbc 16384-byte | 148271.79 | 152921.37 | 151887.97592592598 |KB/sec | 
| camellia-192-cbc 256-byte | 141535.93 | 146474.95 | 145306.7385185185 |KB/sec | 
| camellia-192-cbc 64-byte | 127530.59 | 131539.45 | 130329.48555555556 |KB/sec | 
| camellia-192-cbc 8192-byte | 144049.0 | 152806.81 | 151590.18555555555 |KB/sec | 
| camellia-256-cbc 1024-byte | 149437.01 | 151431.51 | 150464.56666666666 |KB/sec | 
| camellia-256-cbc 16-byte | 86338.13 | 91581.69 | 90527.69074074074 |KB/sec | 
| camellia-256-cbc 16384-byte | 151277.78 | 152956.65 | 152096.0588888889 |KB/sec | 
| camellia-256-cbc 256-byte | 144450.91 | 146459.27 | 145471.35148148149 |KB/sec | 
| camellia-256-cbc 64-byte | 128175.52 | 131470.9 | 130429.96370370372 |KB/sec | 
| camellia-256-cbc 8192-byte | 151075.06 | 152811.4 | 151938.33925925927 |KB/sec | 
| des-ede3 1024-byte | 28783.8 | 29329.44 | 29133.498518518514 |KB/sec | 
| des-ede3 16-byte | 27667.14 | 28270.56 | 28057.690000000006 |KB/sec | 
| des-ede3 16384-byte | 28889.05 | 29438.32 | 29209.654074074075 |KB/sec | 
| des-ede3 256-byte | 28600.65 | 29292.04 | 29067.913333333334 |KB/sec | 
| des-ede3 64-byte | 28374.77 | 28995.75 | 28784.202222222222 |KB/sec | 
| des-ede3 8192-byte | 28872.63 | 29430.92 | 29197.996296296296 |KB/sec | 
| md5 1024-byte | 541071.68 | 562370.84 | 557335.1180555555 |KB/sec | 
| md5 16-byte | 41051.05 | 44687.58 | 44036.971388888895 |KB/sec | 
| md5 16384-byte | 669983.67 | 686605.66 | 681825.9061111112 |KB/sec | 
| md5 256-byte | 343673.88 | 355983.55 | 353118.2319444444 |KB/sec | 
| md5 64-byte | 135846.31 | 144694.23 | 142455.1908333333 |KB/sec | 
| md5 8192-byte | 659610.64 | 675423.13 | 670087.9744444445 |KB/sec | 
| sha1 1024-byte | 518003.89 | 720021.42 | 709253.8599999999 |KB/sec | 
| sha1 16-byte | 17825.02 | 44057.06 | 43025.363611111105 |KB/sec | 
| sha1 16384-byte | 863560.56 | 953382.3 | 941411.4713888889 |KB/sec | 
| sha1 256-byte | 217097.77 | 400824.59 | 392850.8069444444 |KB/sec | 
| sha1 64-byte | 65566.0 | 145517.68 | 141894.745 |KB/sec | 
| sha1 8192-byte | 869517.29 | 934553.17 | 925201.5333333333 |KB/sec | 
| sha256 1024-byte | 310838.3 | 374676.53 | 367162.79472222217 |KB/sec | 
| sha256 16-byte | 16463.92 | 36128.92 | 34169.54055555556 |KB/sec | 
| sha256 16384-byte | 422172.75 | 441639.53 | 437536.81305555559 |KB/sec | 
| sha256 256-byte | 164815.16 | 250462.04 | 241373.8311111111 |KB/sec | 
| sha256 64-byte | 56583.42 | 106414.3 | 101591.33805555555 |KB/sec | 
| sha256 8192-byte | 408205.29 | 435258.14 | 431032.23138888887 |KB/sec | 
| sha512 1024-byte | 385470.51 | 486102.23 | 479484.17851851848 |KB/sec | 
| sha512 16-byte | 14926.34 | 29452.83 | 28693.511851851854 |KB/sec | 
| sha512 16384-byte | 632075.77 | 648555.6 | 643126.89 |KB/sec | 
| sha512 256-byte | 172639.93 | 271882.98 | 266164.3025925926 |KB/sec | 
| sha512 64-byte | 59402.31 | 117240.32 | 114292.82444444442 |KB/sec | 
| sha512 8192-byte | 606058.11 | 634570.56 | 629781.2355555556 |KB/sec | 
