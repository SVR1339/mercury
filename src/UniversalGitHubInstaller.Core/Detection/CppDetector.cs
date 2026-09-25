using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class CppDetector : IProjectDetector
{
    public string Name => "C/C++";
    public int Priority => 80;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        var evidence = new List<string>();

        if (File.Exists(Path.Combine(projectPath, "CMakeLists.txt")))
            evidence.Add("CMakeLists.txt");
        if (File.Exists(Path.Combine(projectPath, "Makefile")))
            evidence.Add("Makefile");
        if (File.Exists(Path.Combine(projectPath, "meson.build")))
            evidence.Add("meson.build");
        if (File.Exists(Path.Combine(projectPath, "vcpkg.json")))
            evidence.Add("vcpkg.json");

        if (evidence.Count == 0)
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        string buildCmd, buildTool;

        if (evidence.Contains("CMakeLists.txt"))
        {
            buildTool = "CMake";
            buildCmd = "cmake -B build && cmake --build build";
        }
        else if (evidence.Contains("meson.build"))
        {
            buildTool = "Meson";
            buildCmd = "meson setup build && meson compile -C build";
        }
        else
        {
            buildTool = "Make";
            buildCmd = "make";
        }

        result.Technology = new DetectedTechnology
        {
            Name = "C/C++",
            Category = TechCategory.Language,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime { Name = buildTool, RequiredVersion = "latest", Status = RuntimeStatus.Unknown };
        result.BuildCommand = buildCmd;

        return Task.FromResult(result);
    }
}
