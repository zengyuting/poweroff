# Installs .NET 8 SDK to %USERPROFILE%\.dotnet (no administrator required).
# Official script: https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script
#
# If execution is blocked, run once (CurrentUser, usually no admin):
#   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
# Or: powershell -ExecutionPolicy Bypass -File .\install-dotnet-sdk-user.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$InstallDir = Join-Path $env:USERPROFILE '.dotnet'
$TempScript = Join-Path $env:TEMP ("dotnet-install-{0}.ps1" -f [Guid]::NewGuid().ToString('N'))

# Same order as Microsoft docs; some corporate gateways block dot.net but allow builds.dotnet.microsoft.com.
$ScriptUrls = @(
  'https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1',
  'https://dot.net/v1/dotnet-install.ps1',
  'https://raw.githubusercontent.com/dotnet/install-scripts/main/src/dotnet-install.ps1'
)

$BrowserUa = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36'

function Get-ProxyFromEnvironment {
  foreach ($name in @('HTTPS_PROXY', 'https_proxy', 'HTTP_PROXY', 'http_proxy', 'ALL_PROXY', 'all_proxy')) {
    $v = [Environment]::GetEnvironmentVariable($name, 'Process')
    if ([string]::IsNullOrWhiteSpace($v)) { $v = [Environment]::GetEnvironmentVariable($name, 'User') }
    if ([string]::IsNullOrWhiteSpace($v)) { $v = [Environment]::GetEnvironmentVariable($name, 'Machine') }
    if (-not [string]::IsNullOrWhiteSpace($v)) { return $v.Trim().Trim('"') }
  }
  return $null
}

function Save-InstallScriptFromWeb {
  [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

  $headers = @{ 'User-Agent' = $BrowserUa }

  foreach ($uri in $ScriptUrls) {
    try {
      Write-Host ("Trying download: " + $uri) -ForegroundColor DarkGray
      Invoke-WebRequest -Uri $uri -OutFile $TempScript -UseBasicParsing -Headers $headers
      if ((Test-Path -LiteralPath $TempScript) -and ((Get-Item -LiteralPath $TempScript).Length -gt 1000)) {
        Write-Host ("OK: " + $uri) -ForegroundColor Green
        return $true
      }
    }
    catch {
      Write-Host ("Invoke-WebRequest failed: " + $uri + " -- " + $_.Exception.Message) -ForegroundColor DarkYellow
    }
    Remove-Item -LiteralPath $TempScript -ErrorAction SilentlyContinue

    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($curl -and $curl.Source) {
      try {
        Write-Host ("Trying curl.exe: " + $uri) -ForegroundColor DarkGray
        & curl.exe -fsSL -L -A $BrowserUa -o $TempScript $uri
        if ((Test-Path -LiteralPath $TempScript) -and ((Get-Item -LiteralPath $TempScript).Length -gt 1000)) {
          Write-Host ("OK (curl): " + $uri) -ForegroundColor Green
          return $true
        }
      }
      catch {
        Write-Host ("curl failed: " + $uri + " -- " + $_.Exception.Message) -ForegroundColor DarkYellow
      }
      Remove-Item -LiteralPath $TempScript -ErrorAction SilentlyContinue
    }
  }

  return $false
}

$localScript = Join-Path $PSScriptRoot 'tools\dotnet-install.ps1'
if ((Test-Path -LiteralPath $localScript) -and ((Get-Item -LiteralPath $localScript).Length -gt 1000)) {
  Write-Host ("Using local script: " + $localScript) -ForegroundColor Cyan
  Copy-Item -LiteralPath $localScript -Destination $TempScript -Force
}
elseif (-not (Save-InstallScriptFromWeb)) {
  Write-Host ""
  Write-Host "Could not download dotnet-install.ps1 (HTTP 400/403 often means a company proxy or SSL inspection)." -ForegroundColor Red
  Write-Host ""
  Write-Host "Try one of these:" -ForegroundColor Yellow
  Write-Host "  1) On a PC with normal internet, save the file from:" -ForegroundColor White
  Write-Host "     https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1" -ForegroundColor White
  Write-Host "     Put it at:  <this repo>\tools\dotnet-install.ps1" -ForegroundColor White
  Write-Host "     Then run this script again (it will use the local copy)." -ForegroundColor White
  Write-Host "  2) Ask IT for proxy settings, then set user env HTTPS_PROXY / HTTP_PROXY, or configure Windows proxy." -ForegroundColor White
  Write-Host "  3) Install .NET SDK from an offline installer or internal software center (if your company provides one)." -ForegroundColor White
  Write-Host ""
  exit 1
}

Write-Host ("Install directory (per-user, no admin): " + $InstallDir) -ForegroundColor Cyan

$proxyAddr = Get-ProxyFromEnvironment
$installParams = @{
  Channel      = '8.0'
  Quality      = 'ga'
  InstallDir   = $InstallDir
  Architecture = 'x64'
  NoPath       = $true
}
if ($proxyAddr) {
  $installParams['ProxyAddress'] = $proxyAddr
  Write-Host ("Using proxy from environment: " + $proxyAddr) -ForegroundColor DarkGray
  if ($env:DOTNET_INSTALL_PROXY_USE_DEFAULT_CREDS -eq '1') {
    $installParams['ProxyUseDefaultCredentials'] = $true
    Write-Host "ProxyUseDefaultCredentials enabled (DOTNET_INSTALL_PROXY_USE_DEFAULT_CREDS=1)" -ForegroundColor DarkGray
  }
}

try {
  try {
    & $TempScript @installParams
  }
  catch {
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "SDK zip download failed (common: company firewall blocks aka.ms / Microsoft CDN)." -ForegroundColor Yellow
    Write-Host "Option A - Proxy: set user env HTTPS_PROXY to your IT proxy URL, then run this script again." -ForegroundColor White
    Write-Host "  Example: http://proxy.company.com:8080" -ForegroundColor DarkGray
    Write-Host "  For NTLM try also: set DOTNET_INSTALL_PROXY_USE_DEFAULT_CREDS=1" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Option B - Offline zip (no network on this PC):" -ForegroundColor White
    Write-Host "  1) On another PC or in browser, download SDK x64 zip for .NET 8 from:" -ForegroundColor White
    Write-Host "     https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "  2) Copy dotnet-sdk-*-win-x64.zip here, then run:" -ForegroundColor White
    Write-Host "     .\install-dotnet-from-local-zip.ps1 -ZipPath <full-path-to-zip>" -ForegroundColor White
    Write-Host ""
    exit 1
  }
}
finally {
  Remove-Item -LiteralPath $TempScript -ErrorAction SilentlyContinue
}

$dotnetExe = Join-Path $InstallDir 'dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetExe)) {
  Write-Host ("After install, dotnet.exe not found: " + $dotnetExe) -ForegroundColor Red
  Write-Host "Try: .\install-dotnet-from-local-zip.ps1 -ZipPath <path-to-sdk-zip>" -ForegroundColor Yellow
  exit 1
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
  Write-Host ("Added to User PATH (no admin): " + $InstallDir) -ForegroundColor Green
}
else {
  Write-Host ("User PATH already contains: " + $InstallDir) -ForegroundColor Green
}

$env:Path = $InstallDir + ';' + $env:Path
Write-Host ""
Write-Host "PATH updated for this session only. Close and reopen the terminal, then run: .\publish.ps1" -ForegroundColor Yellow
Write-Host "If dotnet is still not found, sign out of Windows once or reboot." -ForegroundColor Yellow
