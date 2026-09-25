using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class DockerDetector : IProjectDetector
{
    public string Name => "Docker";
    public int Priority => 90;

    private static readonly string[] ComposeFiles = {
        "compose.yml", "compose.yaml", "docker-compose.yml", "docker-compose.yaml"
    };

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        var evidence = new List<string>();

        // Check for Dockerfile
        var dockerFiles = Directory.GetFiles(projectPath, "Dockerfile*", SearchOption.TopDirectoryOnly);
        foreach (var df in dockerFiles)
            evidence.Add(Path.GetFileName(df));

        // Check for compose files
        string? composeFile = null;
        foreach (var cf in ComposeFiles)
        {
            if (File.Exists(Path.Combine(projectPath, cf)))
            {
                evidence.Add(cf);
                composeFile ??= cf;
            }
        }

        if (evidence.Count == 0)
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;

        result.Technology = new DetectedTechnology
        {
            Name = "Docker",
            Category = TechCategory.Container,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime
        {
            Name = "Docker",
            RequiredVersion = "24+",
            Status = RuntimeStatus.Unknown
        };

        // Parse compose for services
        if (composeFile != null)
        {
            try
            {
                var content = File.ReadAllText(Path.Combine(projectPath, composeFile));
                // Simple YAML service detection
                var serviceMatches = Regex.Matches(content, @"^\s{2}(\w[\w-]*)\s*:", RegexOptions.Multiline);
                foreach (Match m in serviceMatches)
                {
                    var svcName = m.Groups[1].Value;
                    if (svcName is "version" or "services" or "volumes" or "networks" or "secrets" or "configs")
                        continue;

                    var svcType = InferServiceType(svcName, content);
                    int? port = null;
                    var portMatch = Regex.Match(content, $@"{svcName}:.*?ports:\s*\n\s*-\s*""?(\d+)", RegexOptions.Singleline);
                    if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var p))
                        port = p;

                    result.Services.Add(new DetectedService
                    {
                        Name = svcName,
                        Type = svcType,
                        Port = port,
                        IsRequired = true
                    });
                }
            }
            catch { }

            result.RunCommands.Add($"docker compose -f {composeFile} up -d");
        }

        return Task.FromResult(result);
    }

    private static string InferServiceType(string name, string content)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("postgres") || lower.Contains("pg") || lower == "db") return "Database";
        if (lower.Contains("mysql") || lower.Contains("mariadb")) return "Database";
        if (lower.Contains("mongo")) return "Database";
        if (lower.Contains("redis")) return "Cache";
        if (lower.Contains("rabbit") || lower.Contains("mq")) return "MessageQueue";
        if (lower.Contains("elastic") || lower.Contains("search")) return "Search";
        if (lower.Contains("web") || lower.Contains("app") || lower.Contains("api")) return "Application";
        if (lower.Contains("worker") || lower.Contains("celery")) return "Worker";
        if (lower.Contains("nginx") || lower.Contains("proxy") || lower.Contains("caddy")) return "Proxy";
        return "Service";
    }
}
