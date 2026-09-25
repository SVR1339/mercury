# Universal GitHub Project Installer — Architecture

## Overview

Universal GitHub Project Installer (UGPI) is a native Windows desktop application that automates the setup of GitHub repositories. Users provide a repo folder, ZIP, or URL, and UGPI handles detection, dependency installation, building, testing, and launching.

## Solution Structure

```
UniversalGitHubInstaller.sln
├── src/
│   ├── UniversalGitHubInstaller.Core/     # Shared core engine (netstandard/net8.0)
│   │   ├── Detection/                     # Project technology detectors
│   │   │   ├── IProjectDetector.cs        # Detector interface
│   │   │   ├── ProjectScanner.cs          # Orchestrates all detectors
│   │   │   ├── PythonDetector.cs
│   │   │   ├── NodeDetector.cs
│   │   │   ├── DotNetDetector.cs
│   │   │   ├── RustDetector.cs
│   │   │   ├── GoDetector.cs
│   │   │   ├── JavaDetector.cs
│   │   │   ├── PhpDetector.cs
│   │   │   ├── RubyDetector.cs
│   │   │   ├── CppDetector.cs
│   │   │   ├── DockerDetector.cs
│   │   │   └── EnvironmentDetector.cs
│   │   ├── Models/
│   │   │   ├── ProjectInfo.cs             # Scan result model
│   │   │   ├── ProjectProfile.cs          # Saved project profile
│   │   │   ├── OperationResult.cs         # Command execution result
│   │   │   ├── CommandClassification.cs   # Security classification
│   │   │   └── WindowsEnvironmentInfo.cs  # System environment state
│   │   ├── Runtime/
│   │   │   └── WindowsEnvironmentScanner.cs  # System diagnostics
│   │   ├── Security/
│   │   │   ├── CommandClassifier.cs       # SAFE/REVIEW/DANGEROUS
│   │   │   └── SecurityScanner.cs         # Repository security scan
│   │   ├── Services/
│   │   │   ├── ProcessRunner.cs           # Async process execution
│   │   │   ├── GitService.cs              # Git clone/pull/status
│   │   │   ├── ZipService.cs              # Safe ZIP extraction
│   │   │   └── ProjectProfileService.cs   # Profile save/load
│   │   └── Logging/
│   │       └── AppLogger.cs               # Structured file logging
│   │
│   ├── UniversalGitHubInstaller/          # WPF GUI (net8.0-windows)
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── Views/
│   │   │   └── MainWindow.xaml / .cs      # Main application window
│   │   ├── ViewModels/
│   │   │   ├── BaseViewModel.cs           # INotifyPropertyChanged base
│   │   │   ├── MainViewModel.cs           # Main application logic
│   │   │   └── RelayCommand.cs            # ICommand implementation
│   │   └── Converters/
│   │       └── BoolToVisibilityConverter.cs
│   │
│   └── UniversalGitHubInstaller.Cli/      # CLI tool (net8.0, single-file)
│       └── Program.cs                     # ugi.exe entry point
│
├── tests/
│   └── UniversalGitHubInstaller.Tests/    # Unit tests (xUnit)
│
├── installer.iss                          # Inno Setup installer script
├── app.ico                                # Application icon
└── dist/                                  # Final installer output
    └── Universal-GitHub-Project-Installer-Setup.exe
```

## Technology Stack

| Component | Technology |
|-----------|-----------|
| **GUI** | C# / .NET 8 / WPF |
| **CLI** | C# / .NET 8 / Console |
| **Core Engine** | C# / .NET 8 Class Library |
| **Installer** | Inno Setup 6 |
| **Packaging** | Self-contained x64, no runtime required |
| **Tests** | xUnit |

## Architecture Principles

1. **Shared Core Engine**: GUI and CLI share `UniversalGitHubInstaller.Core`. No duplicated logic.
2. **Modular Detection**: Each technology has its own `IProjectDetector` implementation.
3. **Security-First**: All commands classified as SAFE/REVIEW/DANGEROUS before execution.
4. **Async-First**: All long operations are async with cancellation support.
5. **Self-Contained**: End user does not need to install .NET runtime.

## Deployment

- **Install Location**: `%LOCALAPPDATA%\Programs\Universal GitHub Project Installer`
- **CLI on PATH**: Optional `ugi.exe` added to user PATH
- **Context Menu**: Optional "Install with UGPI" right-click on folders
- **Logs**: `%LOCALAPPDATA%\UniversalGitHubProjectInstaller\logs\`

## Version

- v1.0.0
