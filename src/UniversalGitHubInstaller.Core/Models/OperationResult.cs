using System;

namespace UniversalGitHubInstaller.Core.Models;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int? ExitCode { get; set; }
    public string? StdOut { get; set; }
    public string? StdErr { get; set; }
    public TimeSpan Duration { get; set; }

    public static OperationResult Ok(string message = "Success") => new()
    {
        Success = true,
        Message = message
    };

    public static OperationResult Fail(string message, string? detail = null, int? exitCode = null) => new()
    {
        Success = false,
        Message = message,
        Detail = detail,
        ExitCode = exitCode
    };
}
