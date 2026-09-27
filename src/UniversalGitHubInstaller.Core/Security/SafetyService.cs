using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace UniversalGitHubInstaller.Core.Security;

/// <summary>Risk classification for an operation.</summary>
public enum RiskLevel
{
    Safe,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>Result of a safety assessment on a command or URL.</summary>
public class SafetyAssessment
{
    public RiskLevel Risk { get; init; }
    public List<string> Findings { get; init; } = new();
    public string Summary { get; init; } = string.Empty;
    public bool RequiresConsent => Risk >= RiskLevel.Medium;
    public bool IsBlocked => Risk == RiskLevel.Critical;
}

/// <summary>
/// Provides global safety, regulatory compliance, and threat-assessment
/// services for all operations performed by Mercury.
/// </summary>
public class SafetyService
{
    // ── Threat indicators ──────────────────────────────────────────────────
    private static readonly (string Pattern, string Description, RiskLevel Risk)[] CommandPatterns =
    {
        // Critical — system-destructive or privilege-escalation
        (@"rm\s+-rf\s+/|Remove-Item.*-Recurse.*[Cc]:\\",  "Recursive root/system deletion",          RiskLevel.Critical),
        (@"format\s+[a-zA-Z]:",                            "Disk format command",                     RiskLevel.Critical),
        (@"Set-MpPreference.*-Disable",                    "Antivirus tampering",                     RiskLevel.Critical),
        (@"netsh\s+advfirewall.*(disable|state\s+off)",    "Firewall disable",                        RiskLevel.Critical),
        (@"-Enc\s+[A-Za-z0-9+/=]{30,}",                   "Base64-encoded hidden command",            RiskLevel.Critical),
        (@"schtasks.*\/create",                            "Scheduled persistence task",              RiskLevel.Critical),
        (@"net\s+user.*\/add",                             "Hidden user account creation",            RiskLevel.Critical),
        (@"HKLM:.*\\Run|HKCU:.*\\Run",                    "Registry autorun entry",                  RiskLevel.Critical),

        // High — network exfiltration or elevated execution
        (@"Start-Process.*-Verb\s+RunAs",                  "UAC elevation bypass attempt",            RiskLevel.High),
        (@"Invoke-Expression|IEX\s*\(",                    "Dynamic code execution (IEX)",            RiskLevel.High),
        (@"curl.*\|\s*(bash|sh|pwsh|powershell)",          "Piped remote script execution",           RiskLevel.High),
        (@"irm.*\|\s*iex",                                 "Remote PowerShell installation script",   RiskLevel.High),
        (@"DownloadFile|WebClient|Invoke-WebRequest",      "Remote file download",                    RiskLevel.High),

        // Medium — suspicious but common in legitimate installers
        (@"ExecutionPolicy\s+Bypass",                      "ExecutionPolicy Bypass",                  RiskLevel.Medium),
        (@"FromBase64String",                              "Base64 decoding",                         RiskLevel.Medium),
        (@"[Ss]udo\s+",                                    "Elevated sudo execution",                 RiskLevel.Medium),
        (@"chmod\s+\+x",                                   "File executable permission grant",        RiskLevel.Medium),
    };

    private static readonly (string Pattern, string Description, RiskLevel Risk)[] UrlPatterns =
    {
        (@"^(?!https://)",                                 "Non-HTTPS URL (insecure transport)",      RiskLevel.High),
        (@"raw\.githubusercontent\.com",                   "Raw GitHub script (verify source)",       RiskLevel.Medium),
        (@"pastebin\.com|hastebin\.com",                   "Paste-site hosted script",                RiskLevel.High),
        (@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}",          "IP address instead of hostname",          RiskLevel.High),
    };

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Assesses a command string for safety risks.</summary>
    public SafetyAssessment AssessCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return Safe("Empty command.");

        var findings = new List<string>();
        var maxRisk  = RiskLevel.Safe;

        foreach (var (pattern, description, risk) in CommandPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                findings.Add($"[{risk.ToString().ToUpper()}]  {description}");
                if (risk > maxRisk) maxRisk = risk;
            }
        }

        return Build(maxRisk, findings, command);
    }

    /// <summary>Assesses a URL for safety risks.</summary>
    public SafetyAssessment AssessUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Safe("No URL provided.");

        var findings = new List<string>();
        var maxRisk  = RiskLevel.Safe;

        foreach (var (pattern, description, risk) in UrlPatterns)
        {
            if (Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))
            {
                findings.Add($"[{risk.ToString().ToUpper()}]  {description}");
                if (risk > maxRisk) maxRisk = risk;
            }
        }

        return Build(maxRisk, findings, url);
    }

    /// <summary>Enforces HTTPS for repository and script URLs.</summary>
    public static string EnforceHttps(string url)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return "https://" + url[7..];
        return url;
    }

    /// <summary>Validates that a URL is a known safe Git host.</summary>
    public static bool IsKnownGitHost(string url)
    {
        var knownHosts = new[]
        {
            "github.com", "gitlab.com", "bitbucket.org",
            "dev.azure.com", "codeberg.org", "gitea.com"
        };
        return knownHosts.Any(h => url.Contains(h, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sanitizes repository or project names to prevent Path Traversal (CWE-22)
    /// and invalid filesystem characters globally across all operating systems.
    /// </summary>
    public static string SanitizeProjectName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "project";

        // Remove path traversal sequences and slashes
        var sanitized = rawName.Replace("..", "").Replace("/", "_").Replace("\\", "_");

        // Strip invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        sanitized = new string(sanitized.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();

        // Collapse multiple underscores and trim leading/trailing underscores
        sanitized = Regex.Replace(sanitized, @"_+", "_");
        sanitized = sanitized.Trim('_', '.', ' ', '-');

        return string.IsNullOrWhiteSpace(sanitized) ? "project" : sanitized;
    }

    /// <summary>
    /// Validates that a resolved path resides strictly within the expected parent directory,
    /// defending against directory traversal and symlink escape vulnerabilities.
    /// </summary>
    public static bool IsPathWithinDirectory(string candidatePath, string parentDirectory)
    {
        try
        {
            var fullCandidate = Path.GetFullPath(candidatePath);
            var fullParent = Path.GetFullPath(parentDirectory);
            if (!fullParent.EndsWith(Path.DirectorySeparatorChar.ToString()))
                fullParent += Path.DirectorySeparatorChar;

            return fullCandidate.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static SafetyAssessment Safe(string note) => new()
    {
        Risk     = RiskLevel.Safe,
        Summary  = note,
        Findings = new List<string>()
    };

    private static SafetyAssessment Build(RiskLevel risk, List<string> findings, string input)
    {
        var truncated = input.Length > 80 ? input[..80] + "..." : input;
        var summary   = risk switch
        {
            RiskLevel.Safe     => "No safety concerns detected.",
            RiskLevel.Low      => "Minor observations — operation is safe to proceed.",
            RiskLevel.Medium   => $"Moderate risk detected. Review before proceeding: {truncated}",
            RiskLevel.High     => $"High-risk operation identified. Explicit consent required.",
            RiskLevel.Critical => $"BLOCKED — Operation poses a critical system threat.",
            _                  => "Unknown risk level."
        };

        return new SafetyAssessment { Risk = risk, Summary = summary, Findings = findings };
    }
}
