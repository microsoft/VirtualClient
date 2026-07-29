$ErrorActionPreference = 'Stop'

$examplesRoot = $PSScriptRoot
$sourceDirectory = Join-Path $examplesRoot 'os-release'
$outputDirectory = Join-Path $examplesRoot 'hostnamectl-generated'
$archivePath = Join-Path $examplesRoot 'hostnamectl-generated.zip'

if (Test-Path -LiteralPath $outputDirectory) {
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $outputDirectory | Out-Null

Get-ChildItem -LiteralPath $sourceDirectory -File | ForEach-Object {
    $prettyName = $null

    foreach ($line in Get-Content -LiteralPath $_.FullName) {
        if ($line -match '^PRETTY_NAME=(.*)$') {
            $prettyName = $matches[1].Trim('"')
            break
        }
    }

    if ([string]::IsNullOrWhiteSpace($prettyName)) {
        throw "PRETTY_NAME is missing from $($_.FullName)."
    }

    $content = @"
   Static hostname: virtualclient
         Icon name: computer-vm
           Chassis: vm
        Machine ID: 0123456789abcdef0123456789abcdef
           Boot ID: fedcba9876543210fedcba9876543210
    Virtualization: microsoft
  Operating System: $prettyName
            Kernel: Linux 6.8.0
      Architecture: x86-64
"@

    $destination = Join-Path $outputDirectory "$($_.Name).txt"
    Set-Content -LiteralPath $destination -Value $content -Encoding utf8NoBOM
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

Compress-Archive -LiteralPath $outputDirectory -DestinationPath $archivePath
