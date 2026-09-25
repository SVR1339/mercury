using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class ProjectScanner
{
    private readonly List<IProjectDetector> _detectors;

    public ProjectScanner()
    {
        _detectors = new List<IProjectDetector>
        {
            new PythonDetector(),
            new NodeDetector(),
            new DotNetDetector(),
            new RustDetector(),
            new GoDetector(),
            new JavaDetector(),
            new PhpDetector(),
            new RubyDetector(),
            new CppDetector(),
            new DockerDetector(),
            new EnvironmentDetector()
        };
    }

    public event Action<string>? OnProgress;

    public async Task<ProjectInfo> ScanAsync(string projectPath, CancellationToken ct = default)
    {
        var info = new ProjectInfo
        {
            Name = Path.GetFileName(projectPath),
            Path = projectPath,
            ScannedAt = DateTime.UtcNow
        };

        OnProgress?.Invoke("Detecting Git information...");
        DetectGitInfo(projectPath, info);

        OnProgress?.Invoke("Reading documentation...");
        info.ReadmeExcerpt = ReadReadme(projectPath);

        foreach (var detector in _detectors.OrderBy(d => d.Priority))
        {
            ct.ThrowIfCancellationRequested();
            OnProgress?.Invoke($"Scanning for {detector.Name}...");

            try
            {
                var result = await detector.DetectAsync(projectPath, ct);
                if (!result.Detected) continue;

                if (result.Technology != null)
                    info.Technologies.Add(result.Technology);
                if (result.PackageManager != null)
                    info.PackageManagers.Add(result.PackageManager);
                if (result.Runtime != null)
                    info.Runtimes.Add(result.Runtime);

                info.Services.AddRange(result.Services);
                info.ConfigFiles.AddRange(result.ConfigFiles);
                info.PossibleRunCommands.AddRange(result.RunCommands);
                info.EnvironmentVariables.AddRange(result.EnvironmentVariables);

                if (result.BuildCommand != null && info.BuildSystem == null)
                    info.BuildSystem = result.BuildCommand;
                if (result.TestCommand != null && info.TestSystem == null)
                    info.TestSystem = result.TestCommand;
            }
            catch (Exception ex)
            {
                OnProgress?.Invoke($"Warning: {detector.Name} detection error: {ex.Message}");
            }
        }

        OnProgress?.Invoke("Scan complete.");
        return info;
    }

    private void DetectGitInfo(string projectPath, ProjectInfo info)
    {
        var gitDir = Path.Combine(projectPath, ".git");
        if (!Directory.Exists(gitDir) && !File.Exists(gitDir)) return;

        try
        {
            info.GitRemote = RunGit(projectPath, "remote get-url origin");
            info.Branch = RunGit(projectPath, "branch --show-current");
            info.Commit = RunGit(projectPath, "rev-parse --short HEAD");
        }
        catch { }
    }

    private string? RunGit(string workDir, string args)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch { return null; }
    }

    private string? ReadReadme(string projectPath)
    {
        var readmeNames = new[] { "README.md", "readme.md", "README.txt", "README", "README.rst" };
        foreach (var name in readmeNames)
        {
            var path = Path.Combine(projectPath, name);
            if (!File.Exists(path)) continue;
            try
            {
                var lines = File.ReadLines(path).Take(30);
                return string.Join(Environment.NewLine, lines);
            }
            catch { }
        }
        return null;
    }
}
