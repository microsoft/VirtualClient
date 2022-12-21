# OpenSSL
OpenSSL 3.0 is an open-source industry standard transport layer security (TLS, SSL) toolset. The OpenSSL toolset includes a feature/command (openssl speed) that enables measuring
the performance of the CPU in processing operations associated with various cryptography/encryption algorithms supported by the toolset (e.g. md5, sha1, sha256, aes-256-cbc).

This toolset was compiled directly from the open source GitHub repo in order to take advantage of 3.0 beta features sets and expanded support for additional
cryptography algorithms/operations.

* [OpenSSL GitHub](https://github.com/openssl/openssl)
* [OpenSSL Documentation](https://www.openssl.org/)
* [OpenSSL Manual Pages](https://www.openssl.org/docs/manmaster/)
* [OpenSSL speed](https://www.openssl.org/docs/manmaster/man1/openssl-speed.html)

-----------------------------------------------------------------------

### What is Being Tested?
OpenSSL 3.0 is designed to be a very simple benchmarking tool. It produces a set of measurements each testing the performance of the CPU for handling a particular cryptography
algorithm across a set of buffer sizes (e.g. 16-byte, 64-byte, 256-byte, 1024-byte, 8192-byte and 16384-byte).

The OpenSSL 3.0 build used by the VC Team can run the following CPU-intensive cryptography algorithm tests:

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

-----------------------------------------------------------------------

### Supported Platforms
* Linux x64
* Linux arm64
* Windows x64
  * Note that multi-threaded/parallel tests are not supported for Windows builds of OpenSSL 3.0. This means that the OpenSSL speed command
    will not heavily exercise the CPU resources on the system. It will use a single core/vCPU to run each test. With Linux builds, the
    toolset can be configured to use ALL cores/vCPUs available on the system in-parallel.
