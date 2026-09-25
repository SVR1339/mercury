# Mercury - Any GitHub file & Direct Repo PowerShell Installer for Windows and Apple
# Usage: irm https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.ps1 | iex

$ErrorActionPreference = 'Stop'

$AppName = "Mercury"
$InstallDir = Join-Path $env:LOCALAPPDATA "Mercury"
$BinDir = Join-Path $InstallDir "bin"

function Write-Header {
    Write-Host ""
    Write-Host "  ☿ Mercury Installer" -ForegroundColor Cyan
    Write-Host "  Any GitHub file & Direct Repo PowerShell Installer for Windows and Apple" -ForegroundColor Blue
    Write-Host ""
}

function Write-Step  { param($msg) Write-Host "  → $msg" -ForegroundColor Blue }
function Write-Ok    { param($msg) Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Fail  { param($msg) Write-Host "  ✗ $msg" -ForegroundColor Red; exit 1 }

Write-Header

# ─── Check prerequisites ───
Write-Step "Checking prerequisites..."

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Fail "Git is not installed. Download: https://git-scm.com"
}
Write-Ok "Git found: $(git --version)"

# ─── Install ───
Write-Step "Installing Mercury..."

New-Item -ItemType Directory -Path $BinDir -Force | Out-Null

# Check if running from repo
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { $PWD.Path }
$CliProj = Join-Path $ScriptDir "src\UniversalGitHubInstaller.Cli\UniversalGitHubInstaller.Cli.csproj"

if (Test-Path $CliProj) {
    Write-Step "Building from source..."
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Fail ".NET 8 SDK required to build. Download: https://dotnet.microsoft.com/download"
    }
    dotnet publish $CliProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $BinDir 2>$null
    $cliExe = Join-Path $BinDir "UniversalGitHubInstaller.Cli.exe"
    $mercuryExe = Join-Path $BinDir "mercury.exe"
    $ugiExe = Join-Path $BinDir "ugi.exe"
    if (Test-Path $cliExe) {
        Copy-Item $cliExe $mercuryExe -Force
        Copy-Item $cliExe $ugiExe -Force
    }
    Write-Ok "Built from source"
} else {
    # Download pre-built
    $Repo = "svreddy313-dev/mercury"
    $DownloadUrls = @(
        "https://github.com/$Repo/releases/latest/download/mercury.exe",
        "https://github.com/$Repo/releases/latest/download/mercury-win-x64.exe"
    )
    
    $downloadSuccess = $false
    $mercuryExe = Join-Path $BinDir "mercury.exe"
    foreach ($url in $DownloadUrls) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $mercuryExe -UseBasicParsing
            Write-Ok "Downloaded Mercury from $url"
            $downloadSuccess = $true
            break
        } catch {
            continue
        }
    }
    if (-not $downloadSuccess) {
        Write-Fail "Download failed. Build from source: git clone https://github.com/$Repo && cd mercury && .\install.ps1"
    }
}

# ─── Add to PATH ───
$UserPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($UserPath -notlike "*$BinDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$UserPath;$BinDir", "User")
    Write-Ok "Added to PATH"
} else {
    Write-Ok "Already in PATH"
}

# Update current session
$env:PATH = "$env:PATH;$BinDir"

# ─── Done ───
Write-Host ""
Write-Host "  ✓ Mercury installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "  Quick start:" -ForegroundColor White
Write-Host "    mercury install https://github.com/user/project"
Write-Host "    mercury scan .\my-project"
Write-Host "    mercury info .\my-project"
Write-Host ""
Write-Host "  Or download the GUI installer from:" -ForegroundColor Yellow
Write-Host "    https://github.com/$Repo/releases/latest"
Write-Host ""
