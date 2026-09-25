using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class PhpDetector : IProjectDetector
{
    public string Name => "PHP";
    public int Priority => 70;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        if (!File.Exists(Path.Combine(projectPath, "composer.json")))
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        var evidence = new List<string> { "composer.json" };
        if (File.Exists(Path.Combine(projectPath, "composer.lock")))
            evidence.Add("composer.lock");

        bool isLaravel = File.Exists(Path.Combine(projectPath, "artisan"));

        result.Technology = new DetectedTechnology
        {
            Name = "PHP",
            Framework = isLaravel ? "Laravel" : null,
            Category = TechCategory.Language,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime { Name = "PHP", RequiredVersion = "8.1+", Status = RuntimeStatus.Unknown };
        result.PackageManager = new DetectedPackageManager
        {
            Name = "Composer",
            ManifestFile = "composer.json",
            LockFile = evidence.Contains("composer.lock") ? "composer.lock" : null,
            InstallCommand = evidence.Contains("composer.lock") ? "composer install" : "composer install"
        };

        if (isLaravel)
        {
            result.RunCommands.Add("php artisan serve");
            result.TestCommand = "php artisan test";
        }

        return Task.FromResult(result);
    }
}
