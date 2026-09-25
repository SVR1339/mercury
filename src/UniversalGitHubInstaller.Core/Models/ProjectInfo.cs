using System;
using System.Collections.Generic;

namespace UniversalGitHubInstaller.Core.Models;

public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? GitRemote { get; set; }
    public string? Branch { get; set; }
    public string? Commit { get; set; }
    public List<DetectedTechnology> Technologies { get; set; } = new();
    public List<DetectedPackageManager> PackageManagers { get; set; } = new();
    public List<DetectedService> Services { get; set; } = new();
    public List<DetectedRuntime> Runtimes { get; set; } = new();
    public List<string> ConfigFiles { get; set; } = new();
    public List<string> Scripts { get; set; } = new();
    public string? BuildSystem { get; set; }
    public string? TestSystem { get; set; }
    public List<string> PossibleRunCommands { get; set; } = new();
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();
    public string? ReadmeExcerpt { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

public class DetectedTechnology
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Framework { get; set; }
    public TechCategory Category { get; set; }
    public List<string> EvidenceFiles { get; set; } = new();
}

public enum TechCategory
{
    Language,
    Frontend,
    Backend,
    Database,
    Container,
    BuildTool,
    TestTool
}

public class DetectedPackageManager
{
    public string Name { get; set; } = string.Empty;
    public string? LockFile { get; set; }
    public string? ManifestFile { get; set; }
    public int DependencyCount { get; set; }
    public string InstallCommand { get; set; } = string.Empty;
}

public class DetectedService
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? Port { get; set; }
    public bool IsRequired { get; set; }
}

public class DetectedRuntime
{
    public string Name { get; set; } = string.Empty;
    public string? RequiredVersion { get; set; }
    public RuntimeStatus Status { get; set; }
    public string? InstalledVersion { get; set; }
    public string? InstallCommand { get; set; }
}

public enum RuntimeStatus
{
    Installed,
    Missing,
    WrongVersion,
    Unknown
}

public class EnvironmentVariable
{
    public string Key { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSecret { get; set; }
    public string? Description { get; set; }
}
