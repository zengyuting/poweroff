# Publish single-file exe. No admin: run .\install-dotnet-sdk-user.ps1 first.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DotNetExePath {
  $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
  if ($cmd -and $cmd.Source) {
    return $cmd.Source
  }
  # Per-user install first, then machine-wide
  $candidates = @(
    (Join-Path $env:USERPROFILE '.dotnet\dotnet.exe')
    (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe')
    (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe')
    (Join-Path ${env:ProgramFiles(x86)} 'dotnet\dotnet.exe')
  )
  if ($env:DOTNET_ROOT) {
    $candidates = @((Join-Path $env:DOTNET_ROOT 'dotnet.exe')) + $candidates
  }
  foreach ($p in $candidates) {
    if ($p -and (Test-Path -LiteralPath $p)) {
      return $p
    }
  }
  return $null
}

$dotnet = Get-DotNetExePath
if (-not $dotnet) {
  Write-Host ""
  Write-Host "dotnet CLI not found." -ForegroundColor Red
  Write-Host ""
  Write-Host "No admin required - install SDK to your user folder:" -ForegroundColor Yellow
  Write-Host "  .\install-dotnet-sdk-user.ps1" -ForegroundColor White
  Write-Host "If online download fails, copy a dotnet-sdk-*-win-x64.zip here and run:" -ForegroundColor Yellow
  Write-Host "  .\install-dotnet-from-local-zip.ps1 -ZipPath <path-to-zip>" -ForegroundColor White
  Write-Host "Then close and reopen the terminal, and run: .\publish.ps1" -ForegroundColor Yellow
  Write-Host ""
  Write-Host "Or install SDK with an administrator account:" -ForegroundColor Yellow
  Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
  Write-Host ""
  exit 1
}

Push-Location $PSScriptRoot
try {
  $proj = Join-Path $PSScriptRoot 'AutoPowerOff\AutoPowerOff.csproj'
  if (-not (Test-Path -LiteralPath $proj)) {
    Write-Host ""
    Write-Host ("Project file not found: " + $proj) -ForegroundColor Red
    Write-Host "Make sure publish.ps1 is in the repo root and AutoPowerOff\AutoPowerOff.csproj exists." -ForegroundColor Yellow
    Write-Host ("Current script dir: " + $PSScriptRoot) -ForegroundColor DarkGray
    exit 1
  }
  Write-Host ("Using: " + $dotnet)
  & $dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o .\publish
  Write-Host ("Output: " + $PSScriptRoot + "\publish\AutoPowerOff.exe")
}
finally {
  Pop-Location
}
