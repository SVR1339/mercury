using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Services;

public class ProjectProfileService
{
    private const string ProfileDir = ".project-installer";
    private const string ProfileFile = "project.json";

    public ProjectProfile CreateProfile(ProjectInfo info)
    {
        return new ProjectProfile
        {
            ProjectName = info.Name,
            SourcePath = info.Path,
            GitRemote = info.GitRemote,
            Branch = info.Branch,
            Commit = info.Commit,
            DetectedStack = info.Technologies.Select(t => t.Framework != null ? $"{t.Name} ({t.Framework})" : t.Name).ToList(),
            RuntimeVersions = info.Runtimes.ToDictionary(r => r.Name, r => r.InstalledVersion ?? r.RequiredVersion ?? "unknown"),
            DependencyManager = info.PackageManagers.FirstOrDefault()?.Name,
            InstallActions = info.PackageManagers.Select(pm => pm.InstallCommand).ToList(),
            BuildCommand = info.BuildSystem,
            TestCommand = info.TestSystem,
            RunCommand = info.PossibleRunCommands.FirstOrDefault(),
            ServicePorts = info.Services.Where(s => s.Port.HasValue).ToDictionary(s => s.Name, s => s.Port!.Value),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SaveProfile(ProjectProfile profile, string projectPath)
    {
        var dir = Path.Combine(projectPath, ProfileDir);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, ProfileFile);
        File.WriteAllText(filePath, profile.ToJson());
    }

    public ProjectProfile? LoadProfile(string projectPath)
    {
        var filePath = Path.Combine(projectPath, ProfileDir, ProfileFile);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return ProjectProfile.FromJson(json);
        }
        catch { return null; }
    }
}
