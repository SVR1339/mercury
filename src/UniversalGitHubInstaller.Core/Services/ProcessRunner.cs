using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Services;

public class ProcessRunner
{
    public event Action<string>? OnStdOut;
    public event Action<string>? OnStdErr;

    public async Task<OperationResult> RunAsync(
        string command,
        string? arguments = null,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default,
        int timeoutMs = 300_000)
    {
        var sw = Stopwatch.StartNew();
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments ?? "",
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (environmentVariables != null)
            {
                foreach (var (key, value) in environmentVariables)
                    psi.EnvironmentVariables[key] = value;
            }

            using var proc = new Process { StartInfo = psi };

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                    OnStdOut?.Invoke(e.Data);
                }
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stderr.AppendLine(e.Data);
                    OnStdErr?.Invoke(e.Data);
                }
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            try
            {
                await proc.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                sw.Stop();
                var timeoutResult = OperationResult.Fail("Process timed out or was cancelled.",
                    detail: $"Command: {command} {arguments}",
                    exitCode: -1);
                timeoutResult.Duration = sw.Elapsed;
                timeoutResult.StdOut = stdout.ToString();
                timeoutResult.StdErr = stderr.ToString();
                return timeoutResult;
            }

            sw.Stop();

            if (proc.ExitCode == 0)
            {
                return new OperationResult
                {
                    Success = true,
                    Message = "Command completed successfully.",
                    ExitCode = 0,
                    StdOut = stdout.ToString(),
                    StdErr = stderr.ToString(),
                    Duration = sw.Elapsed
                };
            }

            return new OperationResult
            {
                Success = false,
                Message = $"Command failed with exit code {proc.ExitCode}.",
                Detail = stderr.ToString(),
                ExitCode = proc.ExitCode,
                StdOut = stdout.ToString(),
                StdErr = stderr.ToString(),
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var errResult = OperationResult.Fail(
                $"Failed to execute: {ex.Message}",
                detail: $"Command: {command} {arguments}");
            errResult.Duration = sw.Elapsed;
            return errResult;
        }
    }

    public async Task<OperationResult> RunShellAsync(
        string shellCommand,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default,
        int timeoutMs = 300_000)
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            // On Windows, use cmd.exe for generic shell commands or package managers
            return await RunAsync("cmd.exe", $"/c {shellCommand}",
                workingDirectory, environmentVariables, ct, timeoutMs);
        }
        else
        {
            // On macOS / Linux (Apple Terminal), use /bin/zsh or /bin/bash
            var shell = File.Exists("/bin/zsh") ? "/bin/zsh" : (File.Exists("/bin/bash") ? "/bin/bash" : "/bin/sh");
            var escaped = shellCommand.Replace("\"", "\\\"");
            return await RunAsync(shell, $"-c \"{escaped}\"",
                workingDirectory, environmentVariables, ct, timeoutMs);
        }
    }
}
