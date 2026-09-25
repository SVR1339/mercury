using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class GoDetector : IProjectDetector
{
    public string Name => "Go";
    public int Priority => 50;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();

        if (!File.Exists(Path.Combine(projectPath, "go.mod")))
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        var evidence = new List<string> { "go.mod" };
        if (File.Exists(Path.Combine(projectPath, "go.sum")))
            evidence.Add("go.sum");

        result.Technology = new DetectedTechnology
        {
            Name = "Go",
            Category = TechCategory.Language,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime { Name = "Go", RequiredVersion = "1.21+", Status = RuntimeStatus.Unknown };

        result.PackageManager = new DetectedPackageManager
        {
            Name = "Go Modules",
            ManifestFile = "go.mod",
            LockFile = evidence.Contains("go.sum") ? "go.sum" : null,
            InstallCommand = "go mod download"
        };

        result.BuildCommand = "go build ./...";
        result.TestCommand = "go test ./...";
        result.RunCommands.Add("go run .");

        return Task.FromResult(result);
    }
}
