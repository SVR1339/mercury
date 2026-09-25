using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniversalGitHubInstaller.Core.Models;

public class ProjectProfile
{
    public string ProjectName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string? GitRemote { get; set; }
    public string? Branch { get; set; }
    public string? Commit { get; set; }
    public List<string> DetectedStack { get; set; } = new();
    public Dictionary<string, string> RuntimeVersions { get; set; } = new();
    public string? DependencyManager { get; set; }
    public List<string> InstallActions { get; set; } = new();
    public string? BuildCommand { get; set; }
    public string? TestCommand { get; set; }
    public string? RunCommand { get; set; }
    public Dictionary<string, int> ServicePorts { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });

    public static ProjectProfile? FromJson(string json) =>
        JsonSerializer.Deserialize<ProjectProfile>(json);
}
