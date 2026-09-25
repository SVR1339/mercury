using System.Collections.Generic;

namespace UniversalGitHubInstaller.Core.Models;

public class WindowsEnvironmentInfo
{
    public string WindowsVersion { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string CpuArchitecture { get; set; } = string.Empty;
    public long AvailableDiskSpaceMB { get; set; }
    public long TotalRamMB { get; set; }
    public bool HasGit { get; set; }
    public string? GitVersion { get; set; }
    public bool HasPowerShell { get; set; }
    public bool HasWinget { get; set; }
    public bool HasChocolatey { get; set; }
    public bool HasScoop { get; set; }
    public Dictionary<string, RuntimeInfo> Runtimes { get; set; } = new();
}

public class RuntimeInfo
{
    public bool Installed { get; set; }
    public string? Version { get; set; }
    public string? Path { get; set; }
}
