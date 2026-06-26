param(
  [string]$Project = "$PSScriptRoot\..\src\ENTcapture2.WinForms\ENTcapture2.WinForms.csproj",
  [string]$Configuration = "Release",
  [string]$RuntimeIdentifier = "win-x64",
  [string]$InnoScript = "$PSScriptRoot\..\installer.iss",
  [string]$CertificateThumbprint = "89AA6D9BABBAAE6672A34DE9F07E47359389ACF2",
  [string]$TimeStampUrl = "http://timestamp.digicert.com",
  [switch]$SkipPublish,
  [switch]$SkipSign
)

$ErrorActionPreference = 'Stop'

function Info($message) { Write-Host "[INFO] $message" -ForegroundColor Cyan }
function Warn($message) { Write-Host "[WARN] $message" -ForegroundColor Yellow }
function Fail($message) { Write-Host "[ERR ] $message" -ForegroundColor Red; exit 1 }

function Invoke-Checked {
  param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [Parameter(Mandatory = $true)]
    [string[]]$Arguments
  )

  & $FilePath @Arguments | Out-Host
  if ($LASTEXITCODE -ne 0) {
    throw "Command failed ($LASTEXITCODE): $FilePath $($Arguments -join ' ')"
  }
}

function Find-SignTool {
  $candidates = Get-ChildItem `
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
    -Recurse `
    -Filter signtool.exe `
    -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
    Sort-Object FullName -Descending

  return $candidates | Select-Object -First 1 -ExpandProperty FullName
}

function Assert-CodeSigningCertificate {
  $certificate = Get-ChildItem "Cert:\CurrentUser\My\$CertificateThumbprint" `
    -ErrorAction SilentlyContinue
  if (-not $certificate) {
    Fail "Code-signing certificate was not found: $CertificateThumbprint"
  }
  if (-not $certificate.HasPrivateKey) {
    Fail 'The code-signing certificate does not have a private key.'
  }
  if ($certificate.NotAfter -le (Get-Date)) {
    Fail "The code-signing certificate expired: $($certificate.NotAfter)"
  }
  $hasCodeSigningUsage = $certificate.EnhancedKeyUsageList |
    Where-Object {
      $objectId = $_.ObjectId
      if ($objectId -is [string]) {
        $objectId -eq '1.3.6.1.5.5.7.3.3'
      } else {
        $objectId.Value -eq '1.3.6.1.5.5.7.3.3'
      }
    }
  if (-not $hasCodeSigningUsage) {
    Fail 'The certificate is not valid for code signing.'
  }

  Info "Certificate: $($certificate.Subject)"
  Info "Certificate expires: $($certificate.NotAfter)"
}

function Sign-File {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$SignTool
  )

  Info "Sign: $Path"
  Invoke-Checked $SignTool @(
    'sign',
    '/sha1', $CertificateThumbprint,
    '/s', 'My',
    '/fd', 'SHA256',
    '/tr', $TimeStampUrl,
    '/td', 'SHA256',
    $Path
  )

  Invoke-Checked $SignTool @('verify', '/pa', '/v', $Path)
}

$Project = [System.IO.Path]::GetFullPath($Project.Trim('"'))
$InnoScript = [System.IO.Path]::GetFullPath($InnoScript.Trim('"'))
$TimeStampUrl = $TimeStampUrl.Trim()
$RootDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$PublishDir = Join-Path $RootDir "artifacts\publish\$RuntimeIdentifier"
$InstallerDir = Join-Path $RootDir 'artifacts\installer'
$ArtifactsDir = Join-Path $RootDir 'artifacts'
$FfmpegDownloadScript = Join-Path $PSScriptRoot 'download_ffmpeg.ps1'

if (-not (Test-Path $Project)) { Fail "Project not found: $Project" }
if (-not (Test-Path $InnoScript)) { Fail "Inno script not found: $InnoScript" }
if (-not (Test-Path $FfmpegDownloadScript)) {
  Fail "FFmpeg download script not found: $FfmpegDownloadScript"
}

[xml]$props = Get-Content (Join-Path $RootDir 'Directory.Build.props') -Raw
$propertyGroup = $props.Project.PropertyGroup | Select-Object -First 1
$appVersion = [string]$propertyGroup.AppVersion
$version = $appVersion
if ([string]::IsNullOrWhiteSpace($version)) {
  $version = [string]$propertyGroup.InformationalVersion
}
if ([string]::IsNullOrWhiteSpace($version)) {
  $version = [string]$propertyGroup.VersionPrefix
}
if (-not [string]::IsNullOrWhiteSpace($appVersion)) {
  $version = $version.Replace('$(AppVersion)', $appVersion)
}
if ([string]::IsNullOrWhiteSpace($version) -or $version.Contains('$(')) {
  Fail 'Version was not found in Directory.Build.props.'
}
Info "AppVersion = $version"

