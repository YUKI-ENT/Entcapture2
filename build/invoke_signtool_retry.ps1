param(
  [Parameter(Mandatory = $true)]
  [string]$SignTool,

  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$SignArguments
)

$ErrorActionPreference = 'Stop'

function Get-TargetPath {
  param([string[]]$Arguments)

  for ($index = $Arguments.Count - 1; $index -ge 0; $index--) {
    $argument = $Arguments[$index].Trim('"')
    if ($argument -and (Test-Path -LiteralPath $argument -PathType Leaf)) {
      return [System.IO.Path]::GetFullPath($argument)
    }
  }

  return $null
}

function Wait-FileWritable {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [int]$TimeoutSeconds = 30
  )

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $attempt = 0
  do {
    try {
      $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None)
      $stream.Dispose()
      return
    }
    catch [System.IO.IOException] {
      $attempt++
      Start-Sleep -Milliseconds ([Math]::Min(2500, 250 * $attempt))
    }
  } while ((Get-Date) -lt $deadline)

  throw "Timed out waiting for file to become writable: $Path"
}

$targetPath = Get-TargetPath -Arguments $SignArguments
if ($targetPath) {
  Wait-FileWritable -Path $targetPath
}

& $SignTool @SignArguments
exit $LASTEXITCODE
