using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Detection;
using UniversalGitHubInstaller.Core.Logging;
using UniversalGitHubInstaller.Core.Models;
using UniversalGitHubInstaller.Core.Runtime;
using UniversalGitHubInstaller.Core.Security;
using UniversalGitHubInstaller.Core.Services;

namespace UniversalGitHubInstaller.Cli;

class Program
{
    private static readonly AppLogger Logger = new("cli");
    private static readonly SmartCommandProcessor SmartProcessor = new();
    private static readonly TerminalService TerminalService = new();

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var targetArgs = args.Skip(1).ToArray();

        return command switch
        {
            "scan" => await RunScan(targetArgs),
            "install" => await RunInstall(targetArgs),
            "repair" => await RunRepair(targetArgs),
            "update" => await RunUpdate(targetArgs),
            "run" => await RunRun(targetArgs),
            "doctor" => await RunDoctor(),
            "terminal" or "shell" => RunTerminal(targetArgs),
            "exec" or "run-cmd" => await RunExec(string.Join(" ", targetArgs)),
            "version" or "--version" or "-v" => PrintVersion(),
            "help" or "--help" or "-h" => PrintHelp(),
            _ => await HandleAutoInput(args)
        };
    }

    static async Task<int> HandleAutoInput(string[] args)
    {
        var input = string.Join(" ", args);
        var info = SmartProcessor.ParseInput(input);

        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  ⚡ Auto-Detected: {info.Description}\n");
        Console.ResetColor();

        if (info.IsDirectRepo && !string.IsNullOrWhiteSpace(info.ExtractedGitUrl))
        {
            return await RunInstall(new[] { info.ExtractedGitUrl });
        }
        else
        {
            return await RunExec(info.ExecutableCommand);
        }
    }

    static async Task<int> RunExec(string command)
    {
        command = command.Trim().Trim('"', '\'').Trim('\\').Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            PrintError("No command specified to execute.");
            return 1;
        }

        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  Executing in System Terminal (PowerShell / Shell)...");
        Console.WriteLine($"  Command: {command}");
        Console.WriteLine($"  Internet & Full Terminal Access: ENABLED\n");
        Console.ResetColor();

        var info = SmartProcessor.ParseInput(command);
        var result = await SmartProcessor.ExecuteCommandAsync(
            info,
            Environment.CurrentDirectory,
            onOutput: line => Console.WriteLine($"    {line}"),
            onError: line => { Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine($"    [stderr] {line}"); Console.ResetColor(); });

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  ✓ Command completed successfully ({result.Duration.TotalSeconds:F1}s)\n");
            Console.ResetColor();
            return 0;
        }
        else
        {
            PrintError($"Command exited with code {result.ExitCode}: {result.Message}");
            return result.ExitCode ?? 1;
        }
    }

    static int RunTerminal(string[] args)
    {
        PrintBanner();
        var shell = ShellType.Auto;
        if (args.Length > 0)
        {
            shell = args[0].ToLowerInvariant() switch
            {
                "pwsh" or "powershell" => ShellType.PowerShell,
                "cmd" => ShellType.Cmd,
                "apple" or "bash" or "zsh" => ShellType.AppleTerminal,
                _ => ShellType.Auto
            };
        }

        var dir = args.Length > 1 ? args[1] : Environment.CurrentDirectory;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  Opening interactive terminal ({shell}) in: {dir}");
        Console.WriteLine("  Full internet & bypass access enabled.\n");
        Console.ResetColor();

        var res = TerminalService.OpenTerminalWindow(dir, shell);
        if (res.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {res.Message}\n");
            Console.ResetColor();
            return 0;
        }
        else
        {
            PrintError(res.Message);
            return 1;
        }
    }

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("+==============================================================+");
        Console.WriteLine("|   Mercury - Universal GitHub and Terminal Installer v1.0.0   |");
        Console.WriteLine("+==============================================================+");
        Console.ResetColor();
    }

    static int PrintHelp()
    {
        PrintBanner();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("USAGE:");
        Console.ResetColor();
        Console.WriteLine("  mercury <path|url|command>            Auto-detect and install repo or run script");
        Console.WriteLine("  mercury install <path|url|command>    Clone repo and auto-install all dependencies");
        Console.WriteLine("  mercury scan <path|url>               Analyze technologies, services, and security");
        Console.WriteLine("  mercury exec \"<command>\"              Execute command with full terminal & internet access");
        Console.WriteLine("  mercury terminal [pwsh|cmd|apple]     Open an interactive terminal window");
        Console.WriteLine("  mercury run <path>                    Run project using detected start command");
        Console.WriteLine("  mercury repair <path>                 Repair project environment and dependencies");
        Console.WriteLine("  mercury update <path>                 Pull latest Git changes and re-verify");
        Console.WriteLine("  mercury doctor                        Diagnose system runtimes, git, and network");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("EXAMPLES:");
        Console.ResetColor();
        Console.WriteLine("  mercury https://github.com/user/project");
        Console.WriteLine("  mercury \"git clone https://github.com/user/project\"");
        Console.WriteLine("  mercury \"irm https://raw.githubusercontent.com/... | iex\"");
        Console.WriteLine("  mercury \"curl -fsSL https://raw.githubusercontent.com/... | bash\"");
        Console.WriteLine("  mercury terminal pwsh");
        Console.WriteLine("  mercury doctor");
        return 0;
    }

    static int PrintVersion()
    {
        Console.WriteLine("☿ Mercury - Universal GitHub & Terminal Installer v1.0.0");
        return 0;
    }

    static async Task<int> RunScan(string[] args)
    {
        var path = args.Length > 0 ? args[0] : ".";
        PrintBanner();

        // Check if it's a URL
        var gitService = new GitService();
        if (gitService.IsGitUrl(path))
        {
            var repoName = gitService.ExtractRepoName(path);
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mercury", "projects", repoName);

            Console.WriteLine($"\nCloning {path}...");
            var cloneResult = await gitService.CloneAsync(path, targetDir);
            if (!cloneResult.Success)
            {
                PrintError($"Clone failed: {cloneResult.Message}");
                return 1;
            }
            path = targetDir;
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            PrintError($"Directory not found: {path}");
            return 1;
        }

        Console.WriteLine($"\n  Scanning: {path}\n");

        var scanner = new ProjectScanner();
        scanner.OnProgress += msg =>
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {msg}");
            Console.ResetColor();
        };

        var info = await scanner.ScanAsync(path);

        // Display results
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n  â•â•â• PROJECT ANALYSIS â•â•â•\n");
        Console.ResetColor();

        Console.WriteLine($"  Project:     {info.Name}");
        Console.WriteLine($"  Location:    {info.Path}");
        if (info.GitRemote != null) Console.WriteLine($"  Git Remote:  {info.GitRemote}");
        if (info.Branch != null) Console.WriteLine($"  Branch:      {info.Branch}");
        if (info.Commit != null) Console.WriteLine($"  Commit:      {info.Commit}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n  Technologies:");
        Console.ResetColor();
        foreach (var tech in info.Technologies)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  âœ“ ");
            Console.ResetColor();
            Console.Write(tech.Name);
            if (tech.Framework != null) Console.Write($" ({tech.Framework})");
            if (tech.Version != null) Console.Write($" â€” {tech.Version}");
            Console.WriteLine();
        }

        if (info.PackageManagers.Any())
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  Package Managers:");
            Console.ResetColor();
            foreach (var pm in info.PackageManagers)
            {
                Console.WriteLine($"    {pm.Name}: {pm.InstallCommand}");
                if (pm.DependencyCount > 0)
                    Console.WriteLine($"    Dependencies: {pm.DependencyCount}");
            }
        }

        if (info.Services.Any())
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  Services:");
            Console.ResetColor();
            foreach (var svc in info.Services)
            {
                Console.WriteLine($"    {svc.Name} ({svc.Type}){(svc.Port.HasValue ? $" â€” Port {svc.Port}" : "")}");
            }
        }

        if (info.EnvironmentVariables.Any())
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  Environment Variables:");
            Console.ResetColor();
            foreach (var ev in info.EnvironmentVariables)
            {
                var status = ev.IsRequired ? "Required" : "Optional";
                Console.ForegroundColor = ev.IsRequired ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
                Console.Write($"    {status,-12}");
                Console.ResetColor();
                Console.WriteLine($" {ev.Key}");
            }
        }

        if (info.BuildSystem != null)
            Console.WriteLine($"\n  Build:       {info.BuildSystem}");
        if (info.TestSystem != null)
            Console.WriteLine($"  Test:        {info.TestSystem}");
        if (info.PossibleRunCommands.Any())
            Console.WriteLine($"  Run:         {info.PossibleRunCommands.First()}");

        Console.WriteLine();

        // Security scan
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  â•â•â• SECURITY SCAN â•â•â•\n");
        Console.ResetColor();

        var secScanner = new SecurityScanner();
        var secResult = secScanner.Scan(path);

        if (secResult.Passed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  âœ“ Security scan passed (no critical issues)");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  âœ— {secResult.CriticalCount} critical issue(s) found");
            Console.ResetColor();
        }

        if (secResult.WarningCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  âš  {secResult.WarningCount} warning(s)");
            Console.ResetColor();
        }

        foreach (var finding in secResult.Findings.Take(10))
        {
            var color = finding.Severity == "Critical" ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.ForegroundColor = color;
            Console.Write($"    [{finding.Severity}] ");
            Console.ResetColor();
            Console.WriteLine($"{finding.File}:{finding.Line} â€” {finding.Description}");
        }

        Console.WriteLine();

        // Save profile
        var profileService = new ProjectProfileService();
        var profile = profileService.CreateProfile(info);
        profileService.SaveProfile(profile, path);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  âœ“ Project profile saved to .project-installer/project.json");
        Console.ResetColor();
        Console.WriteLine();

        return 0;
    }

    static async Task<int> RunInstall(string[] args)
    {
        var path = args.Length > 0 ? args[0] : ".";
        PrintBanner();

        var gitService = new GitService();
        if (gitService.IsGitUrl(path))
        {
            var repoName = gitService.ExtractRepoName(path);
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mercury", "projects", repoName);
            Console.WriteLine($"\nCloning {path}...");
            var cloneResult = await gitService.CloneAsync(path, targetDir);
            if (!cloneResult.Success)
            {
                PrintError($"Clone failed: {cloneResult.Message}");
                return 1;
            }
            path = targetDir;
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            PrintError($"Directory not found: {path}");
            return 1;
        }

        Console.WriteLine($"\n  Scanning {path}...\n");
        var scanner = new ProjectScanner();
        var info = await scanner.ScanAsync(path);

        var runner = new ProcessRunner();
        runner.OnStdOut += line => Console.WriteLine($"    {line}");
        runner.OnStdErr += line => { Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine($"    {line}"); Console.ResetColor(); };

        foreach (var pm in info.PackageManagers)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Installing {pm.Name} dependencies: {pm.InstallCommand}");
            Console.ResetColor();

            var result = await runner.RunShellAsync(pm.InstallCommand, path);
            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  âœ“ {pm.Name} complete ({result.Duration.TotalSeconds:F1}s)");
                Console.ResetColor();
            }
            else
            {
                PrintError($"{pm.Name} failed: {result.Message}");
                if (result.ExitCode.HasValue)
                    Console.WriteLine($"    Exit code: {result.ExitCode}");
                return 1;
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n  âœ“ All dependencies installed successfully.\n");
        Console.ResetColor();
        return 0;
    }

    static async Task<int> RunRepair(string[] args)
    {
        var path = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
        PrintBanner();
        Console.WriteLine($"\n  Repairing project: {path}\n");

        return await RunInstall(new[] { path });
    }

    static async Task<int> RunUpdate(string[] args)
    {
        var path = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
        PrintBanner();
        Console.WriteLine($"\n  Updating project: {path}\n");

        var gitService = new GitService();
        bool hasChanges = await gitService.HasLocalChangesAsync(path);
        if (hasChanges)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  âš  Local uncommitted changes detected.");
            Console.ResetColor();
            Console.Write("  Pull anyway? (y/N) ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer != "y")
            {
                Console.WriteLine("  Cancelled.");
                return 0;
            }
        }

        var result = await gitService.PullAsync(path);
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  âœ“ Updated successfully.");
            Console.ResetColor();

            // Re-install dependencies
            return await RunInstall(new[] { path });
        }
        else
        {
            PrintError($"Update failed: {result.Message}");
            return 1;
        }
    }

    static async Task<int> RunRun(string[] args)
    {
        var path = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
        PrintBanner();

        var scanner = new ProjectScanner();
        var info = await scanner.ScanAsync(path);

        if (!info.PossibleRunCommands.Any())
        {
            PrintError("No run command detected.");
            return 1;
        }

        var runCmd = info.PossibleRunCommands.First();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  Running: {runCmd}\n");
        Console.ResetColor();

        var runner = new ProcessRunner();
        runner.OnStdOut += line => Console.WriteLine(line);
        runner.OnStdErr += line => Console.WriteLine(line);

        var result = await runner.RunShellAsync(runCmd, path, timeoutMs: 0);
        return result.ExitCode ?? (result.Success ? 0 : 1);
    }

    static async Task<int> RunDoctor()
    {
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n  â•â•â• SYSTEM DIAGNOSTICS â•â•â•\n");
        Console.ResetColor();

        var envScanner = new WindowsEnvironmentScanner();
        var info = await Task.Run(() => envScanner.Scan());

        Console.WriteLine($"  Windows:        {info.WindowsVersion}");
        Console.WriteLine($"  Architecture:   {info.Architecture}");
        Console.WriteLine($"  CPU:            {info.CpuArchitecture}");
        Console.WriteLine($"  Disk Space:     {info.AvailableDiskSpaceMB:N0} MB available");
        Console.WriteLine($"  RAM:            {info.TotalRamMB:N0} MB");
        Console.WriteLine($"  Git:            {(info.HasGit ? info.GitVersion : "NOT INSTALLED")}");
        Console.WriteLine($"  winget:         {(info.HasWinget ? "Available" : "Not found")}");
        Console.WriteLine($"  Chocolatey:     {(info.HasChocolatey ? "Available" : "Not found")}");
        Console.WriteLine($"  Scoop:          {(info.HasScoop ? "Available" : "Not found")}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n  â•â•â• RUNTIMES â•â•â•\n");
        Console.ResetColor();

        foreach (var (name, rt) in info.Runtimes.OrderBy(x => x.Key))
        {
            Console.ForegroundColor = rt.Installed ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.Write(rt.Installed ? "  âœ“ " : "  âœ— ");
            Console.ResetColor();
            Console.Write($"{name,-20}");
            Console.ForegroundColor = rt.Installed ? ConsoleColor.White : ConsoleColor.DarkGray;
            Console.WriteLine(rt.Installed ? rt.Version : "Not installed");
            Console.ResetColor();
        }

        Console.WriteLine();
        return 0;
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  âœ— {message}");
        Console.ResetColor();
        Logger.Error(message);
    }
}
