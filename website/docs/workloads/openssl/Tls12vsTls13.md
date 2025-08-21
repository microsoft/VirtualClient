# Tls 1_2 vs TLS 1_3
Transport Layer Security 1.3 (TLS)
The Internet Engineering Task Force (IETF) Request for Comments (RFC) 8446, released August 2018, “specifies version 1.3 of the Transport Layer Security (TLS) protocol. TLS allows client/server applications to communicate over the Internet in a way that is designed to prevent eavesdropping, tampering, and message forgery.”

In addition to improvement on privacy and performance, the following are some of the major differences between TLS 1.2 and 1.3:

* The supported symmetric encryption algorithm has been reduced based upon legacy status.
* The approved symmetric encryption algorithms are all authenticated encryption with associated data (AEAD) algorithms.
* The cipher suite concept has been changed to separate the authentication and key exchange mechanisms from the record protection algorithm.
* A zero round-trip time (0-RTT) mode was added, saving a round trip at connection setup for some application data.
* Static RSA and Diffie-Hellman cipher suites have been removed; all public key-based key exchange mechanisms now provide forward secrecy.
* All handshake messages after the ServerHello are now encrypted.
* Elliptic curve algorithms are now in the base spec, and new signature algorithms, such as Edwards-curve Digital Signature Algorithm (EdDSA), are included.
* The TLS 1.2 version negotiation mechanism has been deprecated in favor of a version list in an extension.


Two vulnerabilities that are related to using TLS 1.2 are:

Compression Ratio Info-leak Made Easy (CRIME) (CVE-2012-4929)
Security Losses from Obsolete and Truncated Transcript Hashes (SLOTH) (CVE-20157575)

## The initial handshake of TLS 1.3 has three phases:

* Key exchange. 
	These are exchanges of shared key material and parameters initiated by the client. All communications are encrypted after this point.
* Server parameters. 
	These are other handshake parameters like application-layer protocol support.
* Authentication. 
	The server, and, optionally, the client, are authenticated and provide key confirmation and handshake integrity.

In the key exchange phase, the client sends the ClientHello message, which contains a random nonce (ClientHello.random); its offered protocol versions; a list of symmetric cipher/HKDF hash pairs; either a set of Diffie-Hellman key shares; a set of preshared key labels, or both and, potentially, additional extensions. Additional fields and/or messages may also be present for middlebox compatibility.

The server processes the ClientHello and determines the appropriate cryptographic parameters for the connection. It then responds with its own ServerHello, which indicates the negotiated connection parameters. The combination of the ClientHello and the ServerHello determines the shared keys. If (EC)DHE key establishment is in use, then the ServerHello contains a key-share extension with the server’s ephemeral Diffie-Hellman share; the server’s share must be in the same group as one of the client’s shares. If PSK key establishment is in use, then the ServerHello contains a preshared key extension indicating which of the client’s offered PSKs was selected. Note that implementations can use (EC)DHE and PSK together, in which case both extensions will be supplied.

Reference:
* [TLS 1.3](https://www.rfc-editor.org/rfc/rfc8446.txt)