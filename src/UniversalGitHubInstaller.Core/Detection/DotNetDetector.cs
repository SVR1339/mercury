using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class DotNetDetector : IProjectDetector
{
    public string Name => ".NET";
    public int Priority => 30;

    private static readonly string[] SolutionExts = { ".sln", ".slnx" };
    private static readonly string[] ProjectExts = { ".csproj", ".fsproj", ".vbproj" };

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        var evidenceFiles = new List<string>();

        var files = Directory.GetFiles(projectPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (SolutionExts.Contains(ext) || ProjectExts.Contains(ext))
                evidenceFiles.Add(Path.GetFileName(f));
        }

        // Also check one level deep for .csproj etc.
        try
        {
            foreach (var dir in Directory.GetDirectories(projectPath))
            {
                foreach (var f in Directory.GetFiles(dir, "*.*proj"))
                    evidenceFiles.Add(Path.GetRelativePath(projectPath, f));
            }
        }
        catch { }

        if (evidenceFiles.Count == 0)
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;

        // Check global.json for SDK version
        string? sdkVersion = null;
        var globalJson = Path.Combine(projectPath, "global.json");
        if (File.Exists(globalJson))
        {
            try
            {
                var content = File.ReadAllText(globalJson);
                var match = System.Text.RegularExpressions.Regex.Match(content, @"""version""\s*:\s*""([^""]+)""");
                if (match.Success) sdkVersion = match.Groups[1].Value;
            }
            catch { }
        }

        result.Technology = new DetectedTechnology
        {
            Name = ".NET",
            Version = sdkVersion,
            Category = TechCategory.Language,
            EvidenceFiles = evidenceFiles
        };

        result.Runtime = new DetectedRuntime
        {
            Name = ".NET SDK",
            RequiredVersion = sdkVersion ?? "8.0+",
            Status = RuntimeStatus.Unknown
        };

        result.PackageManager = new DetectedPackageManager
        {
            Name = "NuGet",
            ManifestFile = evidenceFiles.FirstOrDefault(f => f.EndsWith(".csproj") || f.EndsWith(".sln")) ?? "",
            InstallCommand = "dotnet restore"
        };

        result.BuildCommand = "dotnet build";
        result.TestCommand = "dotnet test";

        var slnFile = evidenceFiles.FirstOrDefault(f => f.EndsWith(".sln") || f.EndsWith(".slnx"));
        if (slnFile != null)
            result.RunCommands.Add($"dotnet run --project <select-project>");
        else
        {
            var projFile = evidenceFiles.FirstOrDefault(f => ProjectExts.Any(e => f.EndsWith(e)));
            if (projFile != null)
                result.RunCommands.Add($"dotnet run --project {projFile}");
        }

        return Task.FromResult(result);
    }
}
