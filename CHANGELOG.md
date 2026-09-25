# Changelog

All notable changes to Mercury are documented here.

## [1.0.0] - 2026-09-25

### 🚀 Mercury v1.0.0 — Universal GitHub & Terminal Installer

#### ✨ Key Features
- **Universal Copied Command Execution** — Copy and paste ANY installation command line from the internet (`irm ... | iex`, `curl ... | bash`, `git clone ...`, `pip install git+...`, `owner/repo`) and Mercury executes it directly with full internet access and appropriate permissions.
- **Direct Terminal Access** — One-click launcher and direct CLI commands for **PowerShell** (with `-ExecutionPolicy Bypass` and TLS 1.2/1.3), **Command Prompt (CMD)**, and macOS **Apple Terminal**.
- **Automated Stack & Dependency Detection** — Auto-identifies and configures environments for:
  - Python (`requirements.txt`, `pyproject.toml`, `setup.py`, `pipenv`, `conda`)
  - Node.js (`package.json`, `npm`, `yarn`, `pnpm`, `bun`)
  - .NET / C# (`*.sln`, `*.csproj`, `*.fsproj`)
  - Rust (`Cargo.toml`)
  - Go (`go.mod`)
  - Java (`pom.xml`, `build.gradle`, `build.gradle.kts`)
  - PHP (`composer.json`)
  - Ruby (`Gemfile`)
  - C / C++ (`CMakeLists.txt`, `Makefile`, `meson.build`)
  - Docker (`Dockerfile`, `docker-compose.yml`)
- **Centralized Project Management** — All cloned and installed projects are neatly managed in `%LOCALAPPDATA%\Mercury\projects` or user-selected folders.
- **Security Verification Engine** — Analyzes commands and scripts (Safe / Needs Review / Blocked) before running, protecting against malicious payloads.
- **Live Output Streaming** — Real-time stdout and stderr output capture with progress monitoring and error diagnosis.
- **Professional Cross-Platform Support** — Full-featured modern WPF desktop interface for Windows 10/11 and lightweight cross-platform CLI tool for Windows, macOS, and Linux.

#### 📦 Distribution & Packaging
- Self-contained Windows installer wizard (`Mercury-Setup.exe`, ~94 MB) — no .NET runtime required.
- Standalone single-file CLI executable (`mercury.exe`, ~67 MB).
- Desktop shortcut, Start Menu entry, optional PATH integration, and Windows Explorer right-click context menu.
- One-line terminal installation for Windows (`install.ps1`) and macOS (`install.sh`).
