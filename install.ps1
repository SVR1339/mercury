# Mercury - Any GitHub file & Direct Repo PowerShell Installer for Windows and Apple
# Usage: irm https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.ps1 | iex

$ErrorActionPreference = 'Stop'

# Enforce TLS 1.2 / TLS 1.3 for secure downloads across all Windows environments
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13

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
    Write-Ok "Built CLI from source"

    # Build GUI if present
    $GuiProj = Join-Path $ScriptDir "src\UniversalGitHubInstaller\UniversalGitHubInstaller.csproj"
    $GuiDir = Join-Path $InstallDir "gui"
    if (Test-Path $GuiProj) {
        Write-Step "Building GUI application..."
        dotnet publish $GuiProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $GuiDir 2>$null
        $guiExe = Join-Path $GuiDir "UniversalGitHubInstaller.exe"
        if (Test-Path $guiExe) {
            Write-Ok "Built GUI application"
            try {
                $WshShell = New-Object -ComObject WScript.Shell
                $DesktopDir = [Environment]::GetFolderPath("Desktop")
                $Shortcut = $WshShell.CreateShortcut((Join-Path $DesktopDir "Mercury.lnk"))
                $Shortcut.TargetPath = $guiExe
                $Shortcut.WorkingDirectory = $GuiDir
                $Shortcut.Description = "Mercury - Universal GitHub & Terminal Installer"
                $iconPath = Join-Path $ScriptDir "app.ico"
                if (Test-Path $iconPath) { $Shortcut.IconLocation = $iconPath }
                $Shortcut.Save()

                $StartMenuDir = Join-Path ([Environment]::GetFolderPath("Programs")) "Mercury"
                New-Item -ItemType Directory -Path $StartMenuDir -Force | Out-Null
                $StartShortcut = $WshShell.CreateShortcut((Join-Path $StartMenuDir "Mercury.lnk"))
                $StartShortcut.TargetPath = $guiExe
                $StartShortcut.WorkingDirectory = $GuiDir
                $StartShortcut.Description = "Mercury - Universal GitHub & Terminal Installer"
                if (Test-Path $iconPath) { $StartShortcut.IconLocation = $iconPath }
                $StartShortcut.Save()
                Write-Ok "Created Desktop & Start Menu shortcuts"
            } catch { }
        }
    }
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
    $NewPath = if ([string]::IsNullOrWhiteSpace($UserPath)) { $BinDir } else { "$UserPath;$BinDir" }
    [Environment]::SetEnvironmentVariable("Path", $NewPath, "User")
    Write-Ok "Added Mercury to user PATH"
} else {
    Write-Ok "Already in PATH"
}

# Broadcast environment change to system shell (WM_SETTINGCHANGE)
try {
    Add-Type -Namespace Win32 -Name NativeMethods -MemberDefinition @"
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
"@
    $HWND_BROADCAST = [IntPtr]0xffff
    $WM_SETTINGCHANGE = 0x001A
    $result = [UIntPtr]::Zero
    [Win32.NativeMethods]::SendMessageTimeout($HWND_BROADCAST, $WM_SETTINGCHANGE, [UIntPtr]::Zero, "Environment", 2, 2000, [ref]$result) | Out-Null
} catch { }

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
Write-Host "    https://github.com/svreddy313-dev/mercury/releases/latest"
Write-Host ""
