using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Services;

public enum CommandInputKind
{
    GitRepositoryUrl,
    GitHubShortHand,
    GitCloneCommand,
    PowerShellInstallCommand,
    AppleTerminalInstallCommand,
    PackageManagerInstall,
    CompoundCommand,
    GeneralShellCommand
}

public class SmartCommandInfo
{
    public string RawInput { get; set; } = string.Empty;
    public CommandInputKind Kind { get; set; }
    public string? ExtractedGitUrl { get; set; }
    public string? ExtractedRepoName { get; set; }
    public string ExecutableCommand { get; set; } = string.Empty;
    public ShellType RecommendedShell { get; set; } = ShellType.Auto;
    public string Description { get; set; } = string.Empty;
    public bool RequiresInternet { get; set; } = true;
    public bool IsDirectRepo { get; set; }
}

public class SmartCommandProcessor
{
    private readonly GitService _gitService = new();
    private readonly TerminalService _terminalService = new();
    private readonly ProcessRunner _runner = new();

    /// <summary>
    /// Intelligently parses any user input (GitHub URL, git clone command, PowerShell one-liner,
    /// Apple Terminal curl pipe bash, package manager command, or compound script).
    /// </summary>
    public SmartCommandInfo ParseInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new SmartCommandInfo
            {
                RawInput = "",
                Kind = CommandInputKind.GeneralShellCommand,
                Description = "Empty input."
            };
        }

        var trimmed = input.Trim();

        // 1. Package Manager Git installs (pip install git+..., cargo install --git ..., npm i ..., go install ...)
        if (Regex.IsMatch(trimmed, @"pip\s+install\s+(git\+|https?://)", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"cargo\s+install\s+--git", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"go\s+install\s+github\.com", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"npm\s+(i|install)\s+(-g\s+)?(https?://|github:)", RegexOptions.IgnoreCase))
        {
            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.PackageManagerInstall,
                ExecutableCommand = trimmed,
                RecommendedShell = ShellType.Auto,
                Description = "Direct Package Manager Git Installation (Downloads and installs dependencies)",
                IsDirectRepo = false
            };
        }

        // 2. git clone / gh repo clone commands
        var gitCloneMatch = Regex.Match(trimmed, @"^(?:git\s+clone|gh\s+repo\s+clone)\s+([^\s;&|]+(?:\s+-[^\s;&|]+)*\s+)?(https?://[^\s;&|]+|git@[^\s;&|]+|[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+)", RegexOptions.IgnoreCase);
        if (gitCloneMatch.Success)
        {
            var rawUrl = gitCloneMatch.Groups[2].Value;
            var cleanUrl = rawUrl.StartsWith("http") || rawUrl.StartsWith("git@")
                ? _gitService.NormalizeGitUrl(rawUrl)
                : $"https://github.com/{rawUrl}.git";
            var repoName = _gitService.ExtractRepoName(cleanUrl);

            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.GitCloneCommand,
                ExtractedGitUrl = cleanUrl,
                ExtractedRepoName = repoName,
                ExecutableCommand = trimmed,
                RecommendedShell = ShellType.PowerShell,
                Description = $"Git Clone Command: {repoName} (Extracts repository & automates dependencies)",
                IsDirectRepo = true
            };
        }

        // 3. PowerShell Install One-Liners (irm ... | iex, iwr ... | iex, powershell -c ...)
        if (Regex.IsMatch(trimmed, @"(irm|iwr|Invoke-RestMethod|Invoke-WebRequest).+\|\s*(iex|Invoke-Expression)", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"powershell.*(-c|-Command|-File)", RegexOptions.IgnoreCase) ||
            trimmed.StartsWith("Install-Module", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("winget", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("choco", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("scoop", StringComparison.OrdinalIgnoreCase))
        {
            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.PowerShellInstallCommand,
                ExecutableCommand = trimmed,
                RecommendedShell = ShellType.PowerShell,
                Description = "PowerShell One-Liner / Windows Installer (Executes with TLS 1.2/1.3 & ExecutionPolicy Bypass)",
                IsDirectRepo = false
            };
        }

        // 4. Apple Terminal / Unix Shell Install One-Liners (curl ... | bash, curl ... | sh, brew install ...)
        if (Regex.IsMatch(trimmed, @"curl.*\|\s*(bash|sh|zsh)", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"wget.*\|\s*(bash|sh)", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"(sh|bash|zsh)\s+-c\s+""\$\(curl", RegexOptions.IgnoreCase) ||
            trimmed.StartsWith("brew", StringComparison.OrdinalIgnoreCase))
        {
            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.AppleTerminalInstallCommand,
                ExecutableCommand = trimmed,
                RecommendedShell = TerminalService.IsMacOS ? ShellType.AppleTerminal : ShellType.Bash,
                Description = "Apple Terminal / Bash Installer (Executes in Unix Shell with full internet access)",
                IsDirectRepo = false
            };
        }

        // 5. Short GitHub repo format: "owner/repo" (e.g., "facebook/react" or "torvalds/linux")
        var shortMatch = Regex.Match(trimmed, @"^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$");
        if (shortMatch.Success && !trimmed.Contains(" ") && !trimmed.Contains("\\"))
        {
            var url = $"https://github.com/{trimmed}.git";
            var repoName = trimmed.Split('/')[1];
            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.GitHubShortHand,
                ExtractedGitUrl = url,
                ExtractedRepoName = repoName,
                ExecutableCommand = $"git clone \"{url}\"",
                RecommendedShell = ShellType.PowerShell,
                Description = $"GitHub Repo Shorthand: {trimmed} (Resolves to {url})",
                IsDirectRepo = true
            };
        }

        // 6. Direct Git/GitHub URL
        if (_gitService.IsGitUrl(trimmed) || _gitService.IsGitHubUrl(trimmed))
        {
            var normalized = _gitService.NormalizeGitUrl(trimmed);
            var repoName = _gitService.ExtractRepoName(normalized);
            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.GitRepositoryUrl,
                ExtractedGitUrl = normalized,
                ExtractedRepoName = repoName,
                ExecutableCommand = $"git clone \"{normalized}\"",
                RecommendedShell = ShellType.PowerShell,
                Description = $"GitHub Repository: {repoName} (Clone & Auto-Install)",
                IsDirectRepo = true
            };
        }

        // 7. Compound Commands containing && or ; or |
        if (trimmed.Contains("&&") || trimmed.Contains(";") || trimmed.Contains("|"))
        {
            // Check if there is a git clone inside
            var embeddedRepo = Regex.Match(trimmed, @"https?://github\.com/[^\s;&|]+");
            var extractedUrl = embeddedRepo.Success ? _gitService.NormalizeGitUrl(embeddedRepo.Value) : null;
            var repoName = extractedUrl != null ? _gitService.ExtractRepoName(extractedUrl) : null;

            return new SmartCommandInfo
            {
                RawInput = trimmed,
                Kind = CommandInputKind.CompoundCommand,
                ExtractedGitUrl = extractedUrl,
                ExtractedRepoName = repoName,
                ExecutableCommand = trimmed,
                RecommendedShell = ShellType.PowerShell,
                Description = extractedUrl != null 
                    ? $"Compound Pipeline containing GitHub repo ({repoName})"
                    : "Multi-Step Command Pipeline",
                IsDirectRepo = extractedUrl != null
            };
        }

        // 8. General Shell Command
        return new SmartCommandInfo
        {
            RawInput = trimmed,
            Kind = CommandInputKind.GeneralShellCommand,
            ExecutableCommand = trimmed,
            RecommendedShell = ShellType.PowerShell,
            Description = "Shell Command (Direct terminal execution)",
            IsDirectRepo = false
        };
    }

    /// <summary>
    /// Executes the smart command in the appropriate terminal or runner, streaming output.
    /// </summary>
    public async Task<OperationResult> ExecuteCommandAsync(
        SmartCommandInfo info,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken ct = default)
    {
        onOutput?.Invoke($"[Mercury] Executing ({info.Description})...");

        if (TerminalService.IsWindows)
        {
            // On Windows, use PowerShell with ExecutionPolicy Bypass and TLS 1.2/1.3
            return await _terminalService.RunPowerShellCommandAsync(
                info.ExecutableCommand,
                workingDirectory,
                onOutput,
                onError,
                ct);
        }
        else
        {
            // On macOS / Linux, run in Apple Terminal / Unix shell
            return await _terminalService.RunUnixShellCommandAsync(
                info.ExecutableCommand,
                workingDirectory,
                onOutput,
                onError,
                ct);
        }
    }
}