if (-not $SkipPublish) {
  Info 'Prepare FFmpeg 7.1 shared runtime'
  & $FfmpegDownloadScript
  if ($LASTEXITCODE -ne 0) {
    Fail 'FFmpeg runtime preparation failed.'
  }

  Info "Publish: $Project ($Configuration / $RuntimeIdentifier / self-contained)"
  if (Test-Path $PublishDir) {
    $resolvedPublishDir = [System.IO.Path]::GetFullPath($PublishDir)
    $resolvedArtifactsDir = [System.IO.Path]::GetFullPath($ArtifactsDir)
    if (-not $resolvedPublishDir.StartsWith(
      $resolvedArtifactsDir + [System.IO.Path]::DirectorySeparatorChar,
      [System.StringComparison]::OrdinalIgnoreCase)) {
      Fail "Unsafe publish directory: $resolvedPublishDir"
    }

    Remove-Item -LiteralPath $PublishDir -Recurse -Force
  }

  Invoke-Checked 'dotnet' @(
    'publish',
    $Project,
    '-c', $Configuration,
    '-r', $RuntimeIdentifier,
    '--self-contained', 'true',
    '-p:PublishSingleFile=false',
    '-p:SatelliteResourceLanguages=ja',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    '-o', $PublishDir
  )

  $requiredPublishFiles = @(
    'ENTcapture2.WinForms.exe',
    'ENTcapture2.WinForms.dll',
    'ENTcapture2.Core.dll',
    'coreclr.dll',
    'hostfxr.dll',
    'hostpolicy.dll',
    'ffmpeg\ffmpeg.exe',
    'ffmpeg\avcodec-61.dll',
    'licenses\FFmpeg-LICENSE.txt'
  )
  foreach ($relativePath in $requiredPublishFiles) {
    $requiredPath = Join-Path $PublishDir $relativePath
    if (-not (Test-Path $requiredPath)) {
      Fail "Required publish file was not generated: $requiredPath"
    }
  }
  Info '.NET 10 self-contained runtime and FFmpeg files verified.'
}

if (-not (Test-Path $PublishDir)) {
  Fail "Publish directory not found: $PublishDir"
}

$signTool = Find-SignTool
if (-not $SkipSign) {
  if (-not $signTool) {
    Fail 'signtool.exe was not found.'
  }
  Assert-CodeSigningCertificate

  $filesToSign = @(
    (Join-Path $PublishDir 'ENTcapture2.WinForms.exe'),
    (Join-Path $PublishDir 'ENTcapture2.WinForms.dll'),
    (Join-Path $PublishDir 'ENTcapture2.Core.dll')
  )

  foreach ($file in $filesToSign) {
    if (Test-Path $file) {
      Sign-File -Path $file -SignTool $signTool
    } else {
      Warn "Sign target not found: $file"
    }
  }
}

$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
  $iscc = "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
  Fail 'ISCC.exe was not found. Install Inno Setup 6.'
}

New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null

$publishDirForInno = $PublishDir
Info "Compile Inno Setup: $InnoScript"
$innoArguments = @(
  "/DAppVersion=$version",
  "/DPublishDir=$publishDirForInno"
)
if ($SkipSign) {
  $innoArguments += '/DSkipSign=1'
} else {
  $innoSignCommand = '$q' + $signTool + '$q sign $p $f'
  $innoArguments += "/SMSStore=$innoSignCommand"
  Info 'Inno Setup signing tool configured for this build.'
}
$innoArguments += $InnoScript
Invoke-Checked $iscc $innoArguments

$installer = Get-ChildItem $InstallerDir `
  -Filter "ENTcapture2_v${version}_x64.exe" `
  -ErrorAction SilentlyContinue |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 1

if (-not $installer) {
  Fail "Installer was not generated in: $InstallerDir"
}

if (-not $SkipSign -and $signTool) {
  Invoke-Checked $signTool @('verify', '/pa', '/v', $installer.FullName)
  $installerSignature = Get-AuthenticodeSignature $installer.FullName
  if ($installerSignature.Status -notin @('Valid', 'UnknownError')) {
    Fail "Installer signature validation failed: $($installerSignature.Status)"
  }
}

$hash = Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256
$hashPath = "$($installer.FullName).sha256"
"$($hash.Hash)  $($installer.Name)" | Set-Content -LiteralPath $hashPath -Encoding ascii

Write-Host ""
Write-Host "Completed: $($installer.FullName)" -ForegroundColor Green
Write-Host "SHA256:    $hashPath" -ForegroundColor Green
