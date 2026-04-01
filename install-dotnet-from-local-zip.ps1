# Offline install: expand a dotnet-sdk-*-win-x64.zip into %USERPROFILE%\.dotnet (no admin).
# Get the zip from another PC or browser, e.g. .NET 8 SDK x64:
#   https://dotnet.microsoft.com/download/dotnet/8.0
#
# Usage:
#   .\install-dotnet-from-local-zip.ps1 -ZipPath D:\Downloads\dotnet-sdk-8.0.xxx-win-x64.zip
param(
  [Parameter(Mandatory = $true)]
  [string]$ZipPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ZipPath = [System.IO.Path]::GetFullPath($ZipPath)
if (-not (Test-Path -LiteralPath $ZipPath)) {
  throw ("Zip not found: " + $ZipPath)
}

$InstallDir = Join-Path $env:USERPROFILE '.dotnet'
Write-Host ("Install directory: " + $InstallDir) -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath $InstallDir)) {
  New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

Write-Host ("Extracting: " + $ZipPath) -ForegroundColor DarkGray
Expand-Archive -LiteralPath $ZipPath -DestinationPath $InstallDir -Force

$dotnetExe = Join-Path $InstallDir 'dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetExe)) {
  $dirs = @(Get-ChildItem -LiteralPath $InstallDir -Directory -ErrorAction SilentlyContinue)
  if ($dirs.Count -eq 1) {
    $nested = Join-Path $dirs[0].FullName 'dotnet.exe'
    if (Test-Path -LiteralPath $nested) {
      Write-Host "Moving files from single subfolder to install root..." -ForegroundColor Yellow
      Get-ChildItem -LiteralPath $dirs[0].FullName -Force | ForEach-Object {
        $dest = Join-Path $InstallDir $_.Name
        Move-Item -LiteralPath $_.FullName -Destination $dest -Force
      }
      Remove-Item -LiteralPath $dirs[0].FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
  }
}

$dotnetExe = Join-Path $InstallDir 'dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetExe)) {
  throw ("dotnet.exe not found under " + $InstallDir + ". The zip layout may be unexpected.")
}

& $dotnetExe --list-sdks
Write-Host ""

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if ([string]::IsNullOrEmpty($userPath)) {
  $newUserPath = $InstallDir
}
elseif ($userPath -notlike ('*' + $InstallDir + '*')) {
  $newUserPath = $InstallDir + ';' + $userPath
}
else {
  $newUserPath = $userPath
}

if ($newUserPath -ne $userPath) {
  [Environment]::SetEnvironmentVariable('Path', $newUserPath, 'User')
  Write-Host ("Added to User PATH: " + $InstallDir) -ForegroundColor Green
}
else {
  Write-Host ("User PATH already contains: " + $InstallDir) -ForegroundColor Green
}

$env:Path = $InstallDir + ';' + $env:Path
Write-Host ""
Write-Host "Done. Close and reopen the terminal, then run: .\publish.ps1" -ForegroundColor Yellow
