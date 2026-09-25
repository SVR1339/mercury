namespace UniversalGitHubInstaller.Core.Models;

public enum CommandSafety
{
    Safe,
    RequiresConfirmation,
    Dangerous,
    Unsupported
}

public class ClassifiedCommand
{
    public string Command { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public CommandSafety Safety { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ExpectedEffect { get; set; } = string.Empty;
    public string? Category { get; set; }
}
