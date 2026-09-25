using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class NodeDetector : IProjectDetector
{
    public string Name => "Node.js";
    public int Priority => 20;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        var packageJsonPath = Path.Combine(projectPath, "package.json");

        if (!File.Exists(packageJsonPath))
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        var evidenceFiles = new List<string> { "package.json" };

        // Parse package.json
        JsonDocument? pkgDoc = null;
        string? framework = null;
        int depCount = 0;
        var scripts = new Dictionary<string, string>();

        try
        {
            var json = File.ReadAllText(packageJsonPath);
            pkgDoc = JsonDocument.Parse(json);
            var root = pkgDoc.RootElement;

            // Count dependencies
            if (root.TryGetProperty("dependencies", out var deps))
                depCount += deps.EnumerateObject().Count();
            if (root.TryGetProperty("devDependencies", out var devDeps))
                depCount += devDeps.EnumerateObject().Count();

            // Get scripts
            if (root.TryGetProperty("scripts", out var scriptsEl))
            {
                foreach (var prop in scriptsEl.EnumerateObject())
                    scripts[prop.Name] = prop.Value.GetString() ?? "";
            }

            // Detect framework from dependencies
            var allDeps = new List<string>();
            if (root.TryGetProperty("dependencies", out var d))
                allDeps.AddRange(d.EnumerateObject().Select(p => p.Name));
            if (root.TryGetProperty("devDependencies", out var dd))
                allDeps.AddRange(dd.EnumerateObject().Select(p => p.Name));

            if (allDeps.Contains("next")) framework = "Next.js";
            else if (allDeps.Contains("nuxt")) framework = "Nuxt";
            else if (allDeps.Contains("@angular/core")) framework = "Angular";
            else if (allDeps.Contains("vue")) framework = "Vue";
            else if (allDeps.Contains("svelte") || allDeps.Contains("@sveltejs/kit")) framework = "Svelte";
            else if (allDeps.Contains("react")) framework = "React";
            else if (allDeps.Contains("@nestjs/core")) framework = "NestJS";
            else if (allDeps.Contains("express")) framework = "Express";
            else if (allDeps.Contains("vite")) framework = "Vite";
            else if (allDeps.Contains("astro")) framework = "Astro";

            // Detect required Node version
            string? requiredVersion = null;
            if (root.TryGetProperty("engines", out var engines) &&
                engines.TryGetProperty("node", out var nodeVer))
            {
                requiredVersion = nodeVer.GetString();
            }

            result.Runtime = new DetectedRuntime
            {
                Name = "Node.js",
                RequiredVersion = requiredVersion ?? "18+",
                Status = RuntimeStatus.Unknown
            };
        }
        catch { }

        // Determine package manager from lockfiles
        string pmName;
        string installCmd;
        string? lockFile = null;

        if (File.Exists(Path.Combine(projectPath, "pnpm-lock.yaml")))
        {
            pmName = "pnpm"; installCmd = "pnpm install"; lockFile = "pnpm-lock.yaml";
            evidenceFiles.Add("pnpm-lock.yaml");
        }
        else if (File.Exists(Path.Combine(projectPath, "yarn.lock")))
        {
            pmName = "yarn"; installCmd = "yarn install"; lockFile = "yarn.lock";
            evidenceFiles.Add("yarn.lock");
        }
        else if (File.Exists(Path.Combine(projectPath, "bun.lockb")) || File.Exists(Path.Combine(projectPath, "bun.lock")))
        {
            pmName = "bun"; installCmd = "bun install";
            lockFile = File.Exists(Path.Combine(projectPath, "bun.lockb")) ? "bun.lockb" : "bun.lock";
            evidenceFiles.Add(lockFile);
        }
        else if (File.Exists(Path.Combine(projectPath, "package-lock.json")))
        {
            pmName = "npm"; installCmd = "npm ci"; lockFile = "package-lock.json";
            evidenceFiles.Add("package-lock.json");
        }
        else
        {
            pmName = "npm"; installCmd = "npm install";
        }

        result.PackageManager = new DetectedPackageManager
        {
            Name = pmName,
            ManifestFile = "package.json",
            LockFile = lockFile,
            InstallCommand = installCmd,
            DependencyCount = depCount
        };

        result.Technology = new DetectedTechnology
        {
            Name = "Node.js",
            Framework = framework,
            Category = framework != null && new[] { "React", "Vue", "Angular", "Svelte", "Next.js", "Nuxt", "Astro", "Vite" }.Contains(framework)
                ? TechCategory.Frontend : TechCategory.Language,
            EvidenceFiles = evidenceFiles
        };

        // Run commands from scripts
        if (scripts.ContainsKey("dev")) result.RunCommands.Add($"{pmName} run dev");
        if (scripts.ContainsKey("start")) result.RunCommands.Add($"{pmName} start");
        if (scripts.ContainsKey("serve")) result.RunCommands.Add($"{pmName} run serve");

        // Build command
        if (scripts.ContainsKey("build")) result.BuildCommand = $"{pmName} run build";

        // Test command
        if (scripts.ContainsKey("test")) result.TestCommand = $"{pmName} test";

        pkgDoc?.Dispose();
        return Task.FromResult(result);
    }
}
