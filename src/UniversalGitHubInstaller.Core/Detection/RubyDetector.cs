using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class RubyDetector : IProjectDetector
{
    public string Name => "Ruby";
    public int Priority => 75;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        if (!File.Exists(Path.Combine(projectPath, "Gemfile")))
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        var evidence = new List<string> { "Gemfile" };
        if (File.Exists(Path.Combine(projectPath, "Gemfile.lock")))
            evidence.Add("Gemfile.lock");

        bool isRails = File.Exists(Path.Combine(projectPath, "config", "application.rb"));

        result.Technology = new DetectedTechnology
        {
            Name = "Ruby",
            Framework = isRails ? "Rails" : null,
            Category = TechCategory.Language,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime { Name = "Ruby", RequiredVersion = "3.0+", Status = RuntimeStatus.Unknown };
        result.PackageManager = new DetectedPackageManager
        {
            Name = "Bundler",
            ManifestFile = "Gemfile",
            LockFile = evidence.Contains("Gemfile.lock") ? "Gemfile.lock" : null,
            InstallCommand = "bundle install"
        };

        if (isRails)
        {
            result.RunCommands.Add("rails server");
            result.TestCommand = "rails test";
        }

        return Task.FromResult(result);
    }
}
