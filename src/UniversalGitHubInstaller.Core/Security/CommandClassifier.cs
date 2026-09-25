using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Security;

public class CommandClassifier
{
    private static readonly string[] DangerousPatterns = {
        @"rm\s+-rf\s+[/~]", @"Remove-Item\s+['""]?\$env:SystemDrive",
        @"format\s+[a-z]:", @"del\s+/[sq]\s+[a-z]:\\",
        @"reg\s+(add|delete)\s+hklm", @"schtasks\s+/create",
        @"net\s+user", @"netsh", @"runas",
        @"Invoke-Expression\s*\(", @"IEX\s*\(",
        @"DROP\s+(TABLE|DATABASE|SCHEMA)", @"TRUNCATE\s+TABLE",
        @"DELETE\s+FROM\s+\w+\s*$", @"DELETE\s+FROM\s+\w+\s+WHERE\s+1\s*=\s*1",
        @"docker.*--privileged",
    };

    private static readonly string[] ConfirmationPatterns = {
        @"pip\s+install", @"npm\s+(install|ci|i)", @"pnpm\s+install",
        @"yarn\s+install", @"bun\s+install",
        @"dotnet\s+restore", @"cargo\s+(fetch|install)", @"go\s+(mod\s+download|install)",
        @"composer\s+install", @"bundle\s+install",
        @"docker\s+compose.*up", @"docker\s+run",
        @"\.ps1", @"\.cmd", @"\.bat", @"\.vbs", @"\.sh",
        @"winget\s+install", @"choco\s+install", @"scoop\s+install", @"brew\s+install",
        @"python\s+.*\.py", @"node\s+.*\.js",
        @"git\s+clone", @"git\s+pull",
        @"(irm|iwr|Invoke-RestMethod|Invoke-WebRequest).+\|\s*(iex|Invoke-Expression)",
        @"curl.*\|\s*(bash|sh|powershell|zsh)", @"wget.*\|\s*(bash|sh)",
        @"powershell.*(-c|-Command)",
    };

    private static readonly string[] SafePatterns = {
        @"^git\s+(status|log|branch|diff|remote|tag|show)",
        @"^git\s+fetch$",
        @"^python\s+--version", @"^node\s+--version",
        @"^dotnet\s+--version", @"^cargo\s+--version",
        @"^(dir|ls|cat|type|more|head|tail|find|findstr|grep)",
        @"^echo\s+", @"^cd\s+", @"^pwd$", @"^whoami$",
        @"^dotnet\s+(build|test|run)",
        @"^cargo\s+(build|test|run|check|clippy)",
        @"^go\s+(build|test|vet|fmt)",
        @"^pytest", @"^npm\s+test", @"^jest",
    };

    public ClassifiedCommand Classify(string command, string workingDirectory, string? context = null)
    {
        var result = new ClassifiedCommand
        {
            Command = command,
            WorkingDirectory = workingDirectory
        };

        // Check dangerous patterns first
        foreach (var pattern in DangerousPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                result.Safety = CommandSafety.Dangerous;
                result.Reason = $"Command matches dangerous pattern: {pattern}";
                result.ExpectedEffect = "This command could damage the system, delete data, or compromise security.";
                return result;
            }
        }

        // Check safe patterns
        foreach (var pattern in SafePatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                result.Safety = CommandSafety.Safe;
                result.Reason = "Command is a known safe operation.";
                result.ExpectedEffect = "Read-only or controlled operation.";
                return result;
            }
        }

        // Check confirmation patterns
        foreach (var pattern in ConfirmationPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                result.Safety = CommandSafety.RequiresConfirmation;
                result.Reason = "Command modifies the local environment or installs software.";
                result.ExpectedEffect = "Will install dependencies or modify files in the project directory.";
                return result;
            }
        }

        // Default: requires confirmation for unknown commands
        result.Safety = CommandSafety.RequiresConfirmation;
        result.Reason = "Unknown command â€” user confirmation recommended.";
        result.ExpectedEffect = "Effect is unknown; review before executing.";
        return result;
    }
}
