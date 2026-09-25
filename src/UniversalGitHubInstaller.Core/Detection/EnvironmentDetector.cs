using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class EnvironmentDetector : IProjectDetector
{
    public string Name => "Environment";
    public int Priority => 100;

    private static readonly string[] EnvTemplates = {
        ".env.example", ".env.sample", ".env.template", ".env.dist", ".example.env"
    };

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        string? envTemplate = null;

        foreach (var t in EnvTemplates)
        {
            if (File.Exists(Path.Combine(projectPath, t)))
            {
                envTemplate = t;
                break;
            }
        }

        if (envTemplate == null)
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        result.ConfigFiles.Add(envTemplate);

        // Parse environment template
        try
        {
            var lines = File.ReadAllLines(Path.Combine(projectPath, envTemplate));
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0) continue;

                var key = trimmed[..eqIdx].Trim();
                var value = trimmed[(eqIdx + 1)..].Trim();

                bool isSecret = key.Contains("KEY", StringComparison.OrdinalIgnoreCase) ||
                                key.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
                                key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
                                key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase);

                bool isRequired = string.IsNullOrEmpty(value) ||
                                  value.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                                  value.Equals("your_key_here", StringComparison.OrdinalIgnoreCase) ||
                                  value.StartsWith("<") || value.StartsWith("TODO");

                result.EnvironmentVariables.Add(new EnvironmentVariable
                {
                    Key = key,
                    DefaultValue = string.IsNullOrEmpty(value) ? null : value,
                    IsRequired = isRequired,
                    IsSecret = isSecret
                });
            }
        }
        catch { }

        return Task.FromResult(result);
    }
}
