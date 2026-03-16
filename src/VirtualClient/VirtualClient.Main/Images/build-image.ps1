# Build script for VirtualClient Ubuntu image
param(
    [string]$ImageName = "vc-ubuntu",
    [string]$Tag = "22.04"
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DockerfilePath = Join-Path $ScriptDir "Dockerfile.ubuntu"

Write-Host "Building VirtualClient Ubuntu image..." -ForegroundColor Cyan
Write-Host "Dockerfile: $DockerfilePath" -ForegroundColor Gray

docker build -t "${ImageName}:${Tag}" -f $DockerfilePath $ScriptDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nImage built successfully!" -ForegroundColor Green
    Write-Host "Image: ${ImageName}:${Tag}" -ForegroundColor Green
    Write-Host "`nTo run VirtualClient with this image:" -ForegroundColor Yellow
    Write-Host "  VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --image=${ImageName}:${Tag}" -ForegroundColor White
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit 1
}