using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Services;

public class GitService
{
    private readonly ProcessRunner _runner = new();

    public event Action<string>? OnProgress;

    public bool IsGitHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        url = url.Trim();
        return Regex.IsMatch(url, @"^(https?://|git@|ssh://git@)github\.com[/:]", RegexOptions.IgnoreCase);
    }

    public bool IsGitUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        url = url.Trim();
        return Regex.IsMatch(url, @"^(https?://|git@|ssh://|git://)", RegexOptions.IgnoreCase) ||
               url.EndsWith(".git", StringComparison.OrdinalIgnoreCase);
    }

    public string NormalizeGitUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        url = url.Trim();

        // Extract from "git clone <url>" or "gh repo clone <url>"
        var cloneMatch = Regex.Match(url, @"(?:git\s+clone|gh\s+repo\s+clone)\s+([^\s;&|]+(?:\s+-[^\s;&|]+)*\s+)?(https?://[^\s;&|]+|git@[^\s;&|]+|[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+)", RegexOptions.IgnoreCase);
        if (cloneMatch.Success)
        {
            url = cloneMatch.Groups[2].Value;
        }

        // Shorthand owner/repo
        if (Regex.IsMatch(url, @"^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$"))
        {
            return $"https://github.com/{url}.git";
        }

        url = url.TrimEnd('/');

        // Strip /tree/main, /tree/master, /blob/main, etc.
        url = Regex.Replace(url, @"/(?:tree|blob)/[^/]+.*$", "");

        // Convert SSH format git@github.com:user/repo to https://github.com/user/repo.git
        if (url.StartsWith("git@"))
        {
            url = Regex.Replace(url, @"^git@([^:]+):", "https://$1/");
        }

        // Remove query parameters or fragments
        if (url.Contains('?')) url = url.Substring(0, url.IndexOf('?'));
        if (url.Contains('#')) url = url.Substring(0, url.IndexOf('#'));

        if (!url.EndsWith(".git") && (url.Contains("github.com") || url.StartsWith("http")))
            url += ".git";

        return url;
    }

    public string ExtractRepoName(string url)
    {
        var normalized = NormalizeGitUrl(url);
        var match = Regex.Match(normalized, @"/([^/]+?)(?:\.git)?$");
        return match.Success ? match.Groups[1].Value : "project";
    }

    public async Task<OperationResult> CloneAsync(string url, string targetDir, CancellationToken ct = default)
    {
        OnProgress?.Invoke($"Cloning {url}...");
        var result = await _runner.RunAsync("git", $"clone --progress \"{url}\" \"{targetDir}\"",
            Path.GetDirectoryName(targetDir), ct: ct, timeoutMs: 600_000);

        if (result.Success)
        {
            // Initialize submodules if present
            var gitmodulesPath = Path.Combine(targetDir, ".gitmodules");
            if (File.Exists(gitmodulesPath))
            {
                OnProgress?.Invoke("Initializing submodules...");
                await _runner.RunAsync("git", "submodule update --init --recursive",
                    targetDir, ct: ct, timeoutMs: 300_000);
            }
        }

        return result;
    }

    public async Task<bool> HasLocalChangesAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _runner.RunAsync("git", "status --porcelain", repoPath, ct: ct);
        return result.Success && !string.IsNullOrWhiteSpace(result.StdOut);
    }

    public async Task<OperationResult> PullAsync(string repoPath, CancellationToken ct = default)
    {
        OnProgress?.Invoke("Fetching updates...");
        var fetchResult = await _runner.RunAsync("git", "fetch", repoPath, ct: ct);
        if (!fetchResult.Success) return fetchResult;

        OnProgress?.Invoke("Pulling changes...");
        return await _runner.RunAsync("git", "pull", repoPath, ct: ct);
    }
}
