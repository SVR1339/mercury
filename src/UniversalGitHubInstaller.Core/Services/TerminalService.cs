using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Services;

public enum ShellType
{
    Auto,
    PowerShell,
    Cmd,
    AppleTerminal,
    Bash
}

public class TerminalService
{
    private readonly ProcessRunner _runner = new();

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Finds the best available PowerShell executable on the system.
    /// </summary>
    public static string FindPowerShellExe()
    {
        if (IsWindows)
        {
            // Check pwsh (PowerShell 7+) first
            var pwshPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PowerShell", "7", "pwsh.exe");
            if (File.Exists(pwshPath)) return pwshPath;

            // Check pwsh in PATH
            if (CanExecute("pwsh.exe")) return "pwsh.exe";

            // Fallback to standard Windows PowerShell 5.1
            return "powershell.exe";
        }
        else
        {
            // macOS / Linux pwsh if installed
            if (CanExecute("pwsh")) return "pwsh";
            return "pwsh";
        }
    }

    private static bool CanExecute(string binary)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = binary,
                Arguments = IsWindows ? "-NoProfile -Command \"exit 0\"" : "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (proc == null) return false;
            proc.WaitForExit(3000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Launches an external interactive terminal window in the target directory.
    /// Supports PowerShell, CMD, and Apple Terminal.
    /// </summary>
    public OperationResult OpenTerminalWindow(string? workingDirectory = null, ShellType shell = ShellType.Auto)
    {
        var targetDir = string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : workingDirectory;

        try
        {
            if (IsWindows)
            {
                if (shell == ShellType.Cmd)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/k title Mercury CMD Console",
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                    return OperationResult.Ok("Command Prompt opened.");
                }
                else
                {
                    // PowerShell (preferred)
                    var psExe = FindPowerShellExe();
                    // Launch with ExecutionPolicy Bypass to allow any installation scripts
                    var args = "-NoExit -ExecutionPolicy Bypass -Command \"Write-Host '=== Mercury PowerShell Terminal ===' -ForegroundColor Cyan; Write-Host 'Full internet and terminal access enabled.' -ForegroundColor Green; Write-Host ''\"";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = psExe,
                        Arguments = args,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                    return OperationResult.Ok($"PowerShell ({psExe}) opened.");
                }
            }
            else if (IsMacOS)
            {
                // Apple Terminal on macOS
                // Use macOS 'open -a Terminal' command
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-a Terminal \"{targetDir}\"",
                    UseShellExecute = false
                });
                return OperationResult.Ok("Apple Terminal opened.");
            }
            else
            {
                // Linux terminal
                var terminals = new[] { "x-terminal-emulator", "gnome-terminal", "konsole", "xfce4-terminal", "xterm" };
                foreach (var term in terminals)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = term,
                            WorkingDirectory = targetDir,
                            UseShellExecute = false
                        });
                        return OperationResult.Ok($"{term} opened.");
                    }
                    catch { }
                }
                return OperationResult.Fail("No supported terminal emulator found.");
            }
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to open terminal: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a command in PowerShell with full execution policy bypass, TLS 1.2/1.3, and internet access.
    /// </summary>
    public async Task<OperationResult> RunPowerShellCommandAsync(
        string command,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken ct = default,
        int timeoutMs = 600_000)
    {
        var psExe = FindPowerShellExe();
        
        // Ensure TLS 1.2/1.3 is enabled so web downloads (irm, iwr) succeed seamlessly
        var setupScript = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13; " + command;
        var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(setupScript));
        var args = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}";

        var runner = new ProcessRunner();
        if (onOutput != null) runner.OnStdOut += onOutput;
        if (onError != null) runner.OnStdErr += onError;

        return await runner.RunAsync(psExe, args, workingDirectory, ct: ct, timeoutMs: timeoutMs);
    }

    /// <summary>
    /// Executes a command in the native Unix shell (macOS Apple Terminal / Linux bash / zsh).
    /// </summary>
    public async Task<OperationResult> RunUnixShellCommandAsync(
        string command,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken ct = default,
        int timeoutMs = 600_000)
    {
        // On macOS default is zsh, or bash
        var shell = File.Exists("/bin/zsh") ? "/bin/zsh" : (File.Exists("/bin/bash") ? "/bin/bash" : "/bin/sh");
        var escaped = command.Replace("\"", "\\\"");
        var args = $"-c \"{escaped}\"";

        var runner = new ProcessRunner();
        if (onOutput != null) runner.OnStdOut += onOutput;
        if (onError != null) runner.OnStdErr += onError;

        return await runner.RunAsync(shell, args, workingDirectory, ct: ct, timeoutMs: timeoutMs);
    }

    /// <summary>
    /// Executes a command in CMD (Windows Command Prompt).
    /// </summary>
    public async Task<OperationResult> RunCmdCommandAsync(
        string command,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken ct = default,
        int timeoutMs = 600_000)
    {
        var runner = new ProcessRunner();
        if (onOutput != null) runner.OnStdOut += onOutput;
        if (onError != null) runner.OnStdErr += onError;

        return await runner.RunAsync("cmd.exe", $"/c {command}", workingDirectory, ct: ct, timeoutMs: timeoutMs);
    }
}
