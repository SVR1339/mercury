# Contributing to Mercury

First off, thank you for considering contributing to **Mercury**! Tools like this grow through the enthusiasm and collaboration of the developer community.

---

## 🛠️ Development Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Git](https://git-scm.com)
- Optional: Visual Studio 2022 (v17.8+), VS Code with C# Dev Kit, or JetBrains Rider
- Optional: [Inno Setup 6](https://jrsoftware.org/isinfo.php) (only if compiling the Windows installer package)

### Getting the Code

```bash
git clone https://github.com/svreddy313-dev/mercury.git
cd mercury
```

### Running Tests

Mercury has an extensive test suite verifying detection across all supported languages:

```bash
dotnet test
```

### Running the CLI Locally

```bash
dotnet run --project src/UniversalGitHubInstaller.Cli -- --help
dotnet run --project src/UniversalGitHubInstaller.Cli -- doctor
```

### Running the GUI Locally

```bash
dotnet run --project src/UniversalGitHubInstaller
```

---

## 🧩 Adding a New Detector

Mercury's polyglot detection engine is modular. To add support for a new technology or framework:

1. Create a new class implementing `IProjectDetector` inside `src/UniversalGitHubInstaller.Core/Detection/`:

   ```csharp
   public class MyLanguageDetector : IProjectDetector
   {
       public string Name => "MyLanguage";
       public int Priority => 50;

       public Task<ProjectDetectionResult?> DetectAsync(string projectPath, CancellationToken cancellationToken = default)
       {
           // Inspect projectPath for manifest files (e.g. project.json, config.yaml)
           // Return detected dependencies, build command, test command, and run command.
       }
   }
   ```

2. Register the detector in `ProjectScanner.cs`.
3. Add corresponding unit tests in `tests/UniversalGitHubInstaller.Tests/DetectionTests.cs`.
4. Run `dotnet test` to confirm everything passes.

---

## 📋 Pull Request Process

1. Fork the repo and create your branch from `main`:

   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Write clean, readable C# following standard .NET coding conventions.
3. Ensure all automated tests pass: `dotnet test`.
4. Add new tests covering your changes or new detectors.
5. Submit a Pull Request with a clear description of the problem solved and testing performed.

---

## 💬 Community & Questions

- Have questions? Start a thread in [GitHub Discussions](https://github.com/svreddy313-dev/mercury/discussions).
- Found a bug? Open a [Bug Report](https://github.com/svreddy313-dev/mercury/issues/new?template=bug_report.yml).
- Have a feature request? Submit a [Feature Request](https://github.com/svreddy313-dev/mercury/issues/new?template=feature_request.yml).
