<div align="center">

<img src="assets/logo.png" alt="Mercury Logo" width="160" height="160" style="border-radius: 28px; margin-bottom: 14px; box-shadow: 0 8px 24px rgba(0,0,0,0.3);" />

# Mercury

### Universal GitHub & Terminal Installer for Windows & Apple

**Paste Any Command or Repo → Auto-Detect → Grant Shell & Internet Access → Download & Install Dependencies → Run**

[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(PowerShell%20%2B%20CMD)-0078D6?style=for-the-badge&logo=windows)](https://github.com/svreddy313-dev/mercury/releases/latest)
[![macOS](https://img.shields.io/badge/macOS-Apple%20Terminal%20(zsh%20%2B%20bash)-000?style=for-the-badge&logo=apple)](https://github.com/svreddy313-dev/mercury/releases/latest)
[![Latest Release](https://img.shields.io/badge/Release-v1.0.0-success?style=for-the-badge)](https://github.com/svreddy313-dev/mercury/releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8.0%20Self--Contained-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

---

[**Download Installer**](https://github.com/svreddy313-dev/mercury/releases/latest) · [**Quick Start**](#-quick-start) · [**Smart Command Engine**](#-universal-smart-command-processing) · [**Terminal Access**](#-direct-terminal-access) · [**CLI Guide**](#-command-line-cli)

</div>

---

## 🚀 Quick Start

### 1. Direct Install in Terminal (One-Liner)

#### 🪟 Windows (PowerShell)
```powershell
irm https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.ps1 | iex
```

#### 🍎 Apple / macOS (Apple Terminal / zsh)
```bash
curl -fsSL https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.sh | bash
```

---

### 2. Windows GUI Installer

Download and run the installer:
- 📦 **[Download Mercury-Setup.exe (v1.0.0)](https://github.com/svreddy313-dev/mercury/releases/download/v1.0.0/Mercury-Setup.exe)** (94 MB, fully self-contained — no .NET runtime required)
- Includes desktop shortcut, Start Menu entry, optional PATH integration, and right-click folder context menu.

---

## ⚡ Universal Smart Command Processing

Mercury solves one of the biggest friction points in developer tools: **running commands copied from the internet**. 

Simply copy and paste **ANY command or repository URL** into Mercury — it parses the syntax, grants necessary shell permissions (`-ExecutionPolicy Bypass`, TLS 1.2/1.3, zsh), connects to the internet, and installs everything automatically:

| Copied Input Example | How Mercury Processes It | Target Shell & Permissions |
|---|---|---|
| `irm https://get.scoop.sh \| iex` | Executes installer script with TLS 1.2/1.3 | **PowerShell** (`-ExecutionPolicy Bypass`, Full Web Access) |
| `curl -fsSL https://bun.sh/install \| bash` | Executes web installer stream | **Apple Terminal / zsh** (Full Web Access) |
| `git clone https://github.com/pallets/flask` | Clones into `Mercury/projects`, detects stack, installs pip/venv | **Mercury Engine** + Native Shell |
| `pip install git+https://github.com/...` | Installs Python package from git directly | **Host Terminal** (PowerShell / Apple Terminal) |
| `facebook/react` | Resolves shorthand to full repo, clones & installs npm | **Mercury Engine** + Auto-Dependencies |
| `npm install -g pnpm && pnpm setup` | Executes compound command pipeline with live output | **Interactive Shell** |

---

## 💻 Direct Terminal Access

Mercury gives you instant, one-click access to your native terminals right inside the application:

- **PowerShell**: Launches Windows PowerShell or PowerShell 7 with `-ExecutionPolicy Bypass`, pre-configured for running any GitHub installer script.
- **Command Prompt (CMD)**: Launches interactive Windows Command Prompt directly in your active project directory.
- **Apple Terminal**: Launches native macOS Apple Terminal (`open -a Terminal`) in the project workspace on macOS.
- **Project Hub**: One-click file explorer access to `%LOCALAPPDATA%\Mercury\projects`.

---

## ✨ Core Features

### 🔍 Automated Stack & Dependency Detection
Mercury inspects project manifests and automatically installs all required dependencies:
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

### 🛡️ Security Verification Engine
- Analyzes commands before execution and classifies them as **Safe**, **Needs Review**, or **Blocked**.
- Transparent execution: full streaming output capture for both standard output (`stdout`) and errors (`stderr`).
- Protects users from accidental destructive system modifications.

---

## 🖥️ Command Line (CLI)

Works out of the box on **Windows** and **Apple macOS**:

```bash
# Pass ANY copied command or repo directly to Mercury
mercury "irm https://raw.githubusercontent.com/... | iex"
mercury "git clone https://github.com/pallets/flask.git"
mercury "facebook/react"

# Launch native terminal in project directory
mercury terminal pwsh       # Open PowerShell
mercury terminal cmd        # Open CMD
mercury terminal apple      # Open Apple Terminal

# Clone, scan, and run projects
mercury install https://github.com/pallets/flask
mercury scan ./my-project
mercury info ./my-project
mercury run ./my-project
mercury doctor              # Diagnose installed runtimes & tools
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

---

## 📋 System Requirements

| | Windows | macOS |
|---|---|---|
| **Operating System** | Windows 10 / 11 (64-bit) | macOS 11+ (Intel & Apple Silicon) |
| **Terminal** | Windows PowerShell / CMD | Apple Terminal (zsh / bash) |
| **Runtime** | None (100% self-contained) | None (100% self-contained) |
| **Git** | Recommended for cloning | Recommended for cloning |

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

<div align="center">

**Mercury** — *Install any GitHub project or terminal script. In one click.*

[Download Latest Release](https://github.com/svreddy313-dev/mercury/releases/latest)

</div>
