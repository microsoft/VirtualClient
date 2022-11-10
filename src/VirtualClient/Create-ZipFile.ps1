<#
.SYNOPSIS
Creates a .zip file to the destination path provided.

.PARAMETER Path
The path to the directory to zip.

.PARAMETER ZipFile
The path to the .zip file to create.

.PARAMETER CompressionLevel
The level of compression (Fastest, NoCompression, Optimal, SmallestSize). Default = Optimal.

.EXAMPLE
Create-ZipFile -Path C:\Downloads\Any -ZipFile C:\Downloads\Any.zip

.EXAMPLE
Create-ZipFile -Path C:\Downloads\Any -ZipFile C:\Downloads\Any.zip -CompressionLevel Fastest

.EXAMPLE
Create-ZipFile C:\Downloads\Any C:\Downloads\Any.zip

.EXAMPLE
Create-ZipFile C:\Downloads\Any C:\Downloads\Any.zip Fastest
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string] $Path,

    [Parameter(Mandatory=$true, Position=1)]
    [Alias("Zip", "DestinationPath", "Destination")]
    [string] $ZipFile,

    [Parameter(Mandatory=$false, Position=2)]
    [Alias("Level")]
    [string] $CompressionLevel = "Optimal"
)

if (Test-Path($ZipFile))
{
    Remove-Item -Path $ZipFile -Force
}

[System.IO.Compression.ZipFile]::CreateFromDirectory($Path, $ZipFile, $CompressionLevel, $false);