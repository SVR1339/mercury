using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class RustDetector : IProjectDetector
{
    public string Name => "Rust";
    public int Priority => 40;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        var cargoToml = Path.Combine(projectPath, "Cargo.toml");

        if (!File.Exists(cargoToml))
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        var evidence = new List<string> { "Cargo.toml" };
        if (File.Exists(Path.Combine(projectPath, "Cargo.lock")))
            evidence.Add("Cargo.lock");

        result.Technology = new DetectedTechnology
        {
            Name = "Rust",
            Category = TechCategory.Language,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime
        {
            Name = "Rust/Cargo",
            RequiredVersion = "stable",
            Status = RuntimeStatus.Unknown
        };

        result.PackageManager = new DetectedPackageManager
        {
            Name = "Cargo",
            ManifestFile = "Cargo.toml",
            LockFile = evidence.Contains("Cargo.lock") ? "Cargo.lock" : null,
            InstallCommand = "cargo fetch"
        };

        result.BuildCommand = "cargo build --release";
        result.TestCommand = "cargo test";
        result.RunCommands.Add("cargo run");

        return Task.FromResult(result);
    }
}
