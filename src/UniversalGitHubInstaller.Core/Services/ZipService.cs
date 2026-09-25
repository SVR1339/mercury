using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Services;

public class ZipService
{
    public event Action<string>? OnProgress;

    public OperationResult Extract(string zipPath, string targetDir)
    {
        try
        {
            if (!File.Exists(zipPath))
                return OperationResult.Fail($"ZIP file not found: {zipPath}");

            OnProgress?.Invoke($"Extracting {Path.GetFileName(zipPath)}...");

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            using var archive = ZipFile.OpenRead(zipPath);

            foreach (var entry in archive.Entries)
            {
                var destPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));

                // Security: prevent directory traversal
                if (!destPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                {
                    OnProgress?.Invoke($"[SECURITY] Blocked unsafe ZIP entry: {entry.FullName}");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destPath);
                }
                else
                {
                    var dir = Path.GetDirectoryName(destPath);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }

            // Detect GitHub's nested directory structure (project-main/)
            var actualRoot = FindRepositoryRoot(targetDir);

            OnProgress?.Invoke("Extraction complete.");
            return new OperationResult
            {
                Success = true,
                Message = "Extraction complete.",
                Detail = actualRoot
            };
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Extraction failed: {ex.Message}");
        }
    }

    public string FindRepositoryRoot(string extractedDir)
    {
        var dirs = Directory.GetDirectories(extractedDir);
        var files = Directory.GetFiles(extractedDir);

        // GitHub ZIP pattern: single subdirectory like "repo-main" or "repo-master"
        if (dirs.Length == 1 && files.Length == 0)
        {
            var subDir = dirs[0];
            var subName = Path.GetFileName(subDir).ToLowerInvariant();
            if (subName.EndsWith("-main") || subName.EndsWith("-master") ||
                subName.EndsWith("-develop") || subName.EndsWith("-dev"))
            {
                return subDir;
            }
            // Also check if the single subdirectory contains typical project files
            var subFiles = Directory.GetFiles(subDir);
            if (subFiles.Any(f =>
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                return name is "package.json" or "requirements.txt" or "pyproject.toml"
                    or "cargo.toml" or "go.mod" or "readme.md";
            }))
            {
                return subDir;
            }
        }

        return extractedDir;
    }
}
