using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class PythonDetector : IProjectDetector
{
    public string Name => "Python";
    public int Priority => 10;

    private static readonly string[] ManifestFiles = {
        "requirements.txt", "pyproject.toml", "setup.py", "setup.cfg",
        "Pipfile", "Pipfile.lock", "poetry.lock", "uv.lock", "environment.yml"
    };

    private static readonly string[] EntryPoints = {
        "app.py", "main.py", "manage.py", "run.py", "server.py", "wsgi.py"
    };

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        var foundFiles = new List<string>();

        foreach (var mf in ManifestFiles)
        {
            if (File.Exists(Path.Combine(projectPath, mf)))
                foundFiles.Add(mf);
        }

        if (foundFiles.Count == 0)
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;

        // Determine package manager
        string installCmd;
        string? lockFile = null;
        string pmName;

        if (foundFiles.Contains("uv.lock") || (foundFiles.Contains("pyproject.toml") && FileContains(projectPath, "pyproject.toml", "[tool.uv]")))
        {
            pmName = "uv";
            installCmd = "uv sync";
            lockFile = "uv.lock";
        }
        else if (foundFiles.Contains("poetry.lock") || (foundFiles.Contains("pyproject.toml") && FileContains(projectPath, "pyproject.toml", "[tool.poetry]")))
        {
            pmName = "Poetry";
            installCmd = "poetry install";
            lockFile = "poetry.lock";
        }
        else if (foundFiles.Contains("Pipfile") || foundFiles.Contains("Pipfile.lock"))
        {
            pmName = "Pipenv";
            installCmd = "pipenv install";
            lockFile = foundFiles.Contains("Pipfile.lock") ? "Pipfile.lock" : null;
        }
        else if (foundFiles.Contains("requirements.txt"))
        {
            pmName = "pip";
            installCmd = "pip install -r requirements.txt";
        }
        else if (foundFiles.Contains("setup.py"))
        {
            pmName = "pip";
            installCmd = "pip install -e .";
        }
        else
        {
            pmName = "pip";
            installCmd = "pip install .";
        }

        int depCount = 0;
        if (foundFiles.Contains("requirements.txt"))
        {
            try
            {
                var lines = File.ReadAllLines(Path.Combine(projectPath, "requirements.txt"));
                depCount = lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#") && !l.TrimStart().StartsWith("-"));
            }
            catch { }
        }

        result.PackageManager = new DetectedPackageManager
        {
            Name = pmName,
            ManifestFile = foundFiles.FirstOrDefault(),
            LockFile = lockFile,
            InstallCommand = installCmd,
            DependencyCount = depCount
        };

        // Detect version requirement
        string? requiredVersion = null;
        if (foundFiles.Contains("pyproject.toml"))
        {
            try
            {
                var content = File.ReadAllText(Path.Combine(projectPath, "pyproject.toml"));
                var match = Regex.Match(content, @"requires-python\s*=\s*""([^""]+)""");
                if (match.Success) requiredVersion = match.Groups[1].Value;
            }
            catch { }
        }

        // Detect framework
        string? framework = DetectPythonFramework(projectPath, foundFiles);

        result.Technology = new DetectedTechnology
        {
            Name = "Python",
            Version = requiredVersion,
            Framework = framework,
            Category = TechCategory.Language,
            EvidenceFiles = foundFiles
        };

        result.Runtime = new DetectedRuntime
        {
            Name = "Python",
            RequiredVersion = requiredVersion ?? "3.8+",
            Status = RuntimeStatus.Unknown
        };

        // Detect run commands
        foreach (var ep in EntryPoints)
        {
            if (File.Exists(Path.Combine(projectPath, ep)))
                result.RunCommands.Add($"python {ep}");
        }

        // Check for src/ directory
        var srcDir = Path.Combine(projectPath, "src");
        if (Directory.Exists(srcDir))
        {
            foreach (var ep in EntryPoints)
            {
                if (File.Exists(Path.Combine(srcDir, ep)))
                    result.RunCommands.Add($"python src/{ep}");
            }
        }

        // Framework-specific run commands
        if (framework == "Django" && File.Exists(Path.Combine(projectPath, "manage.py")))
            result.RunCommands.Add("python manage.py runserver");
        else if (framework == "FastAPI")
            result.RunCommands.Add("uvicorn main:app --reload");
        else if (framework == "Flask")
            result.RunCommands.Add("flask run");

        // Tests
        if (Directory.Exists(Path.Combine(projectPath, "tests")) ||
            Directory.Exists(Path.Combine(projectPath, "test")) ||
            File.Exists(Path.Combine(projectPath, "pytest.ini")) ||
            File.Exists(Path.Combine(projectPath, "tox.ini")))
        {
            result.TestCommand = "pytest";
        }

        return Task.FromResult(result);
    }

    private string? DetectPythonFramework(string projectPath, List<string> foundFiles)
    {
        var filesToCheck = new[] { "requirements.txt", "pyproject.toml", "setup.py", "Pipfile" };
        foreach (var file in filesToCheck)
        {
            if (!foundFiles.Contains(file)) continue;
            try
            {
                var content = File.ReadAllText(Path.Combine(projectPath, file)).ToLowerInvariant();
                if (content.Contains("django")) return "Django";
                if (content.Contains("fastapi")) return "FastAPI";
                if (content.Contains("flask")) return "Flask";
                if (content.Contains("streamlit")) return "Streamlit";
                if (content.Contains("gradio")) return "Gradio";
            }
            catch { }
        }
        return null;
    }

    private static bool FileContains(string basePath, string fileName, string text)
    {
        try
        {
            var content = File.ReadAllText(Path.Combine(basePath, fileName));
            return content.Contains(text, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
