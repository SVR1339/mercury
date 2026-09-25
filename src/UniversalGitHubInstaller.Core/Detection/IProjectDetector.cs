using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public interface IProjectDetector
{
    string Name { get; }
    int Priority { get; }
    Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default);
}

public class DetectionResult
{
    public bool Detected { get; set; }
    public DetectedTechnology? Technology { get; set; }
    public DetectedPackageManager? PackageManager { get; set; }
    public DetectedRuntime? Runtime { get; set; }
    public List<DetectedService> Services { get; set; } = new();
    public List<string> ConfigFiles { get; set; } = new();
    public string? BuildCommand { get; set; }
    public string? TestCommand { get; set; }
    public List<string> RunCommands { get; set; } = new();
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();
}
