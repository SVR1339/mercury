<div align="center">

<img src="assets/logo.png" alt="Mercury Logo" width="160" height="160" style="border-radius: 24px; margin-bottom: 12px;" />

# Mercury

### Any GitHub File & Direct Repo PowerShell / Terminal Installer for Windows & Apple

**Paste Any Command or Repo → Auto-Detect → Grant Shell & Internet Access → Download & Install Dependencies → Run**

[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(PowerShell%20%2B%20CMD)-0078D6?style=for-the-badge&logo=windows)](../../releases/latest)
[![macOS](https://img.shields.io/badge/macOS-Apple%20Terminal%20(zsh%20%2B%20bash)-000?style=for-the-badge&logo=apple)](../../releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8.0%20Self--Contained-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

---

[**Download**](../../releases/latest) · [**Quick Start**](#-quick-start) · [**Smart Command Engine**](#-smart-command-processing) · [**Terminal Access**](#-direct-terminal-access) · [**CLI**](#-command-line)

</div>

---

## 🚀 Quick Start

### 1. Direct Install in Terminal

#### 🪟 Windows (PowerShell)
```powershell
irm https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.ps1 | iex
```

#### 🍎 Apple / macOS (Apple Terminal / zsh)
```bash
curl -fsSL https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.sh | bash
```

### 2. Windows GUI Installer
Download **`Mercury-Setup.exe`** from [Releases](../../releases/latest) and run it. Includes desktop icon, right-click context menu, and terminal integration.

---

## ⚡ Universal Smart Command Processing

Mercury accepts **ANY copied install command** or repository string from the internet and executes it directly with full internet access, necessary shell execution policies, and automated dependency setup:

| Copied Input Example | How Mercury Processes It | Shell & Access |
|---|---|---|
| `irm https://get.scoop.sh \| iex` | Executes directly in PowerShell with TLS 1.2/1.3 | **PowerShell** (ExecutionPolicy Bypass, Full Web Access) |
| `curl -fsSL https://bun.sh/install \| bash` | Executes in Apple Terminal / Unix shell | **Apple Terminal / zsh** (Full Web Access) |
| `git clone https://github.com/pallets/flask` | Clones into workspace, detects Python, installs pip/venv | **Mercury Engine** + Native Shell |
| `pip install git+https://github.com/...` | Executes pip package manager git install directly | **PowerShell / Apple Terminal** |
| `facebook/react` | Resolves shorthand to full repo, clones & installs npm | **Mercury Engine** + Auto-Dependencies |
| `npm install -g pnpm && pnpm setup` | Executes compound pipeline with real-time logs | **Shell Environment** |

---

## 💻 Direct Terminal Access

Mercury empowers users with instant access to their native terminals right from the application:

- **PowerShell**: Launches Windows PowerShell or PowerShell 7 with `-ExecutionPolicy Bypass`, pre-configured for running any GitHub installer script.
- **Command Prompt (CMD)**: Launches interactive Windows Command Prompt in the active project directory.
- **Apple Terminal**: Launches native macOS Apple Terminal (`open -a Terminal`) in the project workspace on macOS.
- **Project Folder**: Instant explorer / finder access to the centralized `Mercury/projects` directory.

---

## ✨ Core Features

### 🔍 Automated Dependency & Project Detection
Mercury automatically identifies project environments and handles dependency installation:
- **Python**: `requirements.txt`, `setup.py`, `pyproject.toml`, `Pipfile`, `conda`
- **Node.js**: `package.json`, `npm`, `yarn`, `pnpm`, `bun`
- **C# / .NET**: `*.sln`, `*.csproj`, `*.fsproj`
- **Rust**: `Cargo.toml`
- **Go**: `go.mod`
- **Java**: `pom.xml`, `build.gradle`, `build.gradle.kts`
- **PHP**: `composer.json`
- **Ruby**: `Gemfile`
- **C / C++**: `CMakeLists.txt`, `Makefile`, `meson.build`
- **Docker**: `Dockerfile`, `docker-compose.yml`

### 🛡️ Built-in Security Classification
- Validates copied web installers, notifying users when scripts request elevated privileges.
- Prevents accidental execution of destructive payloads.
- Live streaming output and stdout/stderr capture.

---

## 🖥️ Command Line (CLI)

Works out of the box on **Windows** and **Apple macOS**:

```bash
# Pass ANY copied command or repo directly to Mercury
mercury "irm https://raw.githubusercontent.com/... | iex"
mercury "git clone https://github.com/user/project.git"
mercury "facebook/react"

# Launch native terminal in project directory
mercury terminal pwsh       # Open PowerShell
mercury terminal cmd        # Open CMD
mercury terminal apple      # Open Apple Terminal

# Clone, scan, and run projects
mercury install https://github.com/user/project
mercury scan ./my-project
mercury info ./my-project
mercury run ./my-project
```

---

## 🏗️ Building from Source

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (Windows installer only)

### Quick Build

**Windows:**
```powershell
git clone https://github.com/svreddy313-dev/mercury.git
cd mercury
.\build.bat
```

**macOS / Linux:**
```bash
git clone https://github.com/svreddy313-dev/mercury.git
cd mercury
chmod +x install.sh && ./install.sh
```

### Manual Build
```bash
# CLI (cross-platform)
dotnet publish src/UniversalGitHubInstaller.Cli/UniversalGitHubInstaller.Cli.csproj \
  -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o publish/cli

# GUI (Windows only)
dotnet publish src/UniversalGitHubInstaller/UniversalGitHubInstaller.csproj \
  -c Release -r win-x64 --self-contained true -o publish/gui
```

---

## 📁 Project Structure

```
mercury/
├── install.sh              # macOS/Linux installer
├── install.ps1             # Windows PowerShell installer
├── build.bat               # Windows build script
├── installer.iss           # Windows GUI installer (Inno Setup)
├── src/
│   ├── UniversalGitHubInstaller.Core/   # Cross-platform detection engine
│   ├── UniversalGitHubInstaller/        # Windows WPF GUI
│   └── UniversalGitHubInstaller.Cli/    # Cross-platform CLI
└── docs/
    └── ARCHITECTURE.md
```

---

## 📋 System Requirements

| | Windows | macOS |
|---|---|---|
| **OS** | Windows 10+ (64-bit) | macOS 11+ (Intel & Apple Silicon) |
| **Install** | GUI wizard or PowerShell | Terminal script |
| **Runtime** | None (self-contained) | None (self-contained) |
| **Git** | Required | Required |

---

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

<div align="center">

**☿ Mercury** — *Install any GitHub project. No hassle. No headaches.*

Windows · macOS · CLI

</div>
