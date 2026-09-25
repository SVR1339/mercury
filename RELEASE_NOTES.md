# ☿ Mercury v1.0.0 — Commercial Production Release

## Universal GitHub & Terminal Installer for Windows & Apple

---

## Executive Overview

Mercury bridges the gap between open-source software and effortless local execution. By eliminating manual environment setup, runtime installations, and terminal permission barriers, Mercury allows developers, data scientists, engineers, and everyday users to install and run any project or command-line installation script in a single step.

---

## Key Capabilities

### 1. Universal Copied Command Engine

Paste **any installation one-liner copied from the internet** directly into Mercury:

- **PowerShell One-Liners** (`irm https://... | iex`): Executed natively with automatic `-ExecutionPolicy Bypass`, TLS 1.2/1.3 security negotiation, and full internet access.
- **Apple Terminal / Unix Shell One-Liners** (`curl -fsSL https://... | bash`): Executed seamlessly in native `/bin/zsh` or `/bin/bash` with live streaming output.
- **Git Clone Commands** (`git clone https://...`): Cloned directly into the managed `%LOCALAPPDATA%\Mercury\projects` workspace with automated stack detection.
- **Package Manager Git Installs** (`pip install git+...`, `cargo install --git`, `npm i`, `go install`): Handled directly via detected host package managers.
- **GitHub Repository Shorthand** (`owner/repo`): Automatically resolved to canonical GitHub endpoints.

### 2. Native Direct Terminal Access

One-click interactive terminal integration:

- **PowerShell**: Launches interactive terminal pre-configured with script execution permissions and TLS security protocols.
- **Command Prompt (CMD)**: Launches interactive console directly in the active project directory.
- **Apple Terminal**: Launches native macOS Apple Terminal (`open -a Terminal`) in the project directory.
- **Centralized Project Hub**: Instant file explorer access to `%LOCALAPPDATA%\Mercury\projects`.

### 3. Autonomous Stack & Dependency Detection

Mercury analyzes project manifests and provisions dependencies automatically across 10+ ecosystems:

- **Python**: `requirements.txt`, `setup.py`, `pyproject.toml`, `Pipfile`, `conda`
- **Node.js**: `package.json` + `npm`, `yarn`, `pnpm`, `bun`
- **C# / .NET**: `*.sln`, `*.csproj`, `*.fsproj`
- **Rust**: `Cargo.toml`
- **Go**: `go.mod`
- **Java**: `pom.xml`, `build.gradle`, `build.gradle.kts`
- **PHP**: `composer.json`
- **Ruby**: `Gemfile`
- **C / C++**: `CMakeLists.txt`, `Makefile`, `meson.build`
- **Docker**: `Dockerfile`, `docker-compose.yml`

### 4. Enterprise-Grade Security Engine

- Pre-flight script inspection classifies actions into **Safe**, **Needs Review**, or **Blocked**.
- Complete real-time audit logging capturing `stdout`, `stderr`, exit codes, and process durations.

### 5. Zero-Configuration Self-Contained Deployment

- No .NET runtime or SDK installation required — all runtimes and dependencies are 100% self-contained.

---

## Installation Options

### Windows GUI Setup Wizard (Recommended)

Download and execute [`Mercury-Setup.exe`](https://github.com/svreddy313-dev/mercury/releases/download/v1.0.0/Mercury-Setup.exe):

- Automatic desktop shortcut & Start Menu creation
- Optional PATH integration (`mercury` command available anywhere)
- Windows Explorer right-click folder context menu integration ("Install with Mercury")
- Clean uninstall support via Windows Settings

### Windows PowerShell (Direct Terminal Install)

```powershell
irm https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.ps1 | iex
```

### Apple / macOS (Apple Terminal / zsh)

```bash
curl -fsSL https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.sh | bash
```

---

## Cryptographic Checksums (SHA-256)

| Asset | Platform / Architecture | SHA-256 Checksum |
| :--- | :--- | :--- |
| `Mercury-Setup.exe` | Windows x64 (GUI Desktop Installer) | `E98CC41D082391F8D30EEA85153CA385BF7880E05A12FB3535862059D11A1D01` |
| `mercury.exe` | Windows x64 (Standalone CLI) | `9E894787FEAA38BB5BFE97C20E2F91C4E2D9505F2F2B2B316C7E562981D14E2A` |
| `mercury-win-x64.exe` | Windows x64 (Direct CLI Binary) | `9E894787FEAA38BB5BFE97C20E2F91C4E2D9505F2F2B2B316C7E562981D14E2A` |
| `mercury-osx-arm64` | macOS Apple Silicon (M1/M2/M3/M4) | `F339210B537FC88A10B005A0161F07E3519CA86A42286E574ACEF2A7D3CC14E5` |
| `mercury-osx-x64` | macOS Intel (x64) | `67A09A77BD4B2E691637AE33080BE5DA6C1FBD9AAA2C9B61B717656E598AF004` |

---

## Community & Support

- **GitHub Repository**: [https://github.com/svreddy313-dev/mercury](https://github.com/svreddy313-dev/mercury)
- **Discussions**: [https://github.com/svreddy313-dev/mercury/discussions](https://github.com/svreddy313-dev/mercury/discussions)
- **Issue Tracker**: [https://github.com/svreddy313-dev/mercury/issues](https://github.com/svreddy313-dev/mercury/issues)
- **License**: MIT License
