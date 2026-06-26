param(
    [string]$Destination = (
        Join-Path $PSScriptRoot "..\third_party\ffmpeg"
    )
)

$ErrorActionPreference = "Stop"

$assetName = "ffmpeg-n7.1-latest-win64-lgpl-shared-7.1.zip"
$downloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/$assetName"
$archivePath = Join-Path $env:TEMP $assetName
$extractPath = Join-Path $env:TEMP "entcapture2-ffmpeg-7.1"

if ((Test-Path (Join-Path $Destination "bin\avcodec-61.dll")) -and
    (Test-Path (Join-Path $Destination "bin\ffmpeg.exe"))) {
    Write-Host "FFmpeg 7.1 shared libraries are already installed."
    exit 0
}

Write-Host "Downloading $assetName ..."
Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

if (Test-Path $extractPath) {
    Remove-Item -LiteralPath $extractPath -Recurse -Force
}

Expand-Archive -LiteralPath $archivePath -DestinationPath $extractPath
$packageRoot = Get-ChildItem -LiteralPath $extractPath -Directory |
    Select-Object -First 1
if ($null -eq $packageRoot) {
    throw "FFmpeg archive did not contain a package directory."
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $packageRoot.FullName "bin") `
    -Destination $Destination -Recurse -Force
Copy-Item -LiteralPath (Join-Path $packageRoot.FullName "LICENSE.txt") `
    -Destination $Destination -Force
$readmePath = Join-Path $packageRoot.FullName "README.txt"
if (Test-Path $readmePath) {
    Copy-Item -LiteralPath $readmePath -Destination $Destination -Force
}

$ffmpeg = Join-Path $Destination "bin\ffmpeg.exe"
$configuration = & $ffmpeg -hide_banner -buildconf 2>&1 | Out-String
foreach ($required in @("--enable-ffnvcodec", "--enable-amf", "--enable-libvpl")) {
    if ($configuration -notmatch [regex]::Escape($required)) {
        throw "Downloaded FFmpeg does not include $required."
    }
}

Write-Host "FFmpeg 7.1 LGPL shared build installed in $Destination"
