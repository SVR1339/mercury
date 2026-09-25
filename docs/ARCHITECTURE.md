# Mercury — Technical Architecture & Engine Design

## Overview

**Mercury** is a cross-platform (Windows & Apple macOS) universal application and project runner. It automates the detection, dependency acquisition, permission configuration, and execution of any GitHub repository or terminal install command.

Users can supply:

- Repository URLs (`https://github.com/owner/repo`)
- GitHub shorthands (`owner/repo`)
- Direct `git clone` commands (`git clone https://github.com/...`)
- PowerShell one-liners (`irm ... | iex`)
- Apple Terminal / Unix bash one-liners (`curl -fsSL ... | bash`)
- Package manager installs (`pip install git+...`, `npm`, `cargo`)
- Local project folders or ZIP archives

Mercury orchestrates environment detection, security verification, dependency installation, and process execution with full interactive terminal access.

---

## Solution Structure

```text
UniversalGitHubInstaller.sln
├── src/
│   ├── UniversalGitHubInstaller.Core/       # Shared core engine (.NET 8)
│   │   ├── Detection/                       # Polyglot technology detectors
│   │   │   ├── IProjectDetector.cs          # Detector interface contract
│   │   │   ├── ProjectScanner.cs            # Orchestrates all language detectors
│   │   │   ├── PythonDetector.cs            # pip, venv, poetry, conda, pyproject.toml
│   │   │   ├── NodeDetector.cs              # npm, yarn, pnpm, bun, package.json
│   │   │   ├── DotNetDetector.cs            # .sln, .csproj, dotnet CLI
│   │   │   ├── RustDetector.cs              # Cargo.toml, rustc
│   │   │   ├── GoDetector.cs                # go.mod, go build
│   │   │   ├── JavaDetector.cs              # Maven (pom.xml), Gradle
│   │   │   ├── PhpDetector.cs               # Composer (composer.json)
│   │   │   ├── RubyDetector.cs              # Bundler (Gemfile)
│   │   │   ├── CppDetector.cs               # CMake, Make, Meson
│   │   │   ├── DockerDetector.cs            # Dockerfile, docker-compose.yml
│   │   │   └── EnvironmentDetector.cs       # System runtime detection
│   │   ├── Models/
│   │   │   ├── ProjectInfo.cs               # Scanned project metadata & tech stack
│   │   │   ├── ProjectProfile.cs            # Saved project workspace configuration
│   │   │   ├── OperationResult.cs           # Command execution exit codes & streams
│   │   │   ├── CommandClassification.cs     # Security threat level (Safe/Review/Blocked)
│   │   │   └── WindowsEnvironmentInfo.cs    # Host environment diagnostics
│   │   ├── Runtime/
│   │   │   └── WindowsEnvironmentScanner.cs # Diagnostic scanner for PATH & runtimes
│   │   ├── Security/
│   │   │   ├── CommandClassifier.cs         # Heuristic & pattern-based command safety
│   │   │   └── SecurityScanner.cs           # Static repo scan for suspicious patterns
│   │   ├── Services/
│   │   │   ├── SmartCommandProcessor.cs     # Universal command & URL syntax engine
│   │   │   ├── TerminalService.cs           # Native terminal launcher (pwsh, cmd, apple)
│   │   │   ├── ProcessRunner.cs             # Async process execution with streaming
│   │   │   ├── GitService.cs                # Git clone/pull/status & repo resolution
│   │   │   ├── ZipService.cs                # Safe ZIP archive extraction
│   │   │   └── ProjectProfileService.cs     # Workspace persistence & caching
│   │   └── Logging/
│   │       └── AppLogger.cs                 # Structured file logger
│   │
│   ├── UniversalGitHubInstaller/            # WPF GUI application (.NET 8 Windows)
│   │   ├── App.xaml / App.xaml.cs           # Application entry & global exception handling
│   │   ├── Views/
│   │   │   └── MainWindow.xaml / .cs        # Modern reactive UI (Status, Logs, Terminals)
│   │   ├── ViewModels/
│   │   │   ├── BaseViewModel.cs             # INotifyPropertyChanged base implementation
│   │   │   ├── MainViewModel.cs             # UI state machine & orchestration logic
│   │   │   └── RelayCommand.cs              # Async/sync command bindings
│   │   └── Converters/
│   │       └── BoolToVisibilityConverter.cs # WPF visibility conversion
│   │
│   └── UniversalGitHubInstaller.Cli/        # CLI tool (.NET 8, single-file cross-platform)
│       └── Program.cs                       # mercury / ugi console entry point
│
├── tests/
│   └── UniversalGitHubInstaller.Tests/      # xUnit automated test suite (39 tests)
│
├── assets/                                  # Logos, clean geometry, application icons
│   ├── logo.png                             # Hi-res professional logo (liquid mercury droplet)
│   ├── icon.png                             # Square application icon
│   └── app.ico                              # Standard multi-resolution DIB uncompressed icon
├── installer.iss                            # Inno Setup 6 production installer script
├── install.ps1                              # One-line PowerShell web installer
├── install.sh                               # One-line Apple Terminal / bash installer
└── dist/
    └── Mercury-Setup.exe                    # Production Windows desktop installer
```

