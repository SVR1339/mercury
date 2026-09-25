using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UniversalGitHubInstaller.Core.Security;

public class SecurityFinding
{
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Severity { get; set; } = "Warning"; // Info, Warning, Critical
    public string Description { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
}

public class SecurityScanResult
{
    public bool Passed { get; set; }
    public List<SecurityFinding> Findings { get; set; } = new();
    public int CriticalCount => Findings.Count(f => f.Severity == "Critical");
    public int WarningCount => Findings.Count(f => f.Severity == "Warning");
}

public class SecurityScanner
{
    private static readonly (string Pattern, string Description, string Severity)[] ScanPatterns =
    {
        (@"Invoke-Expression|IEX\s*\(", "PowerShell code execution (IEX/Invoke-Expression)", "Critical"),
        (@"DownloadString|DownloadFile|WebClient", "Network download detected", "Warning"),
        (@"FromBase64String|ToBase64String", "Base64 encoding/decoding", "Warning"),
        (@"-Enc\s+[A-Za-z0-9+/=]{20,}", "Encoded PowerShell command", "Critical"),
        (@"New-Object\s+Net\.WebClient", "WebClient instantiation", "Warning"),
        (@"Start-Process.*-Verb\s+RunAs", "UAC elevation request", "Critical"),
        (@"HKLM:|HKCU:.*\\Run", "Registry persistence mechanism", "Critical"),
        (@"schtasks\s+/create", "Scheduled task creation", "Critical"),
        (@"net\s+user\s+\w+\s+\w+\s+/add", "User account creation", "Critical"),
        (@"netsh\s+advfirewall", "Firewall modification", "Critical"),
        (@"Set-MpPreference.*-DisableRealtimeMonitoring", "Antivirus disable attempt", "Critical"),
        (@"(password|secret|api_key|token)\s*=\s*['""][^'""]{8,}", "Hardcoded secret", "Warning"),
    };

    private static readonly string[] ScanExtensions = {
        ".ps1", ".psm1", ".psd1", ".cmd", ".bat", ".vbs", ".js", ".py",
        ".sh", ".bash", ".yml", ".yaml", ".json", ".xml", ".toml", ".cfg",
        ".ini", ".conf", ".env"
    };

    public SecurityScanResult Scan(string projectPath)
    {
        var result = new SecurityScanResult();

        try
        {
            var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
                .Where(f => ScanExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => !f.Contains(Path.Combine("node_modules", "")) &&
                           !f.Contains(Path.Combine(".git", "")) &&
                           !f.Contains(Path.Combine(".venv", "")) &&
                           !f.Contains(Path.Combine("__pycache__", "")));

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length > 1_000_000) continue; // Skip files > 1MB

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        foreach (var (pattern, desc, severity) in ScanPatterns)
                        {
                            if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase))
                            {
                                result.Findings.Add(new SecurityFinding
                                {
                                    File = Path.GetRelativePath(projectPath, file),
                                    Line = i + 1,
                                    Severity = severity,
                                    Description = desc,
                                    Pattern = pattern
                                });
                            }
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        // Check for suspicious executables
        try
        {
            var exeFiles = Directory.GetFiles(projectPath, "*.exe", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.Combine("node_modules", "")) &&
                           !f.Contains(Path.Combine(".git", "")));

            foreach (var exe in exeFiles)
            {
                result.Findings.Add(new SecurityFinding
                {
                    File = Path.GetRelativePath(projectPath, exe),
                    Severity = "Warning",
                    Description = "Pre-compiled executable found in repository"
                });
            }
        }
        catch { }

        result.Passed = result.CriticalCount == 0;
        return result;
    }
}
