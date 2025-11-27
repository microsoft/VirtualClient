
<#
.SYNOPSIS
Creates a certificate that can be used for unit/functional testing.

.DESCRIPTION
Creates a self-signed certificate that can be used for unit/functional testing. Note that this certificate
is NOT backed by any valid certificate authority and cannot be used for authentication/authorization of any
kind. This certificate is for local development testing purposes only.

.PARAMETER OutputPath
The path to which the certificate file should be output.

.EXAMPLE
New-TestCertificate -OutputPath S:\Certificates
#>

param(
    [Alias("Path")]
    [Parameter(Mandatory=$true)]
    [string]$OutputPath,
	
    [Parameter(Mandatory=$false)]
    [DateTime]$ExpirationDate = [DateTime]::UtcNow.AddYears(30)
)

Write-Host("")
$issuer = "DC=GBL, DC=AME, CN=AME Infra Test CA 777"
$subject = "CN=virtualclient.test.corp.azure.com"
$certPwd = ConvertTo-SecureString -String 'CRC' -Force -AsPlainText

$rootCert = New-SelfSignedCertificate -Type Custom `
	-KeySpec Signature `
	-Subject "$issuer" `
	-KeyExportPolicy Exportable `
	-HashAlgorithm sha256 `
	-KeyLength 2048 `
	-CertStoreLocation "Cert:\CurrentUser\My" `
	-KeyUsageProperty Sign `
	-KeyUsage CertSign `
	-NotAfter $ExpirationDate

$clientCert = New-SelfSignedCertificate -Type Custom `
	-DnsName "$subject" `
	-KeySpec Signature `
	-Subject "$subject" `
	-KeyExportPolicy Exportable `
	-HashAlgorithm sha256 `
	-KeyLength 2048 `
	-CertStoreLocation "Cert:\CurrentUser\My" `
	-Signer $rootCert `
	-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
	-NotAfter $ExpirationDate

Write-Host("")
Write-Host("[Client Certificate]")
$clientCert
	
Export-PfxCertificate `
	-Cert $clientCert `
	-FilePath "$OutputPath\virtualclient_test_certificate.pfx" `
	-Password $certPwd `
	-CryptoAlgorithmOption AES256_SHA256