---

## Technical Specifications

| Component | Target Runtime | Packaging | Architecture |
| :--- | :--- | :--- | :--- |
| **Mercury GUI** | .NET 8 (Windows Desktop WPF) | Self-Contained / Single Executable | x64 |
| **Mercury CLI** | .NET 8 Console | Self-Contained Single-File Binary | x64 / macOS ARM64 |
| **Mercury Core** | .NET 8 Class Library | Embedded Assembly | Cross-Platform |
| **Installer** | Inno Setup 6 | Native Win32 Setup Executable | x64 |
| **Test Suite** | xUnit + FluentAssertions | .NET 8 Test Project | Cross-Platform |

---

## Core Systems & Pipelines

### 1. Smart Command Processor (`SmartCommandProcessor`)

The command engine analyzes unstructured user input and routes it to the optimal execution path:

- **PowerShell One-Liners (`irm ... | iex`)**: Detected and routed to `powershell.exe` with `-NoProfile -ExecutionPolicy Bypass`, ensuring scripts execute seamlessly without execution policy restrictions.
- **Apple / Unix One-Liners (`curl ... | bash`)**: Detected and routed to macOS `zsh` or native bash with proper pipe handling and streaming output.
- **Git Clone Commands (`git clone <url>`)**: URL is extracted, cloned into the project workspace (`%LOCALAPPDATA%\Mercury\projects`), and automatically passed to the project scanner for dependency resolution.
- **Package Manager Invocation (`pip install ...`, `npm install ...`)**: Executed directly within the active project directory environment.
- **Shorthand Repositories (`owner/repo`)**: Automatically converted to full HTTPS Git URLs and cloned.

### 2. Native Terminal Integration (`TerminalService`)

Developers need direct access to native terminals during development. Mercury provides one-click terminal launching:

- **Windows PowerShell**: Opens an interactive session in the project root with `-ExecutionPolicy Bypass`.
- **Command Prompt (CMD)**: Launches standard Command Prompt in the target project folder.
- **Apple Terminal**: On macOS, triggers `open -a Terminal <path>` to spawn native Apple Terminal.
- **Project Directory Explorer**: Opens Windows File Explorer or macOS Finder at the project location.

### 3. Polyglot Project Scanner (`ProjectScanner`)

Mercury's scanner evaluates repository files in parallel across 10+ technology stacks:

1. Identifies package managers (`pip`, `poetry`, `npm`, `pnpm`, `yarn`, `bun`, `cargo`, `dotnet`, `maven`, `gradle`, `cmake`, `composer`, `bundle`).
2. Generates optimized dependency installation commands (e.g. `python -m pip install -r requirements.txt`).
3. Generates optimal build and test commands.
4. Identifies the primary executable entry point (`main.py`, `index.js`, `cargo run`, `dotnet run`).

### 4. Security Verification Engine (`CommandClassifier` & `SecurityScanner`)

Security is built into the execution lifecycle:

- **Safe Commands**: Read-only, build, and package installation commands (`git clone`, `npm install`, `dotnet build`) execute immediately.
- **Review Required**: Commands modifying system configurations or accessing sensitive paths require explicit user confirmation.
- **Dangerous Commands**: Potentially harmful operations (`format`, `rmdir /s /q C:\`, `del /f /s /q C:\Windows`) are blocked by default.

---

## Deployment & Persistence

- **Installer Output**: `dist\Mercury-Setup.exe` (Self-contained, includes desktop icon, Start Menu shortcut, optional PATH entry, and right-click folder context menu).
- **CLI Executable**: `publish\cli\mercury.exe` (Single-file executable for CI/CD and terminal automation).
- **Projects Directory**: `%LOCALAPPDATA%\Mercury\projects\`
- **Profiles & State**: `%LOCALAPPDATA%\Mercury\profiles.json`
- **Application Logs**: `%LOCALAPPDATA%\Mercury\logs\`
