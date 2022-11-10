<#
.SYNOPSIS
Unzips a .zip file to the destination path provided.

.PARAMETER ZipFile
The path to the .zip file to unpack.

.PARAMETER DestinationPath
The path to the directory where the .zip file contents will be unpacked.

.EXAMPLE
Unzip-ZipFile -ZipFile C:\Downloads\Any.zip -DestinationPath C:\Downloads\Any

.EXAMPLE
Unzip-ZipFile C:\Downloads\Any.zip C:\Downloads\Any
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [Alias("Zip")]
    [string] $ZipFile,

    [Parameter(Mandatory=$true, Position=1)]
    [Alias("Destination")]
    [string] $DestinationPath
)

# Add-Type -AssemblyName System.IO.Compression
[System.IO.Compression.ZipFile]::ExtractToDirectory($ZipFile, $DestinationPath);