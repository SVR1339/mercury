# Mercury

## Universal GitHub & Terminal Installer for Windows & Apple

Paste Any Command or Repo → Auto-Detect → Grant Shell & Internet Access → Download & Install Dependencies → Run

![Windows](https://img.shields.io/badge/Windows-10%2F11%20(PowerShell%20%2B%20CMD)-0078D6?style=for-the-badge&logo=windows)
![macOS](https://img.shields.io/badge/macOS-Apple%20Terminal%20(zsh%20%2B%20bash)-000?style=for-the-badge&logo=apple)
![Latest Release](https://img.shields.io/badge/Release-v1.0.0-success?style=for-the-badge)
![.NET 8](https://img.shields.io/badge/.NET-8.0%20Self--Contained-512BD4?style=for-the-badge&logo=dotnet)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)

---

[**Download Installer**](https://github.com/svreddy313-dev/mercury/releases/latest) · [**Quick Start**](#quick-start) · [**Why Mercury?**](#why-developers-love-mercury) · [**Smart Engine**](#universal-smart-command-processing) · [**Terminal Access**](#direct-terminal-access) · [**CLI Guide**](#command-line-interface-cli) · [**Architecture**](docs/ARCHITECTURE.md)

---

## Why Developers Love Mercury

Setting up open-source GitHub repositories from scratch is notorious for unexpected environment friction: missing compilers, broken Python virtual environments, PowerShell execution policy restrictions, uninstalled package managers, or cryptic dependency errors.

**Mercury eliminates the entire manual onboarding hurdle.**

```text
┌─────────────────────────┐     ┌─────────────────────────┐     ┌─────────────────────────┐     ┌─────────────────────────┐
│  Paste Any Input        │ ──► │  Polyglot Detection     │ ──► │  Shell & Web Access     │ ──► │  Installed & Running    │
│  • PowerShell one-liner │     │  • Detects Python/Node/ │     │  • Grants execution     │     │  • Virtual environments │
│  • Apple curl | bash    │     │    Rust/Go/.NET/C++/etc.│       policies (pwsh/bash)    │     │  • Built & configured   │
│  • git clone / URL      │     │  • Resolves manifests   │     │  • Enables TLS & Web    │     │  • Launched with 1 click│
└─────────────────────────┘     └─────────────────────────┘     └─────────────────────────┘     └─────────────────────────┘
```

| Friction Point | Traditional Manual Setup | With Mercury |
| :--- | :--- | :--- |
| **PowerShell Execution Policy** | `File ... cannot be loaded because running scripts is disabled` | Auto-configured with `-ExecutionPolicy Bypass` |
| **Cloning & Placement** | Manual `mkdir`, `cd`, `git clone`, finding where it went | Automatic organized workspace in `%LOCALAPPDATA%\Mercury\projects` |
| **Dependency Manifests** | Figuring out whether to run `pip`, `poetry`, `npm`, `cargo`, `dotnet` | Automatic stack detection & zero-click dependency installation |
| **Internet Access** | Blocked downloads, outdated TLS protocols | Managed TLS 1.2/1.3 and full streaming download pipelines |
| **Apple Terminal / macOS** | Different shell commands (`zsh`, `brew`, `bash`) | Cross-platform syntax translation and one-click Apple Terminal access |
| **Terminal Context** | Opening a terminal and manually `cd`-ing into nested directories | Instant one-click terminal launcher inside the exact project root |

---

## Quick Start

### 1. Direct Install in Terminal (One-Liner)

#### Windows (PowerShell)

```powershell
irm https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.ps1 | iex
```

#### Apple / macOS (Apple Terminal / zsh / bash)

```bash
curl -fsSL https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.sh | bash
```

---

### 2. Windows GUI Desktop Installer

Download the complete Windows desktop application with full graphical user interface:

- 📦 **[Download Mercury-Setup.exe (v1.0.0)](https://github.com/svreddy313-dev/mercury/releases/download/v1.0.0/Mercury-Setup.exe)**
  - Size: 94 MB
  - **100% Self-Contained** — No .NET runtime or SDK installation required
  - Includes Desktop Shortcut, Start Menu entry, optional PATH integration, and File Explorer folder right-click context menu

---

## Universal Smart Command Processing

Mercury was designed to accept **whatever the developer copies from the internet**:

| Copied Input Example | How Mercury Processes It | Target Shell & Permissions |
| :--- | :--- | :--- |
| `irm https://get.scoop.sh \| iex` | Executes installer script with TLS 1.2/1.3 | **PowerShell** (`-ExecutionPolicy Bypass`, Full Web Access) |
| `curl -fsSL https://bun.sh/install \| bash` | Executes web installer stream | **Apple Terminal / zsh** (Full Web Access) |
| `git clone https://github.com/pallets/flask` | Clones into `Mercury/projects`, detects stack, installs pip/venv | **Mercury Engine** + Native Shell |
| `pip install git+https://github.com/...` | Installs Python package from git directly | **Host Terminal** (PowerShell / Apple Terminal) |
| `facebook/react` | Resolves shorthand to full repo, clones & installs npm | **Mercury Engine** + Auto-Dependencies |
| `npm install -g pnpm && pnpm setup` | Executes compound command pipeline with live output | **Interactive Shell** |

---

## Direct Terminal Access

Mercury gives you instant, one-click access to your native terminals right inside the application:

- **PowerShell**: Launches Windows PowerShell or PowerShell 7 with `-ExecutionPolicy Bypass`, pre-configured for running any GitHub installer script.
- **Command Prompt (CMD)**: Launches interactive Windows Command Prompt directly in your active project directory.
- **Apple Terminal**: Launches native macOS Apple Terminal (`open -a Terminal`) in the project workspace on macOS.
- **Project Hub**: One-click file explorer access to `%LOCALAPPDATA%\Mercury\projects`.

---

## Supported Stacks & Ecosystems

Mercury auto-detects and installs dependencies for all major software ecosystems:

- 🐍 **Python**: `requirements.txt`, `setup.py`, `pyproject.toml`, `Pipfile`, `conda`
- 🟢 **Node.js**: `package.json`, `npm`, `yarn`, `pnpm`, `bun`
- 🟣 **.NET / C#**: `*.sln`, `*.csproj`, `*.fsproj`
- 🦀 **Rust**: `Cargo.toml`, `cargo`
- 🔵 **Go**: `go.mod`, `go build`
- ☕ **Java**: `pom.xml` (Maven), `build.gradle` (Gradle)
- 🐘 **PHP**: `composer.json` (Composer)
- 💎 **Ruby**: `Gemfile` (Bundler)
- ⚙️ **C / C++**: `CMakeLists.txt`, `Makefile`, `meson.build`
- 🐳 **Docker**: `Dockerfile`, `docker-compose.yml`

---

## Security Verification Engine

Mercury protects developers from malicious commands and accidental data loss:

- **Heuristic Command Safety**: All commands are classified as **Safe**, **Needs Review**, or **Blocked** prior to execution.
- **Live Output Streaming**: Transparent, unbuffered live streaming of both standard output (`stdout`) and error streams (`stderr`).
- **Dangerous Command Guard**: Destructive system operations (e.g. disk wipes, system directory deletions) are intercepted and prevented.

---

## Command Line Interface (CLI)

Prefer working from the terminal? Mercury provides a full-featured, zero-dependency CLI:

```bash
# Pass ANY copied command or repo directly to Mercury
mercury "irm https://raw.githubusercontent.com/... | iex"
mercury "git clone https://github.com/pallets/flask.git"
mercury "facebook/react"

# Launch native terminal in project directory
mercury terminal pwsh       # Open PowerShell with execution bypass
mercury terminal cmd        # Open Command Prompt
mercury terminal apple      # Open Apple Terminal (macOS)

# Clone, scan, and run projects
mercury install https://github.com/pallets/flask
mercury scan ./my-project
mercury info ./my-project
mercury run ./my-project
mercury doctor              # Diagnose installed runtimes & tools
```

---

## Building from Source

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

## System Requirements

| Environment | Windows | macOS |
| :--- | :--- | :--- |
| **Operating System** | Windows 10 / 11 (64-bit) | macOS 11+ Big Sur / Monterey / Ventura / Sonoma / Sequoia |
| **Architecture** | x64, ARM64 | Intel x64 & Apple Silicon (M1/M2/M3/M4) |
| **Terminal** | Windows PowerShell / CMD / Windows Terminal | Apple Terminal (zsh / bash) |
| **Runtime** | None (100% self-contained binary) | None (100% self-contained binary) |
| **Git** | Recommended for cloning | Recommended for cloning |

---

## Community & Contributing

Contributions are warmly welcomed! If you would like to contribute:

1. Fork the repository on [GitHub](https://github.com/svreddy313-dev/mercury).
2. Create your feature branch (`git checkout -b feature/amazing-feature`).
3. Commit your changes (`git commit -m 'Add amazing feature'`).
4. Push to the branch (`git push origin feature/amazing-feature`).
5. Open a Pull Request.

If you encounter any issues or have feature suggestions, please [open an issue](https://github.com/svreddy313-dev/mercury/issues).

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

**Mercury** — *Install any GitHub project or terminal script. In one click.*

⭐ Star us on GitHub: [github.com/svreddy313-dev/mercury](https://github.com/svreddy313-dev/mercury)
